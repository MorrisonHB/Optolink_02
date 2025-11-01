Imports System.Text.RegularExpressions

Namespace Services
    Public Class HiddenConditionFilter

        Public Class HiddenCondition
            Public Property ParameterId As String
            Public Property ParameterName As String
            Public Property CompareValue As String
            Public Property OperatorType As String

            Public Overrides Function ToString() As String
                Return $"({ParameterId}) {ParameterName} {OperatorType} ""{CompareValue}"""
            End Function
        End Class

        Public Class ConditionGroup
            Public Property Conditions As New List(Of HiddenCondition)
            Public Property LogicalOperator As String
            Public Property IsNegated As Boolean

            Public Overrides Function ToString() As String
                Dim condStr = String.Join($" {LogicalOperator} ", Conditions.Select(Function(c) c.ToString()))
                Return $"HIDDEN: {condStr}"
            End Function
        End Class

        Public Shared Function ParseHiddenString(hiddenString As String) As ConditionGroup
            If String.IsNullOrWhiteSpace(hiddenString) OrElse Not hiddenString.Contains("HIDDEN:") Then Return Nothing

            Dim group As New ConditionGroup With {.LogicalOperator = "OR", .IsNegated = True}
            Dim hiddenMatch = Regex.Match(hiddenString, "HIDDEN:\s*\((.*)\)", RegexOptions.Singleline)
            If Not hiddenMatch.Success Then Return Nothing

            Dim conditionsText = hiddenMatch.Groups(1).Value
            If conditionsText.Contains(" AND ") Then
                group.LogicalOperator = "AND"
            ElseIf conditionsText.Contains(" OR ") Then
                group.LogicalOperator = "OR"
            End If

            Dim conditionParts = SplitByLogicalOperator(conditionsText, group.LogicalOperator)
            For Each part In conditionParts
                Dim condition = ParseSingleCondition(part.Trim())
                If condition IsNot Nothing Then group.Conditions.Add(condition)
            Next

            If group.Conditions.Count = 0 Then Return Nothing
            Return group
        End Function

        Private Shared Function SplitByLogicalOperator(text As String, logicalOp As String) As List(Of String)
            Dim parts As New List(Of String)
            Dim currentPart As New Text.StringBuilder()
            Dim inQuotes As Boolean = False
            Dim i As Integer = 0

            While i < text.Length
                Dim c = text(i)
                If c = """"c Then
                    inQuotes = Not inQuotes
                    Dim unused3 = currentPart.Append(c)
                    i += 1
                ElseIf Not inQuotes AndAlso i + logicalOp.Length <= text.Length Then
                    Dim substring = text.Substring(i, Math.Min(logicalOp.Length + 2, text.Length - i))
                    If substring.Contains($" {logicalOp} ") Then
                        parts.Add(currentPart.ToString())
                        Dim unused2 = currentPart.Clear()
                        i += logicalOp.Length + 2
                        Continue While
                    Else
                        Dim unused1 = currentPart.Append(c)
                        i += 1
                    End If
                Else
                    Dim unused = currentPart.Append(c)
                    i += 1
                End If
            End While

            If currentPart.Length > 0 Then parts.Add(currentPart.ToString())
            Return parts
        End Function

        Private Shared Function ParseSingleCondition(conditionText As String) As HiddenCondition
            Dim pattern = "\""?\(([^)]+)\)\s*([^""]+?)\""?\s*(=|!=|<>)\s*\""([^""]*)\"""
            Dim match = Regex.Match(conditionText, pattern)
            Return If(match.Success,
                New HiddenCondition With {
                .ParameterId = match.Groups(1).Value.Trim(),
                .ParameterName = match.Groups(2).Value.Trim(),
                .OperatorType = If(match.Groups(3).Value = "<>", "!=", match.Groups(3).Value),
                .CompareValue = match.Groups(4).Value.Trim()
            }, Nothing)
        End Function

        Public Shared Function ShouldHideParameter(group As ConditionGroup,
                                                   deviceValues As Dictionary(Of String, String),
                                                   Optional sourceTable As DataTable = Nothing) As Boolean
            If group Is Nothing OrElse group.Conditions.Count = 0 Then Return False

            Dim results As New List(Of Boolean)
            For Each condition In group.Conditions
                ' TryGetValue vermeidet doppelte Lookup
                Dim currentValue As String = Nothing
                If deviceValues.TryGetValue(condition.ParameterId, currentValue) Then
                    Dim resolvedExpectedValue = condition.CompareValue
                    If sourceTable IsNot Nothing Then
                        Dim numericFromEnum = ResolveEnumTextToNumeric(sourceTable, condition.ParameterId, condition.CompareValue)
                        If numericFromEnum IsNot Nothing Then
                            resolvedExpectedValue = numericFromEnum
                        End If
                    End If
                    Dim isMatch = CompareValues(currentValue, resolvedExpectedValue, condition.OperatorType)
                    results.Add(isMatch)
                Else
                    results.Add(False)
                End If
            Next

            Dim finalResult = If(group.LogicalOperator = "AND", results.All(Function(r) r), results.Any(Function(r) r))
            Return finalResult
        End Function

        Private Shared Function ResolveEnumTextToNumeric(sourceTable As DataTable,
                                                    parameterId As String,
                                                    enumText As String) As String
            If sourceTable Is Nothing OrElse String.IsNullOrWhiteSpace(enumText) Then Return Nothing

            Dim searchPattern = $"({parameterId})"
            Dim parameterRows =
                sourceTable.AsEnumerable().Where(Function(r)
                                                     Return sourceTable.Columns.Contains("Parameter") AndAlso
                                                     Convert.ToString(r("Parameter")).StartsWith(searchPattern,
                                                                                                 StringComparison.OrdinalIgnoreCase)
                                                 End Function).ToList()
            If parameterRows.Count = 0 Then Return Nothing
            Dim row = parameterRows.First()
            If Not sourceTable.Columns.Contains("Values") OrElse
                Not sourceTable.Columns.Contains("Descriptions") Then Return Nothing

            Dim values = Convert.ToString(row("Values"))
            Dim descriptions = Convert.ToString(row("Descriptions"))
            If String.IsNullOrWhiteSpace(values) OrElse
                String.IsNullOrWhiteSpace(descriptions) Then Return Nothing

            Dim valueList = values.Split(","c).Select(Function(v)
                                                          Return v.Trim()
                                                      End Function).ToArray()
            Dim descList = descriptions.Split(","c).Select(Function(d)
                                                               Return d.Trim()
                                                           End Function).ToArray()
            If valueList.Length <> descList.Length Then Return Nothing
            For i As Integer = 0 To descList.Length - 1
                If descList(i).Equals(enumText, StringComparison.OrdinalIgnoreCase) Then Return valueList(i)
            Next
            Return Nothing
        End Function

        Private Shared Function CompareValues(actualValue As String,
                                         expectedValue As String,
                                         operatorType As String) As Boolean
            actualValue = If(actualValue?.Trim(), "")
            expectedValue = If(expectedValue?.Trim(), "")

            Dim expectedNumeric As String = expectedValue
            If expectedValue.Contains(" "c) Then
                expectedNumeric = expectedValue.Split(" "c)(0).Trim()
            End If

            Dim exactMatch = actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase)
            Dim numericMatch As Boolean = False
            Dim actualInt, expectedInt As Integer

            If Integer.TryParse(actualValue, actualInt) AndAlso Integer.TryParse(expectedNumeric, expectedInt) Then
                numericMatch = actualInt = expectedInt
            End If

            Select Case operatorType
                Case "="
                    Return exactMatch OrElse numericMatch
                Case "!="
                    Return Not exactMatch AndAlso Not numericMatch
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function ExtractRequiredParameterIds(hiddenStrings As IEnumerable(Of String)) As HashSet(Of String)
            Dim parameterIds As New HashSet(Of String)
            For Each hiddenString In hiddenStrings
                If String.IsNullOrWhiteSpace(hiddenString) Then Continue For
                Dim group = ParseHiddenString(hiddenString)
                If group IsNot Nothing Then
                    For Each condition In group.Conditions
                        If Not String.IsNullOrWhiteSpace(condition.ParameterId) Then
                            Dim unused = parameterIds.Add(condition.ParameterId)
                        End If
                    Next
                End If
            Next

            Dim configParams As String() = {"7000", "00", "76", "7010"}
            For Each param In configParams
                Dim unused = parameterIds.Add(param)
            Next
            Return parameterIds
        End Function

        Public Shared Function FilterByHiddenConditions(sourceTable As DataTable,
                                                        deviceValues As Dictionary(Of String, String)) As DataTable
            If sourceTable Is Nothing Then Return Nothing
            Dim filteredTable = sourceTable.Clone()
            If Not sourceTable.Columns.Contains("Condition") Then
                For Each row As DataRow In sourceTable.Rows
                    filteredTable.ImportRow(row)
                Next
                Return filteredTable
            End If

            For Each row As DataRow In sourceTable.Rows
                Dim conditionText = Convert.ToString(row("Condition"))
                Dim group = ParseHiddenString(conditionText)
                Dim shouldShow As Boolean
                If group Is Nothing Then
                    shouldShow = True
                Else
                    Dim shouldHide = ShouldHideParameter(group, deviceValues, sourceTable)
                    shouldShow = Not shouldHide
                End If
                If shouldShow Then filteredTable.ImportRow(row)
            Next
            Return filteredTable
        End Function

        Public Shared Function CreateDeviceValuesDictionary(deviceData As DataTable,
                                                            idColumnName As String,
                                                            valueColumnName As String) As Dictionary(Of String, String)
            Dim values As New Dictionary(Of String, String)
            If deviceData Is Nothing Then Return values
            If Not deviceData.Columns.Contains(idColumnName) OrElse
           Not deviceData.Columns.Contains(valueColumnName) Then Return values

            For Each row As DataRow In deviceData.Rows
                Dim id = Convert.ToString(row(idColumnName))
                Dim value = Convert.ToString(row(valueColumnName))
                If Not String.IsNullOrWhiteSpace(id) Then
                    Dim idMatch = Regex.Match(id, "\(([^)]+)\)")
                    If idMatch.Success Then id = idMatch.Groups(1).Value
                    values(id) = value
                End If
            Next
            Return values
        End Function

        Public Shared Async Function FilterByHiddenConditionsAsync(sourceTable As DataTable,
                                                                   mqttService As MqttService,
                                                                   sendTopic As String,
                                                                   receiveTopic As String,
                                                                   Optional timeoutMs As Integer = 3000) As Task(Of DataTable)
            If sourceTable Is Nothing Then Return Nothing

            Dim hiddenStrings =
                sourceTable.AsEnumerable().Select(Function(r)
                                                      Return Convert.ToString(r("Condition"))
                                                  End Function).Where(Function(s)
                                                                          Return Not String.IsNullOrWhiteSpace(s) AndAlso
                                                                          s.Contains("HIDDEN:")
                                                                      End Function)
            Dim requiredIds = ExtractRequiredParameterIds(hiddenStrings)
            If requiredIds.Count = 0 Then
                Return sourceTable.Copy()
            End If

            Dim deviceValues As Dictionary(Of String, String)
            Try
                deviceValues = Await DeviceValueRetriever.RetrieveDeviceValuesAsync(mqttService,
                                                                                    sourceTable,
                                                                                    requiredIds,
                                                                                    sendTopic,
                                                                                    receiveTopic,
                                                                                    timeoutMs)
            Catch ex As Exception
                deviceValues = New Dictionary(Of String, String)()
            End Try

            Return FilterByHiddenConditions(sourceTable, deviceValues)
        End Function

        ''' <summary>
        ''' Fragt automatisch alle Konfigurations-Parameter der Anlagenausstattung ab.
        ''' </summary>
        Public Shared Async Function QueryDeviceConfigurationAsync(sourceTable As DataTable,
                                                                   mqttService As MqttService,
                                                                   sendTopic As String,
                                                                   receiveTopic As String,
         Optional timeoutMs As Integer = 3000) As Task(Of Dictionary(Of String, String))

            If sourceTable Is Nothing Then
                Return New Dictionary(Of String, String)()
            End If

            If Not sourceTable.Columns.Contains("Gruppe") Then
                Return New Dictionary(Of String, String)()
            End If

            Dim configGroupNames As String() = {
                "Anlagenausstattung",
                "Allgemein",
                "Gerätedaten",
                "Codierung 1",
                "Codierung 2",
                "Anlagendefinition",
                "Codierung_1",
                "Codierung_2"}

            Dim configParams = sourceTable.AsEnumerable().
                Where(Function(r)
                          Dim gruppe = Convert.ToString(r("Gruppe"))
                          Return configGroupNames.Any(Function(g)
                                                          Return Not String.IsNullOrWhiteSpace(gruppe) AndAlso
                                                          gruppe.Equals(g, StringComparison.OrdinalIgnoreCase)
                                                      End Function)
                      End Function).ToList()

            If configParams.Count = 0 Then
                Return New Dictionary(Of String, String)()
            End If

            Dim mqttCommands As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

            For Each row In configParams
                Dim param = Convert.ToString(row("Parameter"))
                If String.IsNullOrWhiteSpace(param) Then Continue For

                Dim id As String = Nothing
                Dim address As String = Nothing
                Dim byteLength As String = Nothing

                Dim idMatch = Regex.Match(param, "^K([0-9A-Fa-f]{2})_", RegexOptions.IgnoreCase)
                If idMatch.Success Then
                    id = idMatch.Groups(1).Value.Trim().ToUpper()
                End If

                If row.Table.Columns.Contains("Address") Then
                    address = Convert.ToString(row("Address"))
                End If

                If row.Table.Columns.Contains("ByteLength") Then
                    byteLength = Convert.ToString(row("ByteLength"))
                End If

                If Not String.IsNullOrWhiteSpace(id) AndAlso
                    Not String.IsNullOrWhiteSpace(address) AndAlso
                    Not String.IsNullOrWhiteSpace(byteLength) AndAlso
                    Not mqttCommands.ContainsKey(id) Then
                    Dim mqttRead = $"read;{address};{byteLength}"
                    mqttCommands(id) = mqttRead
                End If
            Next

            If mqttCommands.Count = 0 Then
                Return New Dictionary(Of String, String)()
            End If

            Dim deviceValues As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

            For Each kvp In mqttCommands
                Dim parameterId = kvp.Key
                Dim mqttCommand = kvp.Value

                Try
                    Dim response = Await mqttService.RequestResponseAsync(sendTopic,
                                                                          mqttCommand,
                                                                          receiveTopic,
                                                                          timeoutMs)
                    Dim parts = response.Split(";"c)
                    If parts.Length >= 3 Then
                        Dim returnCode = parts(0).Trim()
                        If returnCode.Equals("01", StringComparison.OrdinalIgnoreCase) OrElse
                            returnCode.Equals("1", StringComparison.OrdinalIgnoreCase) Then
                            Dim value = parts(2).Trim()

                            If value.Contains(" "c) Then
                                value = value.Split(" "c)(0)
                            End If

                            deviceValues(parameterId) = value
                        End If
                    End If
                Catch ex As TimeoutException
                    ' Silent error handling
                Catch ex As Exception
                    ' Silent error handling
                End Try
            Next

            Return deviceValues
        End Function

    End Class
End Namespace