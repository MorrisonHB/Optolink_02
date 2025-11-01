Imports System.Text.RegularExpressions
Imports Optolink_02.Domain

Namespace Services
    ''' <summary>
    ''' Service für hierarchische MQTT-Abfragen mit HIDDEN-Filterung
    ''' Führt Gruppen- und Wert-Abfragen in der richtigen Reihenfolge durch
    ''' </summary>
    Public NotInheritable Class HierarchicalMqttQueryService
        Private Sub New()
        End Sub

        ' Globaler Cache für bereits abgefragte Werte (Adresse -> Wert)
        Private Shared ReadOnly _valueCache As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>
        ''' Führt eine vollständige hierarchische Abfrage durch mit Caching
        ''' </summary>
        Public Shared Async Function QueryDeviceHierarchyAsync(
          device As DeviceNode,
            mqttService As MqttService,
            sendTopic As String,
   receiveTopic As String,
  Optional timeoutMs As Integer = 3000,
         Optional progressCallback As Action(Of String) = Nothing
        ) As Task(Of DeviceNode)

            If device Is Nothing OrElse mqttService Is Nothing OrElse Not mqttService.IsConnected Then
                Return device
            End If

            ' Cache leeren bei neuer Abfrage
            _valueCache.Clear()

            ' ============================================================
            ' PHASE 1: GRUPPEN-HIDDEN-CONDITIONS AUSWERTEN
            ' ============================================================
            progressCallback?.Invoke("Phase 1: Analysiere Gruppen-Bedingungen...")

            Dim groupConditions As New Dictionary(Of String, String)()
            Dim groupsWithConditions As New List(Of GroupNode)()

            For Each category In device.Categories
                For Each group In category.Groups
                    If Not String.IsNullOrWhiteSpace(group.HiddenCondition) Then
                        ExtractRequiredConfigValues(group.HiddenCondition, groupConditions)
                        groupsWithConditions.Add(group)
                    Else
                        group.IsVisible = True
                    End If
                Next
            Next

            Dim configValues As New Dictionary(Of String, String)()

            If groupConditions.Count > 0 Then
                progressCallback?.Invoke($"Phase 1: Frage {groupConditions.Count} Konfigurationswerte ab...")
                configValues = Await QueryConfigurationValuesAsync(
                    groupConditions.Keys.ToList(),
                mqttService,
                           sendTopic,
              receiveTopic,
                    timeoutMs
           )
            End If

            progressCallback?.Invoke("Phase 1: Bewerte Gruppen-Bedingungen...")

            For Each group In groupsWithConditions
                group.IsVisible = Not EvaluateHiddenCondition(group.HiddenCondition, configValues)
            Next

            ' ============================================================
            ' PHASE 2: PARAMETER-HIDDEN-CONDITIONS AUSWERTEN
            ' ============================================================
            progressCallback?.Invoke("Phase 2: Analysiere Parameter-Bedingungen...")

            Dim paramConditions As New Dictionary(Of String, String)()
            Dim paramsWithConditions As New List(Of ParameterNode)()

            For Each category In device.Categories
                For Each group In category.Groups.Where(Function(g) g.IsVisible)
                    For Each param In group.Parameters
                        If Not String.IsNullOrWhiteSpace(param.HiddenCondition) Then
                            ExtractRequiredConfigValues(param.HiddenCondition, paramConditions)
                            paramsWithConditions.Add(param)
                        Else
                            param.IsVisible = True
                        End If
                    Next
                Next
            Next

            Dim missingKeys = paramConditions.Keys.Except(configValues.Keys).ToList()

            If missingKeys.Count > 0 Then
                progressCallback?.Invoke($"Phase 2: Frage {missingKeys.Count} zusätzliche Konfigurationswerte ab...")
                Dim additionalValues = Await QueryConfigurationValuesAsync(
              missingKeys,
                mqttService,
               sendTopic,
                receiveTopic,
                       timeoutMs
                       )

                For Each kvp In additionalValues
                    If Not configValues.ContainsKey(kvp.Key) Then
                        configValues(kvp.Key) = kvp.Value
                    End If
                Next
            End If

            progressCallback?.Invoke("Phase 2: Bewerte Parameter-Bedingungen...")

            For Each param In paramsWithConditions
                param.IsVisible = Not EvaluateHiddenCondition(param.HiddenCondition, configValues)
            Next

            ' ============================================================
            ' PHASE 3: WERTE ABFRAGEN (mit Conversion)
            ' ============================================================
            progressCallback?.Invoke("Phase 3: Frage Parameterwerte ab...")

            Await QueryParameterValuesAsync(
                  device,
          mqttService,
                 sendTopic,
           receiveTopic,
                timeoutMs,
                progressCallback
           )

            progressCallback?.Invoke("Abfrage abgeschlossen")
            Return device
        End Function

        Private Shared Sub ExtractRequiredConfigValues(condition As String, ByRef configDict As Dictionary(Of String, String))
            If String.IsNullOrWhiteSpace(condition) Then
                Return
            End If

            Dim cleanCondition = condition
            If cleanCondition.StartsWith("HIDDEN:(", StringComparison.OrdinalIgnoreCase) Then
                cleanCondition = cleanCondition.Substring(8)
            End If
            If cleanCondition.EndsWith(")"c) Then
                cleanCondition = cleanCondition.Substring(0, cleanCondition.Length - 1)
            End If

            Dim pattern As String = "\b([0-9A-F]{2,3}_[a-zA-Z0-9_]+)\b"
            Dim matches = Regex.Matches(cleanCondition, pattern, RegexOptions.IgnoreCase)

            For Each match As Match In matches
                Dim varName = match.Groups(1).Value
                If Not configDict.ContainsKey(varName) Then
                    configDict(varName) = ""
                End If
            Next
        End Sub

        Private Shared Async Function QueryConfigurationValuesAsync(
           keys As List(Of String),
          mqttService As MqttService,
                   sendTopic As String,
                   receiveTopic As String,
           timeoutMs As Integer
               ) As Task(Of Dictionary(Of String, String))

            Dim result As New Dictionary(Of String, String)()

            If keys Is Nothing OrElse keys.Count = 0 Then
                Return result
            End If

            Dim addressMap = Await GetConfigAddressMapAsync(keys)

            For Each key In keys
                Dim key_address As String = Nothing
                If addressMap.TryGetValue(key, key_address) Then

                    Dim key_address_value As String = Nothing
                    ' Cache-Check
                    If _valueCache.TryGetValue(key_address, key_address_value) Then
                        result(key) = key_address_value
                        Continue For
                    End If

                    Try
                        Dim QuerySingleValueAsync_value = Await QuerySingleValueAsync(key_address, mqttService, sendTopic, receiveTopic, timeoutMs, 2)
                        result(key) = QuerySingleValueAsync_value
                        _valueCache(key_address) = QuerySingleValueAsync_value
                    Catch ex As Exception
                        result(key) = ""
                    End Try
                Else
                    result(key) = ""
                End If
            Next

            Return result
        End Function

        Private Shared Async Function GetConfigAddressMapAsync(keys As List(Of String)) As Task(Of Dictionary(Of String, String))
            Dim result As New Dictionary(Of String, String)()

            If keys Is Nothing OrElse keys.Count = 0 Then
                Return result
            End If

            Try
                Dim dt = Await Services.SqlQueryService.GetConfigAddressMappingAsync(keys)
                For Each row As DataRow In dt.Rows
                    Dim key = Convert.ToString(row("ConfigKey"))
                    Dim address = Convert.ToString(row("HexAddress"))
                    If Not String.IsNullOrWhiteSpace(key) AndAlso Not String.IsNullOrWhiteSpace(address) Then
                        result(key) = address
                    End If
                Next
            Catch ex As Exception
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Fragt einen einzelnen Wert ab (mit Conversion und Cache)
        ''' </summary>
        Private Shared Async Function QuerySingleValueAsync(address As String,
                                                            mqttService As MqttService,
                                                            sendTopic As String,
                                                            receiveTopic As String,
                                                            timeoutMs As Integer,
                                                            Optional byteLength As Integer = 2,
                                                            Optional conversionType As String = Nothing,
                                                            Optional conversionFactor As Double = 1.0,
                                                            Optional conversionOffset As Double = 0.0) As Task(Of String)

            If String.IsNullOrWhiteSpace(address) Then Return ""
            Dim address_value As String = Nothing
            ' Cache-Check
            If _valueCache.TryGetValue(address, address_value) Then Return address_value
            Dim command = $"read;{address};{byteLength}"

            Try
                Dim response = Await mqttService.SendCommandAndWaitForResponseAsync(command,
                                                                                    sendTopic,
                                                                                    receiveTopic,
                                                                                    timeoutMs)

                If Not String.IsNullOrWhiteSpace(response) Then
                    Dim parts = response.Split(";"c)

                    If parts.Length >= 3 AndAlso parts(0) = "1" Then
                        Dim rawValue = parts(2).Trim()

                        ' Conversion anwenden falls angegeben
                        Dim convertedValue = rawValue
                        If Not String.IsNullOrWhiteSpace(conversionType) Then
                            ' WICHTIG: Übergebe Factor und Offset an ConvertValue
                            convertedValue = ValueConversionService.ConvertValue(rawValue, conversionType, conversionFactor, conversionOffset)
                        End If

                        _valueCache(address) = convertedValue
                        Return convertedValue
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"[QuerySingleValue] Fehler bei {address}: {ex.Message}")
            End Try

            Return ""
        End Function

        Private Shared Function EvaluateHiddenCondition(condition As String, configValues As Dictionary(Of String, String)) As Boolean
            If String.IsNullOrWhiteSpace(condition) Then
                Return False
            End If

            Dim cleanCondition = condition
            If cleanCondition.StartsWith("HIDDEN:(", StringComparison.OrdinalIgnoreCase) Then
                cleanCondition = cleanCondition.Substring(8)
            End If
            If cleanCondition.EndsWith(")"c) Then
                cleanCondition = cleanCondition.Substring(0, cleanCondition.Length - 1)
            End If

            Dim evaluatedCondition = cleanCondition
            For Each kvp In configValues
                Dim pattern = $"\b{Regex.Escape(kvp.Key)}\b"
                evaluatedCondition = Regex.Replace(evaluatedCondition, pattern, $"""{kvp.Value}""", RegexOptions.IgnoreCase)
            Next

            If evaluatedCondition.Contains("="c) Then
                Try
                    Dim parts = evaluatedCondition.Split("="c, 2)
                    If parts.Length = 2 Then
                        Dim left = parts(0).Trim().Trim(""""c, "("c, ")"c).Trim()
                        Dim right = parts(1).Trim().Trim(""""c, "("c, ")"c).Trim()
                        Return String.Equals(left, right, StringComparison.OrdinalIgnoreCase)
                    End If
                Catch ex As Exception
                End Try
            End If

            Return False
        End Function

        ''' <summary>
        ''' Fragt Parameterwerte ab mit Conversion und Retry-Logik
        ''' </summary>
        Private Shared Async Function QueryParameterValuesAsync(
           device As DeviceNode,
        mqttService As MqttService,
               sendTopic As String,
               receiveTopic As String,
               timeoutMs As Integer,
          progressCallback As Action(Of String)
           ) As Task

            Dim allParams As New List(Of (param As ParameterNode, byteLength As Integer, conversion As String, factor As Double, offset As Double))()
            Dim skippedParams = 0

            For Each category In device.Categories
                For Each group In category.Groups
                    If group.IsVisible Then
                        For Each param In group.Parameters
                            If param.IsVisible AndAlso Not String.IsNullOrWhiteSpace(param.Address) Then
                                ' WICHTIG: Verwende ByteLength, Conversion, Factor und Offset aus ParameterNode
                                allParams.Add((param, param.ByteLength, param.Conversion, param.ConversionFactor, param.ConversionOffset))
                            ElseIf Not param.IsVisible Then
                                skippedParams += 1
                                param.CurrentValue = "[HIDDEN]"
                            End If
                        Next
                    Else
                        For Each param In group.Parameters
                            skippedParams += 1
                            param.CurrentValue = "[HIDDEN]"
                        Next
                    End If
                Next
            Next

            Dim totalParams = allParams.Count
            Dim queriedParams = 0
            Dim failedParams As New List(Of (param As ParameterNode, byteLength As Integer, conversion As String, factor As Double, offset As Double))()

            progressCallback?.Invoke($"Abfrage Runde 1: 0/{totalParams} Parameter...")

            For Each item In allParams

                Dim param_Address_value As String = Nothing
                ' Cache-Check zuerst
                If _valueCache.TryGetValue(item.param.Address, param_Address_value) Then
                    item.param.CurrentValue = param_Address_value
                    queriedParams += 1
                    If queriedParams Mod 10 = 0 OrElse queriedParams = totalParams Then
                        progressCallback?.Invoke($"Abfrage Runde 1: {queriedParams}/{totalParams} Parameter...")
                    End If
                    Continue For
                End If

                Try
                    Dim QuerySingleValueAsync_value =
                        Await QuerySingleValueAsync(item.param.Address,
                                                    mqttService,
                                                    sendTopic,
                                                    receiveTopic,
                                                    timeoutMs,
                                                    item.byteLength,
                                                    item.conversion,
                                                    item.factor,
                                                    item.offset)

                    If Not String.IsNullOrWhiteSpace(QuerySingleValueAsync_value) Then
                        item.param.CurrentValue = QuerySingleValueAsync_value
                    Else
                        failedParams.Add(item)
                        item.param.CurrentValue = "[WAIT]"
                    End If

                    queriedParams += 1
                    If queriedParams Mod 10 = 0 OrElse queriedParams = totalParams Then
                        progressCallback?.Invoke($"Abfrage Runde 1: {queriedParams}/{totalParams} Parameter...")
                    End If

                Catch ex As Exception
                    failedParams.Add(item)
                    item.param.CurrentValue = "[ERR]"
                    Debug.WriteLine(ex.Message)
                End Try
            Next

            ' Retry-Logik
            Dim retryAttempt = 1
            While failedParams.Count > 0 AndAlso retryAttempt <= 2
                progressCallback?.Invoke($"Wiederhole fehlgeschlagene Abfragen (Versuch {retryAttempt + 1}/3): {failedParams.Count} Parameter...")

                Dim currentFailed = failedParams.ToList()
                failedParams.Clear()

                Dim retryCount = 0
                For Each item In currentFailed
                    Try
                        Dim retryTimeout = timeoutMs + (retryAttempt * 1000)
                        Dim value = Await QuerySingleValueAsync(item.param.Address,
                                                                mqttService,
                                                                sendTopic,
                                                                receiveTopic,
                                                                retryTimeout,
                                                                item.byteLength,
                                                                item.conversion,
                                                                item.factor,
                                                                item.offset)

                        If Not String.IsNullOrWhiteSpace(value) Then
                            item.param.CurrentValue = value
                        Else
                            failedParams.Add(item)
                            item.param.CurrentValue = $"[WAIT-{retryAttempt + 1}]"
                        End If

                        retryCount += 1
                        If retryCount Mod 5 = 0 OrElse retryCount = currentFailed.Count Then
                            progressCallback?.Invoke($"Wiederhole (Versuch {retryAttempt + 1}/3): {retryCount}/{currentFailed.Count} Parameter...")
                        End If

                    Catch ex As Exception
                        failedParams.Add(item)
                        item.param.CurrentValue = $"[ERR-{retryAttempt + 1}]"
                    End Try
                Next

                retryAttempt += 1
            End While

            Dim successCount = allParams.Count - failedParams.Count
            If failedParams.Count > 0 Then
                progressCallback?.Invoke($"Abfrage abgeschlossen: {successCount}/{totalParams} erfolgreich, {failedParams.Count} fehlgeschlagen, {skippedParams} übersprungen")

                For Each item In failedParams
                    item.param.CurrentValue = "N/A"
                Next
            Else
                progressCallback?.Invoke($"Abfrage abgeschlossen: {totalParams}/{totalParams} erfolgreich, {skippedParams} übersprungen")
            End If

        End Function
    End Class
End Namespace
