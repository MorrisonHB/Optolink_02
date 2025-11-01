Namespace Services
    ''' <summary>
    ''' Service zum Abrufen von Gerätewerten über MQTT für die Auswertung von HIDDEN-Conditions.
    ''' Verwendet die generierten MQTT_Read-Kommandos aus der DataTable.
    ''' </summary>
    Public Class DeviceValueRetriever

        ''' <summary>
        ''' Fragt alle benötigten Parameter-IDs über MQTT ab und gibt ein Dictionary mit den Werten zurück
        ''' </summary>
        ''' <param name="mqttService">Der aktive MQTT-Service</param>
        ''' <param name="dataTable">DataTable mit allen DeviceEvents (inkl. MQTT_Read-Spalte)</param>
        ''' <param name="requiredParameterIds">Liste der benötigten Parameter-IDs (z.B. "00", "2E")</param>
        ''' <param name="sendTopic">MQTT Topic zum Senden der Abfragen (z.B. "viessmann/cmnd")</param>
        ''' <param name="receiveTopic">MQTT Topic zum Empfangen der Antworten (z.B. "viessmann/status")</param>
        ''' <param name="timeoutMs">Timeout pro Abfrage in Millisekunden</param>
        ''' <returns>Dictionary mit Parameter-ID als Key und Wert als Value</returns>
        Public Shared Async Function RetrieveDeviceValuesAsync(mqttService As MqttService,
                                                               dataTable As DataTable,
                                                               requiredParameterIds As HashSet(Of String),
                                                               sendTopic As String,
                                                               receiveTopic As String,
                                                               Optional timeoutMs As Integer = 3000) As Task(Of Dictionary(Of String,
                                                               String))

            ArgumentNullException.ThrowIfNull(mqttService)

            If Not mqttService.IsConnected Then
                Throw New InvalidOperationException("MQTT ist nicht verbunden")
            End If

            If dataTable Is Nothing OrElse dataTable.Rows.Count = 0 Then
                Throw New ArgumentException("DataTable ist leer", NameOf(dataTable))
            End If

            If requiredParameterIds Is Nothing OrElse requiredParameterIds.Count = 0 Then
                Return New Dictionary(Of String, String)()
            End If

            ' Prüfe ob MQTT_Read Spalte existiert
            If Not dataTable.Columns.Contains("MQTT_Read") Then
                Throw New InvalidOperationException("DataTable enthält keine MQTT_Read-Spalte. " &
              "Bitte zuerst MqttCommandGenerator.AddMqttCommandColumns() aufrufen.")
            End If

            Dim results As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

            ' Für jede benötigte Parameter-ID
            For Each parameterId In requiredParameterIds
                Try
                    ' Finde die entsprechende Zeile in der DataTable
                    ' Parameter-ID ist z.B. "(00)" -> suche nach EventTypeId oder Codierung
                    Dim parameterRow = FindParameterRow(dataTable, parameterId)

                    If parameterRow Is Nothing Then
                        Continue For
                    End If

                    ' Hole das MQTT_Read-Kommando
                    Dim mqttReadCommand = Convert.ToString(parameterRow("MQTT_Read"))
                    If String.IsNullOrWhiteSpace(mqttReadCommand) Then
                        Continue For
                    End If

                    ' Sende MQTT-Kommando und warte auf Antwort
                    Dim response = Await mqttService.RequestResponseAsync(
                      sendTopic, mqttReadCommand, receiveTopic, timeoutMs)

                    ' Parse die Antwort: Format ist "return_code;address;value"
                    Dim value = ParseMqttResponse(response)

                    If value IsNot Nothing Then
                        results(parameterId) = value
                    End If

                Catch ex As TimeoutException
                    ' Timeout - continue with next parameter
                Catch ex As Exception
                    ' Error - continue with next parameter
                End Try
            Next

            Return results
        End Function

        ''' <summary>
        ''' Findet die DataRow für eine Parameter-ID
        ''' </summary>
        ''' <param name="dataTable">DataTable mit DeviceEvents</param>
        ''' <param name="parameterId">Parameter-ID ohne Klammern (z.B. "00", "2E", "7010")</param>
        ''' <returns>DataRow oder Nothing wenn nicht gefunden</returns>
        Private Shared Function FindParameterRow(dataTable As DataTable, parameterId As String) As DataRow
            ' Parameter-ID kann verschiedene Formate haben:
            ' 1. Als Address direkt (z.B. "0x7700" f?r Parameter "(00)")
            ' 2. Im Parameter-Namen mit Klammern (z.B. "(00) Heizkreis-Warmwasserschema")
            ' 3. Im Parameter-Namen mit K-Prefix (z.B. "K00_KonfiAnlagenschemaGWG_W~0x7700")
            ' 4. L?ngere IDs wie "(7010)", "(7016)" etc.

            ' Erweiterte Address-Mappings von Parameter-IDs zu Adressen
            ' HINWEIS: Diese Mappings sind ger?te-spezifisch!
            ' Verschiedene Ger?te-Familien verwenden unterschiedliche Adressen:
            ' - Vitodens/VScotHO1: 0x7700, 0x572E etc.
            ' - Vitocal/CU401B_S: 0x7000, 0x7010, 0x1A76 etc.
            Dim addressMap As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                {"00", "0x7700"},
                {"01", "0x7701"},
                {"06", "0x5706"},
                {"21", "0x5721"},
                {"23", "0x5723"},
                {"24", "0x5724"},
                {"28", "0x5728"},
                {"2E", "0x572E"},
                {"2F", "0x572F"},
                {"30", "0x5730"},
                {"31", "0x5731"},
                {"32", "0x5732"},
                {"34", "0x5734"},
                {"38", "0x5738"},
                {"50", "0x7750"},
                {"52", "0x7752"},
                {"53", "0x7753"},
                {"54", "0x7754"},
                {"56", "0x6756"},
                {"58", "0x6758"},
                {"59", "0x6759"},
                {"5B", "0x675B"},
                {"62", "0x6762"},
                {"65", "0x6765"},
                {"67", "0x6767"},
                {"6C", "0x676C"},
                {"6D", "0x676D"},
                {"6F", "0x676F"},
                {"71", "0x6771"},
                {"72", "0x6772"},
                {"73", "0x6773"},
                {"76", "0x7776"},
                {"77", "0x7777"},
                {"78", "0x7778"},
                {"79", "0x7779"},
                {"7B", "0x777B"},
                {"7F", "0x777F"},
                {"80", "0x7780"},
                {"81", "0x7781"},
                {"88", "0x7788"},
                {"8A", "0x778A"},
                {"8B", "0x778B"},
                {"8C", "0x778C"},
                {"8D", "0x778D"},
                {"8E", "0x778E"},
                {"90", "0x7790"},
                {"91", "0x7791"},
                {"95", "0x7795"},
                {"97", "0x7797"},
                {"98", "0x7798"},
                {"9B", "0x779B"},
                {"9C", "0x779C"},
                {"9F", "0x779F"},
                {"A0", "0x27A0"},
                {"A2", "0x27A2"},
                {"E5", "0x57E5"},
                {"1A", "0x571A"},
                {"7000", "0x7000"},
                {"7010", "0x7010"},
                {"7016", "0x7016"},
                {"1A76", "0x1A76"}
            }

            Dim targetAddress As String = Nothing
            ' Versuche zuerst ?ber Address-Mapping
            If addressMap.TryGetValue(parameterId, targetAddress) Then
                For Each row As DataRow In dataTable.Rows
                    Dim address = Convert.ToString(row("Address"))
                    If address.Equals(targetAddress, StringComparison.OrdinalIgnoreCase) Then
                        ' Zus?tzliche Pr?fung: Ist MQTT_Read vorhanden und gef?llt?
                        If dataTable.Columns.Contains("MQTT_Read") Then
                            Dim mqttRead = Convert.ToString(row("MQTT_Read"))
                            If Not String.IsNullOrWhiteSpace(mqttRead) Then
                                Return row
                            End If
                        Else
                            Return row
                        End If
                    End If
                Next
            End If

            ' Fallback 1: Suche im Parameter-Namen nach "({id})" - auch f?r l?ngere IDs
            ' Pattern: (00), (2E), (7010), (7016) etc.
            Dim searchPattern1 = $"({parameterId})"
            For Each row As DataRow In dataTable.Rows
                If dataTable.Columns.Contains("Parameter") Then
                    Dim paramName = Convert.ToString(row("Parameter"))
                    If paramName.StartsWith(searchPattern1, StringComparison.OrdinalIgnoreCase) Then
                        ' Pr?fe ob MQTT_Read gef?llt ist
                        If dataTable.Columns.Contains("MQTT_Read") Then
                            Dim mqttRead = Convert.ToString(row("MQTT_Read"))
                            If Not String.IsNullOrWhiteSpace(mqttRead) Then
                                Return row
                            End If
                        Else
                            Return row
                        End If
                    End If
                End If
            Next

            ' Fallback 2: Suche im Parameter-Namen nach "K{id}_" Format (VOR ?bersetzung!)
            ' Pattern: K00_, K76_, K7F_ etc.
            Dim searchPattern2 = $"K{parameterId}_"
            For Each row As DataRow In dataTable.Rows
                If dataTable.Columns.Contains("Parameter") Then
                    Dim paramName = Convert.ToString(row("Parameter"))
                    If paramName.StartsWith(searchPattern2, StringComparison.OrdinalIgnoreCase) Then
                        ' Pr?fe ob MQTT_Read gef?llt ist
                        If dataTable.Columns.Contains("MQTT_Read") Then
                            Dim mqttRead = Convert.ToString(row("MQTT_Read"))
                            If Not String.IsNullOrWhiteSpace(mqttRead) Then
                                Return row
                            End If
                        Else
                            Return row
                        End If
                    End If
                End If
            Next

            ' Fallback 3: Suche nach Address direkt (f?r l?ngere IDs wie "7010")
            If parameterId.Length > 2 Then
                Dim possibleAddress = $"0x{parameterId.ToUpper()}"

                For Each row As DataRow In dataTable.Rows
                    Dim address = Convert.ToString(row("Address"))
                    If address.Equals(possibleAddress, StringComparison.OrdinalIgnoreCase) Then
                        ' Pr?fe ob MQTT_Read gef?llt ist
                        If dataTable.Columns.Contains("MQTT_Read") Then
                            Dim mqttRead = Convert.ToString(row("MQTT_Read"))
                            If Not String.IsNullOrWhiteSpace(mqttRead) Then
                                Return row
                            End If
                        Else
                            Return row
                        End If
                    End If
                Next
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Parsed eine MQTT-Response und extrahiert den Wert
        ''' </summary>
        ''' <param name="response">MQTT Response im Format "return_code;address;value"</param>
        ''' <returns>Der extrahierte Wert oder Nothing bei Fehler</returns>
        Private Shared Function ParseMqttResponse(response As String) As String
            If String.IsNullOrWhiteSpace(response) Then
                Return Nothing
            End If

            ' Format: "return_code;address;value"
            ' z.B. "1;2048;8.2" oder "1;0x7700;2"
            Dim parts = response.Split(";"c)

            If parts.Length < 3 Then
                Return Nothing
            End If

            Dim returnCode = parts(0).Trim()

            ' Prüfe Return-Code (01 = Success)
            If Not returnCode.Equals("01", StringComparison.OrdinalIgnoreCase) AndAlso
                Not returnCode.Equals("1", StringComparison.OrdinalIgnoreCase) Then
                Return Nothing
            End If

            ' Der Wert ist das dritte Teil
            Dim value = parts(2).Trim()

            ' Bei Enum-Werten: Extrahiere nur die ID vor dem Leerzeichen
            ' z.B. "2 A1 + WW" -> "2"
            If value.Contains(" "c) Then
                value = value.Split(" "c)(0)
            End If

            Return value
        End Function

        ''' <summary>
        ''' Konvertiert einen numerischen Wert zu einem textuellen Enum-Wert
        ''' </summary>
        ''' <param name="dataTable">DataTable mit DeviceEvents</param>
        ''' <param name="parameterId">Parameter-ID</param>
        ''' <param name="numericValue">Numerischer Wert (z.B. "2")</param>
        ''' <returns>Textuelle Beschreibung (z.B. "A1 + WW") oder numericValue wenn nicht gefunden</returns>
        Public Shared Function ResolveEnumValue(dataTable As DataTable,
     parameterId As String,
   numericValue As String) As String
            If dataTable Is Nothing OrElse String.IsNullOrWhiteSpace(numericValue) Then
                Return numericValue
            End If

            ' Finde die Row für die Parameter-ID
            Dim row = FindParameterRow(dataTable, parameterId)
            If row Is Nothing Then
                Return numericValue
            End If

            ' Hole Values und Descriptions
            If Not dataTable.Columns.Contains("Values") OrElse
                          Not dataTable.Columns.Contains("Descriptions") Then
                Return numericValue
            End If

            Dim values = Convert.ToString(row("Values"))
            Dim descriptions = Convert.ToString(row("Descriptions"))

            If String.IsNullOrWhiteSpace(values) OrElse String.IsNullOrWhiteSpace(descriptions) Then
                Return numericValue
            End If

            ' Parse die Listen
            Dim valueList = values.Split(","c)
            Dim descList = descriptions.Split(","c)

            If valueList.Length <> descList.Length Then
                Return numericValue
            End If

            ' Finde den Index des numerischen Werts
            For i As Integer = 0 To valueList.Length - 1
                If valueList(i).Trim().Equals(numericValue, StringComparison.OrdinalIgnoreCase) Then
                    ' Gib die Beschreibung zurück (ohne Präfix wie "ecnUnit.")
                    Dim desc = descList(i).Trim()
                    ' Entferne eventuelle Präfixe
                    If desc.Contains(" "c) Then
                        desc = desc.Substring(desc.IndexOf(" "c) + 1)
                    End If
                    Return desc
                End If
            Next

            Return numericValue
        End Function

    End Class
End Namespace