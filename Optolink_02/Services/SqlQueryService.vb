Namespace Services
    Public NotInheritable Class SqlQueryService
        Private Sub New()
        End Sub

        Public Shared Async Function GetDeviceEventsAsync(deviceName As String) As Task(Of DataTable)
            If String.IsNullOrWhiteSpace(deviceName) Then Throw New ArgumentNullException(NameOf(deviceName))
            Return Await Task.Run(Function()
                                      Using cn As New SqlConnection(BuildConnectionString())
                                          cn.Open()
                                          Using cmd As New SqlCommand(SQL_DeviceEventQuery, cn)
                                              Dim unused = cmd.Parameters.AddWithValue("@DeviceName", deviceName)
                                              Using rdr = cmd.ExecuteReader()
                                                  Dim table As New DataTable()
                                                  table.Load(rdr)
                                                  Return table
                                              End Using
                                          End Using
                                      End Using
                                  End Function)
        End Function

        ''' <summary>
        ''' Holt die Adresszuordnung für Konfigurationswerte (z.B. "76_KommMod" -> "0x7776")
        ''' Die Keys sind Parameter-Namen aus HIDDEN-Bedingungen (z.B. "76_KommMod", "A0_KennFBA1M1")
        ''' Diese entsprechen oft dem Kurzformat der Parameter-Namen
        ''' </summary>
        Public Shared Async Function GetConfigAddressMappingAsync(keys As List(Of String)) As Task(Of DataTable)
            Return If(keys Is Nothing OrElse keys.Count = 0,
                New DataTable(),
                Await Task.Run(Function()
                                   Dim result As New DataTable()
                                   Dim unused2 = result.Columns.Add("ConfigKey", GetType(String))
                                   Dim unused1 = result.Columns.Add("HexAddress", GetType(String))

                                   Try
                                       Using cn As New SqlConnection(BuildConnectionString())
                                           cn.Open()

                                           ' Die HIDDEN-Conditions verwenden oft Kurzformen der Parameter-Namen
                                           ' z.B. "76_KommMod" statt "K76_KonfiKommunikationsmodul"
                                           ' Wir müssen in der ecnEventType.Address Spalte suchen
                                           ' Format: "K76_KonfiKommunikationsmodul~0x7776" oder "76_KommMod~0x7776"

                                           ' Erstelle IN-Klausel für die Suche
                                           Dim searchPatterns As New List(Of String)()
                                           For Each key In keys
                                               ' Versuche verschiedene Muster:
                                               ' 1. Exakte Übereinstimmung mit "XX_Name"
                                               ' 2. Mit vorangestelltem "K" -> "KXX_Name"
                                               searchPatterns.Add(key)
                                               If key.Length >= 2 AndAlso Char.IsDigit(key(0)) AndAlso Not key.StartsWith("K"c) Then
                                                   searchPatterns.Add("K" & key)
                                               End If
                                           Next

                                           ' Baue SQL-Query mit LIKE-Bedingungen
                                           Dim whereConditions As New List(Of String)()
                                           For i = 0 To searchPatterns.Count - 1
                                               whereConditions.Add($"et.Address LIKE @pattern{i} + '%'")
                                           Next

                                           Dim query = $"
                                            SELECT DISTINCT
                                            etv.EventTypeID,
                                            et.Address,
                                            etv.HexAddress
                                            FROM ecnEventTypeValue etv
                                            INNER JOIN ecnEventType et ON etv.EventTypeID = et.ID
                                            WHERE ({String.Join(" OR ", whereConditions)})
                                            AND etv.HexAddress IS NOT NULL
                                            AND etv.HexAddress <> ''
                                            AND etv.HexAddress LIKE '0x%' 
                                            "

                                           Using cmd As New SqlCommand(query, cn)
                                               ' Parameter hinzufügen
                                               For i = 0 To searchPatterns.Count - 1
                                                   Dim unused = cmd.Parameters.AddWithValue($"@pattern{i}", searchPatterns(i))
                                               Next

                                               Using rdr = cmd.ExecuteReader()
                                                   While rdr.Read()
                                                       Dim address = Convert.ToString(rdr("Address"))
                                                       Dim hexAddress = Convert.ToString(rdr("HexAddress"))

                                                       ' Extrahiere den Key aus dem Address-Format "Name~0x1234"
                                                       ' und mappe ihn zu den ursprünglichen Keys
                                                       Dim addressParts = address.Split("~"c)
                                                       If addressParts.Length >= 1 Then
                                                           Dim paramName = addressParts(0)

                                                           ' Finde passenden Original-Key
                                                           For Each origKey In keys
                                                               If paramName.Contains(origKey,
                                                                                     StringComparison.OrdinalIgnoreCase) OrElse
                                                                                     origKey.Contains(paramName, StringComparison.OrdinalIgnoreCase) Then
                                                                   Dim row = result.NewRow()
                                                                   row("ConfigKey") = origKey
                                                                   row("HexAddress") = hexAddress
                                                                   result.Rows.Add(row)
                                                                   Debug.WriteLine($"[CONFIG MAPPING] {origKey} -> {hexAddress} (via {paramName})")
                                                                   Exit For
                                                               End If
                                                           Next
                                                       End If
                                                   End While
                                               End Using
                                           End Using
                                       End Using
                                   Catch ex As Exception
                                       Debug.WriteLine($"[ERROR] GetConfigAddressMappingAsync: {ex.Message}")
                                   End Try

                                   Return result
                               End Function))
        End Function
    End Class
End Namespace