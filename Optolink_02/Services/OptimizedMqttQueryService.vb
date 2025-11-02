Imports System.Text.RegularExpressions
Imports Optolink_02.Domain

Namespace Services
    ''' <summary>
    ''' Optimierter Service f�r MQTT-Abfragen mit verbesserter Reihenfolge:
    ''' 1. Enum-Werte sammeln und abfragen
    ''' 2. HIDDEN-Conditions nach gemeinsamen Werten durchsuchen
    ''' 3. Gruppen-HIDDEN-Conditions auswerten
    ''' 4. Sichtbare Parameter abfragen (nur R und R/W)
    ''' 5. Werte konvertieren
    ''' </summary>
    Public NotInheritable Class OptimizedMqttQueryService
        Private Sub New()
        End Sub

        ' Cache f�r bereits abgefragte Werte
        Private Shared ReadOnly _valueCache As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>
        ''' Hauptmethode: F�hrt optimierte hierarchische Abfrage durch
        ''' </summary>
        Public Shared Async Function QueryDevHierarchyAsync(
                                                           device As DeviceNode,
                                                           mqttService As MqttService,
                                                           sendTopic As String,
                                                           receiveTopic As String,
                                                           Optional timeoutMs As Integer = 3000,
                                                           Optional progressCallback As Action(Of String) _
                                                           = Nothing) As Task(Of DeviceNode)

            If device Is Nothing OrElse mqttService Is Nothing OrElse Not mqttService.IsConnected Then
                Return device
            End If

            ' Cache leeren
            _valueCache.Clear()

            Try
                ' ============================================================
                ' PHASE 1: ENUM-WERTE SAMMELN UND ABFRAGEN
                ' ============================================================
                progressCallback?.Invoke("Phase 1: Sammle Enum-Werte...")

                Dim enumValues = Await QueryEnumValuesAsync(device, mqttService, sendTopic, receiveTopic, timeoutMs, progressCallback)

                Debug.WriteLine($"[OPTIMIZED] Phase 1: {enumValues.Count} Enum-Werte abgefragt")

                ' ============================================================
                ' PHASE 2: HIDDEN-CONDITIONS SAMMELN
                ' ============================================================
                progressCallback?.Invoke("Phase 2: Analysiere HIDDEN-Conditions...")

                Dim allHiddenConditions As New List(Of String)()

                ' Sammle Gruppen-Conditions
                For Each category In device.Categories
                    For Each group In category.Groups
                        If Not String.IsNullOrWhiteSpace(group.HiddenCondition) Then
                            allHiddenConditions.Add(group.HiddenCondition)
                            Debug.WriteLine($"[CONDITION GROUP] {group.GroupName}: {group.HiddenCondition}")
                        End If
                    Next
                Next

                ' Sammle Parameter-Conditions
                For Each category In device.Categories
                    For Each group In category.Groups
                        For Each param In group.Parameters
                            If Not String.IsNullOrWhiteSpace(param.HiddenCondition) Then
                                allHiddenConditions.Add(param.HiddenCondition)
                                Debug.WriteLine($"[CONDITION PARAM] {param.ParameterName}: {param.HiddenCondition}")
                            End If
                        Next
                    Next
                Next

                Debug.WriteLine($"[OPTIMIZED] Phase 2: {allHiddenConditions.Count} HIDDEN-Conditions insgesamt gefunden")

                ' Extrahiere ben�tigte Config-Werte (Deduplizierung!)
                Dim requiredConfigKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each condition In allHiddenConditions
                    ExtractConfigKeysFromCondition(condition, requiredConfigKeys)
                Next

                Debug.WriteLine($"[OPTIMIZED] Phase 2: {requiredConfigKeys.Count} eindeutige Config-Werte ben�tigt")
                Debug.WriteLine($"[OPTIMIZED] Config-Keys: {String.Join(", ",
                                                                        requiredConfigKeys.OrderBy(Function(k) k))}")

                ' ============================================================
                ' PHASE 3: CONFIG-WERTE ABFRAGEN (dedupliziert!)
                ' ============================================================
                progressCallback?.Invoke($"Phase 3: Frage {requiredConfigKeys.Count} Config-Werte ab...")

                Dim configValues = Await QueryConfigValuesAsync(
             requiredConfigKeys.ToList(),
      mqttService,
              sendTopic,
         receiveTopic,
         timeoutMs,
                    progressCallback)

                ' Kombiniere mit Enum-Werten
                For Each kvp In enumValues
                    ' TryAdd vermeidet doppelte Lookup
                    Dim unused = configValues.TryAdd(kvp.Key, kvp.Value)
                Next

                Debug.WriteLine($"[OPTIMIZED] Phase 3: {configValues.Count} Config-Werte verf�gbar")

                ' ============================================================
                ' PHASE 4: GRUPPEN-HIDDEN-CONDITIONS AUSWERTEN
                ' ============================================================
                progressCallback?.Invoke("Phase 4: Bewerte Gruppen-Sichtbarkeit...")

                Dim hiddenGroupCount = 0
                For Each category In device.Categories
                    For Each group In category.Groups
                        If Not String.IsNullOrWhiteSpace(group.HiddenCondition) Then
                            group.IsVisible = Not EvaluateHiddenCondition(group.HiddenCondition, configValues)
                            If Not group.IsVisible Then
                                hiddenGroupCount += 1
                            End If
                        Else
                            group.IsVisible = True
                        End If
                    Next
                Next

                Debug.WriteLine($"[OPTIMIZED] Phase 4: {hiddenGroupCount} Gruppen ausgeblendet")

                ' ============================================================
                ' PHASE 5: PARAMETER-HIDDEN-CONDITIONS AUSWERTEN
                ' ============================================================
                progressCallback?.Invoke("Phase 5: Bewerte Parameter-Sichtbarkeit...")

                Dim hiddenParamCount = 0
                For Each category In device.Categories
                    For Each group In category.Groups
                        For Each param In group.Parameters
                            If Not String.IsNullOrWhiteSpace(param.HiddenCondition) Then
                                param.IsVisible = Not EvaluateHiddenCondition(param.HiddenCondition, configValues)
                                If Not param.IsVisible Then
                                    hiddenParamCount += 1
                                End If
                            Else
                                param.IsVisible = True
                            End If
                        Next
                    Next
                Next

                Debug.WriteLine($"[OPTIMIZED] Phase 5: {hiddenParamCount} Parameter ausgeblendet")

                ' ============================================================
                ' PHASE 6: PARAMETER-WERTE ABFRAGEN (nur R und R/W)
                ' ============================================================
                progressCallback?.Invoke("Phase 6: Frage Parameter-Werte ab...")

                Await QueryVisibleParameterValuesAsync(
                   device,
               mqttService,
                 sendTopic,
                     receiveTopic,
              timeoutMs,
                  progressCallback)

                progressCallback?.Invoke("? Abfrage abgeschlossen")
                Return device

            Catch ex As Exception
                Debug.WriteLine($"[OPTIMIZED ERROR] {ex.Message}")
                progressCallback?.Invoke($"? Fehler: {ex.Message}")
                Return device
            End Try
        End Function

        ''' <summary>
        ''' Phase 1: Sammelt und fragt Enum-Werte ab (Parameter mit (Zahl) Beschreibung)
        ''' </summary>
        Private Shared Async Function QueryEnumValuesAsync(
                                                          device As DeviceNode,
                                                          mqttService As MqttService,
                                                          sendTopic As String,
                                                          receiveTopic As String,
                                                          timeoutMs As Integer,
                                                          progressCallback As Action(Of String)
                                                          ) As Task(Of Dictionary(Of String, String))

            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            ' WICHTIG: Dictionary statt List um Duplikate zu vermeiden!
            Dim enumParams As New Dictionary(Of String, ParameterNode)(StringComparer.OrdinalIgnoreCase)

            ' Sammle alle Enum-Parameter (erkennbar an "(Zahl) Beschreibung" im Namen)
            For Each category In device.Categories
                For Each group In category.Groups
                    For Each param In group.Parameters
                        ' Pr�fe ob Parameter-Name mit (Zahl) beginnt
                        If Not String.IsNullOrWhiteSpace(param.ParameterName) AndAlso
                      param.ParameterName.Trim().StartsWith("("c) Then

                            Dim match = Regex.Match(param.ParameterName, "^\((\d+)\)\s+(.+)$")
                            If match.Success Then
                                Dim enumKey = match.Groups(1).Value ' Die Zahl

                                ' TryAdd vermeidet doppelte Lookup
                                If enumParams.TryAdd(enumKey, param) Then
                                    Debug.WriteLine($"[ENUM] Gefunden: ({enumKey}) {param.ParameterName} @ {param.Address}")
                                Else
                                    ' Duplikat gefunden - logge es zur Information
                                    Debug.WriteLine($"[ENUM DUPLIKAT] �berspringe: ({enumKey}) {param.ParameterName} (bereits vorhanden: {enumParams(enumKey).ParameterName})")
                                End If
                            End If
                        End If
                    Next
                Next
            Next

            If enumParams.Count = 0 Then
                Return result
            End If

            progressCallback?.Invoke($"Phase 1: Frage {enumParams.Count} Enum-Werte ab...")
            Debug.WriteLine($"[ENUM] {enumParams.Count} eindeutige Enum-Parameter gefunden")

            ' Frage Enum-Werte ab
            Dim queryCount = 0
            For Each kvp In enumParams
                Try
                    If Not String.IsNullOrWhiteSpace(kvp.Value.Address) Then
                        Dim value = Await QuerySingleValueAsync(
                            kvp.Value.Address,
                            mqttService,
                            sendTopic,
                            receiveTopic,
                            timeoutMs,
                            kvp.Value.ByteLength,
                            kvp.Value.Conversion,
                            kvp.Value.ConversionFactor,
                            kvp.Value.ConversionOffset,
                            kvp.Value.DataType) ' NEU: DataType �bergeben

                        result(kvp.Key) = value
                        Debug.WriteLine($"[ENUM RESULT] ({kvp.Key}) = {value}")
                        queryCount += 1

                        If queryCount Mod 5 = 0 Then
                            progressCallback?.Invoke($"Phase 1: {queryCount}/{enumParams.Count} Enum-Werte...")
                        End If
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[ENUM ERROR] {kvp.Key}: {ex.Message}")
                End Try
            Next

            Return result
        End Function

        ''' <summary>
        ''' Extrahiert Config-Keys aus einer HIDDEN-Condition
        ''' </summary>
        Private Shared Sub ExtractConfigKeysFromCondition(condition As String, ByRef keys As HashSet(Of String))
            If String.IsNullOrWhiteSpace(condition) Then Return

            ' Bereinige Condition
            Dim cleaned = condition
            If cleaned.StartsWith("HIDDEN:(", StringComparison.OrdinalIgnoreCase) Then
                cleaned = cleaned.Substring(8)
            End If
            If cleaned.EndsWith(")"c) Then
                cleaned = cleaned.Substring(0, cleaned.Length - 1)
            End If

            ' Pattern: XX_Name oder XXX_Name (2-3 Hex-Ziffern gefolgt von Unterstrich und Name)
            Dim pattern = "\b([0-9A-F]{2,3}_[a-zA-Z0-9_]+)\b"
            Dim matches = Regex.Matches(cleaned, pattern, RegexOptions.IgnoreCase)

            Dim foundKeys As New List(Of String)()
            For Each match As Match In matches
                Dim key = match.Groups(1).Value

                ' HashSet.Add gibt true zur�ck wenn Element neu ist, false wenn bereits vorhanden
                If keys.Add(key) Then
                    foundKeys.Add(key)
                End If
            Next

            ' Debug-Log nur wenn neue Keys gefunden wurden
            If foundKeys.Count > 0 Then
                Debug.WriteLine($"[CONFIG KEYS] Condition: {condition}")
                Debug.WriteLine($"[CONFIG KEYS] Gefunden: {String.Join(", ", foundKeys)}")
            End If
        End Sub

        ''' <summary>
        ''' Fragt Config-Werte ab (mit Address-Mapping aus DB)
        ''' </summary>
        Private Shared Async Function QueryConfigValuesAsync(
                                                            keys As List(Of String),
                                                            mqttService As MqttService,
                                                            sendTopic As String,
                                                            receiveTopic As String,
                                                            timeoutMs As Integer,
                                                            progressCallback As Action(Of String)
                                                            ) As Task(Of Dictionary(Of String, String))

            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

            If keys Is Nothing OrElse keys.Count = 0 Then
                Return result
            End If

            ' Hole Address-Mapping aus Datenbank
            Dim addressMap = Await GetConfigAddressMapAsync(keys)
            Debug.WriteLine($"[CONFIG VALUES] Address-Mapping geladen: {addressMap.Count} von {keys.Count} Keys gefunden")

            Dim queryCount = 0
            Dim cacheHits = 0
            For Each key In keys
                ' TryGetValue vermeidet doppelte Lookup
                Dim address As String = Nothing
                If addressMap.TryGetValue(key, address) Then
                    ' Cache-Check mit TryGetValue
                    Dim cachedValue As String = Nothing
                    If _valueCache.TryGetValue(address, cachedValue) Then
                        result(key) = cachedValue
                        cacheHits += 1
                        Debug.WriteLine($"[CONFIG CACHE HIT] {key} @ {address} = {cachedValue}")
                        Continue For
                    End If

                    Try
                        Dim value = Await QuerySingleValueAsync(
                            address,
                            mqttService,
                            sendTopic,
                            receiveTopic,
                            timeoutMs,
                            2, ' Standard 2 Bytes f�r Config-Werte
                            Nothing, ' Keine Conversion f�r Config-Werte
                            1.0, ' Factor
                            0.0, ' Offset
                            Nothing) ' Kein DataType f�r Config-Werte (meist Enums/Strings)

                        result(key) = value
                        _valueCache(address) = value
                        Debug.WriteLine($"[CONFIG QUERY] {key} @ {address} = {value}")
                        queryCount += 1

                        If queryCount Mod 10 = 0 Then
                            progressCallback?.Invoke($"Phase 3: {queryCount}/{keys.Count} Config-Werte...")
                        End If
                    Catch ex As Exception
                        result(key) = ""
                        Debug.WriteLine($"[CONFIG ERROR] {key}: {ex.Message}")
                    End Try
                Else
                    result(key) = ""
                    Debug.WriteLine($"[CONFIG NO MAPPING] {key}: Keine Adresse in Datenbank gefunden")
                End If
            Next

            Debug.WriteLine($"[CONFIG VALUES] Abgeschlossen: {queryCount} abgefragt, {cacheHits} aus Cache, {keys.Count - queryCount - cacheHits} fehlgeschlagen")
            Return result
        End Function

        ''' <summary>
        ''' Holt Address-Mapping aus Datenbank
        ''' </summary>
        Private Shared Async Function GetConfigAddressMapAsync(
                                                              keys As List(Of String)) _
                                                              As Task(Of Dictionary(Of String, String))
            Dim result As New Dictionary(Of String, String)()

            Try
                Dim dt = Await SqlQueryService.GetConfigAddressMappingAsync(keys)
                For Each row As DataRow In dt.Rows
                    Dim key = Convert.ToString(row("ConfigKey"))
                    Dim address = Convert.ToString(row("HexAddress"))
                    If Not String.IsNullOrWhiteSpace(key) AndAlso Not String.IsNullOrWhiteSpace(address) Then
                        Dim unused = result.TryAdd(key, address)
                    End If
                Next
            Catch ex As Exception
                Debug.WriteLine($"[ADDRESS MAP ERROR] {ex.Message}")
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Wertet eine HIDDEN-Condition aus (verwendet EnhancedHiddenConditionEvaluator)
        ''' </summary>
        Private Shared Function EvaluateHiddenCondition(condition As String,
                                                        configValues As Dictionary(Of String,
                                                        String)) As Boolean
            ' Verwende den verbesserten Evaluator für korrekte OR/AND/NOT-Unterstützung
            Return EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
        End Function

        ''' <summary>
        ''' Fragt sichtbare Parameter-Werte ab (nur R und R/W, keine W)
        ''' </summary>
        Private Shared Async Function QueryVisibleParameterValuesAsync(device As DeviceNode,
                                                                       mqttService As MqttService,
                                                                       sendTopic As String,
                                                                       receiveTopic As String,
                                                                       timeoutMs As Integer,
                                                                       progressCallback As Action(Of String)) As Task

            Dim queryableParams As New List(Of ParameterNode)()
            Dim skippedCount = 0
            Dim writeOnlyCount = 0

            ' Sammle nur R und R/W Parameter (keine W!)
            For Each category In device.Categories
                For Each group In category.Groups
                    If group.IsVisible Then
                        For Each param In group.Parameters
                            If param.IsVisible AndAlso Not String.IsNullOrWhiteSpace(param.Address) Then
                                ' WICHTIG: Verwende ReadWrite-Property (nicht DataType!)
                                ' ReadWrite kann "R", "R/W" oder "W" sein
                                Dim readWrite = If(param.ReadWrite, "").Trim().ToUpperInvariant()

                                ' Debug-Log f�r erste 5 Parameter
                                If queryableParams.Count + writeOnlyCount < 5 Then
                                    Debug.WriteLine($"[DATATYPE CHECK] {param.ParameterName}: ReadWrite='{param.ReadWrite}', DataType='{param.DataType}'")
                                End If

                                If readWrite = "R" OrElse readWrite = "R/W" OrElse readWrite = "" Then
                                    ' Leerer ReadWrite gilt auch als lesbar (Fallback)
                                    queryableParams.Add(param)
                                Else
                                    param.CurrentValue = "[W]" ' Write-Only
                                    writeOnlyCount += 1
                                    skippedCount += 1
                                    Debug.WriteLine($"[WRITE-ONLY SKIP] {param.ParameterName} @ {param.Address}")
                                End If
                            ElseIf Not param.IsVisible Then
                                param.CurrentValue = "[HIDDEN]"
                                skippedCount += 1
                            End If
                        Next
                    Else
                        ' Ganze Gruppe ist ausgeblendet
                        For Each param In group.Parameters
                            param.CurrentValue = "[HIDDEN]"
                            skippedCount += 1
                        Next
                    End If
                Next
            Next

            Debug.WriteLine($"[QUERY PARAMS] Gesamt: {queryableParams.Count} lesbar, {writeOnlyCount} write-only, {skippedCount - writeOnlyCount} hidden")

            Dim totalCount = queryableParams.Count
            Dim queryCount = 0

            progressCallback?.Invoke($"Phase 6: 0/{totalCount} Parameter...")

            For Each param In queryableParams
                ' Cache-Check mit TryGetValue
                Dim cachedValue As String = Nothing
                If _valueCache.TryGetValue(param.Address, cachedValue) Then
                    param.CurrentValue = cachedValue
                    queryCount += 1
                    Continue For
                End If

                Try
                    Dim value = Await QuerySingleValueAsync(
                        param.Address,
                        mqttService,
                        sendTopic,
                        receiveTopic,
                        timeoutMs,
                        param.ByteLength,
                        param.Conversion,
                        param.ConversionFactor,
                        param.ConversionOffset,
                        param.DataType) ' NEU: DataType �bergeben

                    param.CurrentValue = If(String.IsNullOrWhiteSpace(value), "N/A", value)
                    queryCount += 1

                    If queryCount Mod 10 = 0 OrElse queryCount = totalCount Then
                        progressCallback?.Invoke($"Phase 6: {queryCount}/{totalCount} Parameter...")
                    End If
                Catch ex As Exception
                    param.CurrentValue = "[ERR]"
                    Debug.WriteLine($"[PARAM ERROR] {param.Address}: {ex.Message}")
                End Try
            Next

            progressCallback?.Invoke($"Phase 6: {queryCount}/{totalCount} Parameter abgefragt, {skippedCount} �bersprungen ({writeOnlyCount} write-only)")
        End Function

        ''' <summary>
        ''' Fragt einen einzelnen Wert ab
        ''' </summary>
        Private Shared Async Function QuerySingleValueAsync(address As String,
                                                            mqttService As MqttService,
                                                            sendTopic As String,
                                                            receiveTopic As String,
                                                            timeoutMs As Integer,
                                                            Optional byteLength As Integer = 2,
                                                            Optional conversionType As String = Nothing,
                                                            Optional conversionFactor As Double = 1.0,
                                                            Optional conversionOffset As Double = 0.0,
                                                            Optional dataType As String = Nothing
                                                            ) As Task(Of String)

            If String.IsNullOrWhiteSpace(address) Then Return ""

            ' Cache-Check mit TryGetValue
            Dim cachedValue As String = Nothing
            If _valueCache.TryGetValue(address, cachedValue) Then
                Return cachedValue
            End If

            ' WICHTIG: Verwende MqttCommandGenerator um korrektes Format mit scale/signed zu erhalten
            Dim command = Services.MqttCommandGenerator.GenerateReadCommand(address,
                                                                            byteLength.ToString(),
                                                                            conversionType,
                                                                            dataType)

            ' Fallback wenn Generator nichts zur�ckgibt
            If String.IsNullOrWhiteSpace(command) Then
                command = $"read;{address};{byteLength}"
            End If

            Debug.WriteLine($"[QUERY-SINGLE] MQTT Command: {command}")

            Try
                Dim response =
                    Await mqttService.SendCommandAndWaitForResponseAsync(command,
                                                                         sendTopic,
                                                                         receiveTopic,
                                                                         timeoutMs)

                If Not String.IsNullOrWhiteSpace(response) Then
                    Dim parts = response.Split(";"c)

                    If parts.Length >= 3 AndAlso parts(0) = "1" Then
                        Dim rawValue = parts(2).Trim()

                        ' WICHTIG: Optolink-Splitter liefert bereits konvertierte Werte!
                        ' Keine weitere Konvertierung n�tig - direkt verwenden
                        Dim convertedValue = rawValue

                        Debug.WriteLine($"[QUERY-SINGLE] Response value: {convertedValue}")

                        _valueCache(address) = convertedValue
                        Return convertedValue
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine($"[QUERY ERROR] {address}: {ex.Message}")
            End Try

            Return ""
        End Function
    End Class
End Namespace