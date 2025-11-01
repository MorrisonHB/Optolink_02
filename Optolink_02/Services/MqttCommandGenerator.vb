Imports System.Text

Namespace Services
    ''' <summary>
    ''' Service zum Generieren von MQTT-Kommandos basierend auf DeviceEvent-Daten.
    ''' 
    ''' Unterstützte Kommandos:
    ''' - Read:  read;<address>;<length>[;<scale/type>[;<signed>]]
    ''' - Write: write;<address>;<length>;<value>
    ''' 
    ''' Beispiele:
    ''' - read;0x5525;2;0.1;true
    ''' - write;0x6300;1;45
    ''' </summary>
    Public Class MqttCommandGenerator

        ''' <summary>
        ''' Generiert ein MQTT Read-Kommando aus den Event-Parametern
        ''' </summary>
        ''' <param name="address">Hexadezimale Adresse (z.B. "0x5525")</param>
        ''' <param name="byteLength">Anzahl der zu lesenden Bytes</param>
        ''' <param name="conversion">Konvertierungstyp (z.B. "Div10", "Div2", "NoConversion")</param>
        ''' <param name="dataType">Datentyp (z.B. "Float", "Int", "Byte")</param>
        ''' <returns>MQTT Read-Kommando als String</returns>
        Public Shared Function GenerateReadCommand(address As String,
                                                   byteLength As String,
                                                   conversion As String,
                                                   dataType As String) As String
            If String.IsNullOrWhiteSpace(address) Then
                Return String.Empty
            End If

            Dim cmd As New StringBuilder()
            Dim unused8 = cmd.Append("read")
            Dim unused7 = cmd.Append(";"c)
            Dim unused6 = cmd.Append(address) ' z.B. "0x5525"
            Dim unused5 = cmd.Append(";"c)
            Dim unused4 = cmd.Append(If(String.IsNullOrWhiteSpace(byteLength), "1", byteLength))

            ' Füge Scale/Type hinzu wenn vorhanden
            Dim scale = GetScaleFromConversion(conversion)
            If Not String.IsNullOrWhiteSpace(scale) Then
                Dim unused3 = cmd.Append(";"c)
                Dim unused2 = cmd.Append(scale) ' z.B. "0.1"

                ' Füge Signed-Flag hinzu wenn nötig
                Dim isSigned = DetermineIfSigned(dataType)
                If isSigned.HasValue Then
                    Dim unused1 = cmd.Append(";"c)
                    Dim unused = cmd.Append(If(isSigned.Value, "true", "false"))
                End If
            End If

            Return cmd.ToString()
        End Function

        ''' <summary>
        ''' Generiert ein MQTT Write-Kommando aus den Event-Parametern
        ''' </summary>
        ''' <param name="address">Hexadezimale Adresse (z.B. "0x6300")</param>
        ''' <param name="byteLength">Anzahl der zu schreibenden Bytes</param>
        ''' <param name="valueToWrite">Der zu schreibende Wert (wird als Platzhalter "{value}" eingefügt)</param>
        ''' <returns>MQTT Write-Kommando-Template als String</returns>
        Public Shared Function GenerateWriteCommand(address As String,
         byteLength As String,
      Optional valueToWrite As String = "{value}") As String
            If String.IsNullOrWhiteSpace(address) Then
                Return String.Empty
            End If

            Dim cmd As New StringBuilder()
            Dim unused6 = cmd.Append("write")
            Dim unused5 = cmd.Append(";"c)
            Dim unused4 = cmd.Append(address)
            Dim unused3 = cmd.Append(";"c)
            Dim unused2 = cmd.Append(If(String.IsNullOrWhiteSpace(byteLength), "1", byteLength))
            Dim unused1 = cmd.Append(";"c)
            Dim unused = cmd.Append(valueToWrite) ' Platzhalter für den Wert

            Return cmd.ToString()
        End Function

        ''' <summary>
        ''' Bestimmt den Scale-Faktor aus dem Conversion-Typ
        ''' </summary>
        ''' <param name="conversion">Conversion-String (z.B. "Div10", "Mult2", "NoConversion")</param>
        ''' <returns>Scale-Faktor als String oder Nothing</returns>
        Private Shared Function GetScaleFromConversion(conversion As String) As String
            If String.IsNullOrWhiteSpace(conversion) Then
                Return Nothing
            End If

            ' Mapping von Conversion-Typen zu Scale-Faktoren
            Select Case conversion.Trim().ToUpperInvariant()
                Case "DIV2"
                    Return "0.5"
                Case "DIV10"
                    Return "0.1"
                Case "DIV100"
                    Return "0.01"
                Case "DIV1000"
                    Return "0.001"
                Case "MULT2"
                    Return "2"
                Case "MULT5"
                    Return "5"
                Case "MULT10"
                    Return "10"
                Case "MULT100"
                    Return "100"
                Case "NOCONVERSION"
                    Return Nothing ' Keine Skalierung
                Case Else
                    ' Unbekannte Conversion -> keine Skalierung
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Bestimmt ob der Datentyp vorzeichenbehaftet (signed) ist
        ''' </summary>
        ''' <param name="dataType">Datentyp (z.B. "Float", "Int", "Byte")</param>
        ''' <returns>True wenn signed, False wenn unsigned, Nothing wenn nicht relevant</returns>
        Private Shared Function DetermineIfSigned(dataType As String) As Boolean?
            If String.IsNullOrWhiteSpace(dataType) Then
                Return Nothing
            End If

            Select Case dataType.Trim().ToUpperInvariant()
                Case "FLOAT", "DOUBLE"
                    Return True ' Fließkommazahlen sind immer signed
                Case "INT", "INTEGER", "INT16", "INT32", "LONG"
                    Return True ' Integer standardmäßig signed
                Case "BYTE", "UINT", "UINT16", "UINT32", "ULONG"
                    Return False ' Unsigned Typen
                Case Else
                    Return Nothing ' Unbekannt -> nichts anhängen
            End Select
        End Function

        ''' <summary>
        ''' Fügt MQTT-Command-Spalten zu einer DataTable hinzu
        ''' </summary>
        ''' <param name="table">Die DataTable mit DeviceEvent-Daten</param>
        Public Shared Sub AddMqttCommandColumns(table As DataTable)
            If table Is Nothing Then
                Return
            End If

            ' Prüfe ob die benötigten Spalten existieren
            If Not table.Columns.Contains("Address") OrElse
             Not table.Columns.Contains("ByteLength") OrElse
              Not table.Columns.Contains("Conversion") Then
                ' Debug.WriteLine("[MqttCommandGenerator] Erforderliche Spalten fehlen in der DataTable")
                Return
            End If

            ' Füge neue Spalten hinzu (falls nicht vorhanden)
            If Not table.Columns.Contains("MQTT_Read") Then
                Dim unused1 = table.Columns.Add("MQTT_Read", GetType(String))
            End If

            If Not table.Columns.Contains("MQTT_Write") Then
                Dim unused = table.Columns.Add("MQTT_Write", GetType(String))
            End If

            ' Generiere die Kommandos für jede Zeile
            For Each row As DataRow In table.Rows
                Dim address = Convert.ToString(row("Address"))
                Dim byteLength = Convert.ToString(row("ByteLength"))
                Dim conversion = Convert.ToString(row("Conversion"))
                Dim dataType = If(table.Columns.Contains("DataType"), Convert.ToString(row("DataType")), "")
                Dim readWrite = If(table.Columns.Contains("ReadWrite"), Convert.ToString(row("ReadWrite")), "")
                Dim parameter = If(table.Columns.Contains("Parameter"), Convert.ToString(row("Parameter")), "")

                ' Prüfe ob es ein Codierungs-Parameter ist (beginnt mit "(ID)")
                ' WICHTIG: ID kann 2, 4 oder mehr Zeichen lang sein (z.B. "(00)", "(7010)")
                Dim isConfigParameter = Not String.IsNullOrEmpty(parameter) AndAlso
                    System.Text.RegularExpressions.Regex.IsMatch(parameter, "^\([0-9A-F]+\)",
              System.Text.RegularExpressions.RegexOptions.IgnoreCase)

                ' Generiere Read-Kommando wenn:
                ' 1. ReadWrite enthält "R" ODER
                ' 2. Es ist ein Codierungs-Parameter (für HIDDEN-Condition-Filterung)
                row("MQTT_Read") = If(String.IsNullOrWhiteSpace(readWrite) OrElse
               readWrite.Contains("R"c, StringComparison.OrdinalIgnoreCase) OrElse
        isConfigParameter,
                    GenerateReadCommand(address, byteLength, conversion, dataType),
                    String.Empty)

                ' Generiere Write-Kommando wenn schreibbar
                row("MQTT_Write") = If(readWrite.Contains("W"c, StringComparison.OrdinalIgnoreCase), GenerateWriteCommand(address, byteLength), String.Empty)
            Next

            ' Debug.WriteLine($"[MqttCommandGenerator] MQTT-Kommandos für {table.Rows.Count} Zeilen generiert")
        End Sub

        ''' <summary>
        ''' Generiert ein vollständiges MQTT-Kommando aus einer DataRow
        ''' </summary>
        ''' <param name="row">DataRow mit Event-Daten</param>
        ''' <param name="commandType">Typ des Kommandos: "read" oder "write"</param>
        ''' <param name="writeValue">Optional: Wert zum Schreiben (nur bei write)</param>
        ''' <returns>Vollständiges MQTT-Kommando</returns>
        Public Shared Function GenerateCommandFromRow(row As DataRow,
                commandType As String,
                       Optional writeValue As String = Nothing) As String
            If row Is Nothing Then
                Return String.Empty
            End If

            Dim address = Convert.ToString(row("Address"))
            Dim byteLength = Convert.ToString(row("ByteLength"))
            Dim conversion = If(row.Table.Columns.Contains("Conversion"), Convert.ToString(row("Conversion")), "")
            Dim dataType = If(row.Table.Columns.Contains("DataType"), Convert.ToString(row("DataType")), "")

            If commandType.Equals("read", StringComparison.OrdinalIgnoreCase) Then
                Return GenerateReadCommand(address, byteLength, conversion, dataType)
            ElseIf commandType.Equals("write", StringComparison.OrdinalIgnoreCase) Then
                Dim cmd = GenerateWriteCommand(address, byteLength, "{value}")
                If Not String.IsNullOrWhiteSpace(writeValue) Then
                    cmd = cmd.Replace("{value}", writeValue)
                End If
                Return cmd
            Else
                Return String.Empty
            End If
        End Function

        ''' <summary>
        ''' Validiert ein MQTT-Kommando
        ''' </summary>
        ''' <param name="command">Das zu validierende Kommando</param>
        ''' <returns>True wenn gültig, sonst False</returns>
        Public Shared Function ValidateCommand(command As String) As Boolean
            If String.IsNullOrWhiteSpace(command) Then
                Return False
            End If

            Dim parts = command.Split(";"c)

            ' Mindestens command und address erforderlich
            If parts.Length < 2 Then
                Return False
            End If

            ' Erstes Teil muss ein gültiges Kommando sein
            Dim cmd = parts(0).Trim().ToLowerInvariant()
            If cmd <> "read" AndAlso cmd <> "r" AndAlso
           cmd <> "write" AndAlso cmd <> "w" AndAlso
                  cmd <> "writeraw" AndAlso cmd <> "wraw" Then
                Return False
            End If

            ' Zweites Teil muss eine gültige Hex-Adresse sein
            Dim address = parts(1).Trim()
            Return address.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        End Function

    End Class
End Namespace
