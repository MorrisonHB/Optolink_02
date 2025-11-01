Namespace Parsers

    Public Class EcnDeviceParser
        ' Parses XML/XML\ecnDataPointType.xml to build device display lines similar to the Python implementation.

        Private Shared Function GetXmlPath() As String
            Dim baseDir As String = TryCast(AppDomain.CurrentDomain.GetData("DataDirectory"), String)
            If String.IsNullOrEmpty(baseDir) Then
                baseDir = AppDomain.CurrentDomain.BaseDirectory
            End If
            Return IO.Path.Combine(baseDir, "XML", "ecnDataPointType.xml")
        End Function

        ' Port of get_datapoint_display_lines from Python
        Public Shared Function GetDatapointDisplayLines() As List(Of String())
            Dim evnDataPointTypes As New Dictionary(Of String,
                Dictionary(Of String, String))(StringComparer.OrdinalIgnoreCase)

            ' parse_ecnDataPointType
            Dim xmlPath = GetXmlPath()
            Dim doc As XDocument = XDocument.Load(xmlPath)
            For Each nodes In doc.Root.Elements()
                Dim dataPointType As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

                For Each cell In nodes.Elements()
                    Dim tag = cell.Name.LocalName
                    Dim value = If(cell.Value, String.Empty)

                    If New String() {"Description", "EventOptimisationExceptionList", "EventOptimisation",
                        "Options", "ErrorType"}.Contains(tag) Then
                        Continue For
                    End If

                    If New String() {"ControllerType", "ErrorType", "EventOptimisation"}.Contains(tag) Then
                        ' Try cast to int in Python; keep RPi_CommandValue as string, the check exists to mirror intent
                        Dim tmp As Integer
                        If Integer.TryParse(value, tmp) Then
                            value = tmp.ToString()
                        End If
                    End If

                    dataPointType(tag) = value
                Next

                Dim id As String = String.Empty
                If dataPointType.TryGetValue("ID", id) Then
                    id = If(id, String.Empty).Trim()
                End If
                If String.IsNullOrWhiteSpace(id) Then
                    Continue For
                End If

                Dim unused = dataPointType.Remove("ID")
                evnDataPointTypes(id) = dataPointType
            Next

            Dim lines As New List(Of String())
            For Each kvp In evnDataPointTypes
                Dim ID = kvp.Key
                Dim dataPointType = kvp.Value

                Dim identification As String = Nothing
                If Not dataPointType.TryGetValue("Identification", identification) Then
                    Continue For
                End If
                If Not identification.StartsWith("20") Then
                    Continue For
                End If
                Dim identificationExtension As String = Nothing
                If dataPointType.TryGetValue("IdentificationExtension", identificationExtension) AndAlso
                    identificationExtension.Length <> 4 Then Continue For

                Dim idStr As String = "ecnsysDeviceIdent:" & identification
                If identificationExtension IsNot Nothing Then
                    idStr &= " sysHardware/SoftwareIndexIdent:" & identificationExtension
                    Dim identificationExtensionTill As String = Nothing
                    If dataPointType.TryGetValue("IdentificationExtensionTill", identificationExtensionTill) Then
                        idStr &= "-" & identificationExtensionTill
                    End If
                End If
                Dim f0 As String = Nothing
                If dataPointType.TryGetValue("F0", f0) Then
                    idStr &= " ecnsysDeviceIdentF0:" & f0
                    Dim f0Till As String = Nothing
                    If dataPointType.TryGetValue("F0Till", f0Till) Then
                        idStr &= "-" & f0Till
                    End If
                End If

                lines.Add({idStr, ID})
            Next

            lines.Sort(Function(a, b) String.Compare(a(0), b(0), StringComparison.Ordinal))
            Return lines
        End Function

        ' Builds DeviceInfo list
        ' preferDb:=False -> XML-only (schneller Start, Standard)
        ' preferDb:=True -> DB-first wie im Viessmann-Projekt
        Public Shared Function GetDevices(Optional preferDb As Boolean = False) As List(Of DeviceInfo)
            If preferDb Then
                Dim result As New List(Of DeviceInfo)
                If TryGetDevicesFromDb(result) Then
                    Return result
                End If
            End If
            Return GetDevicesXmlOnly()
        End Function

        Private Shared Function GetDevicesXmlOnly() As List(Of DeviceInfo)
            Dim result As New List(Of DeviceInfo)
            Dim lines = GetDatapointDisplayLines()
            For Each arr In lines
                Dim idStr = arr(0)
                Dim name = arr(1)
                Dim device As New DeviceInfo With {
                    .EcnsysDeviceIdent = ExtractEcnsysDeviceIdent(idStr),
                    .SoftwareIndexIdent = ExtractSoftwareIndex(idStr),
                    .DeviceName = name
                }
                result.Add(device)
            Next
            Return result
        End Function

        Public Shared Function GetDeviceDictionary() As Dictionary(Of String, DeviceInfo)
            ' Standard: schneller XML-Weg beim Start
            Dim dict As New Dictionary(Of String, DeviceInfo)(StringComparer.OrdinalIgnoreCase)
            For Each d In GetDevices() ' default: preferDb:=False
                Dim key = d.EcnsysDeviceIdent & If(String.IsNullOrEmpty(d.SoftwareIndexIdent),
                    "", "|" & d.SoftwareIndexIdent)
                dict(key) = d
            Next
            Return dict
        End Function

        Private Shared Function ExtractEcnsysDeviceIdent(line As String) As String
            ' line begins with "ecnsysDeviceIdent:XXXX" optionally followed by other parts
            Dim prefix = "ecnsysDeviceIdent:"
            Dim idx = line.IndexOf(prefix, StringComparison.Ordinal)
            If idx < 0 Then Return String.Empty
            Dim rest = line.Substring(idx + prefix.Length)
            Dim space = rest.IndexOf(" "c)
            Return If(space >= 0, rest.Substring(0, space), rest)
        End Function

        Private Shared Function ExtractSoftwareIndex(line As String) As String
            Dim token = "sysHardware/SoftwareIndexIdent:"
            Dim idx = line.IndexOf(token, StringComparison.Ordinal)
            If idx < 0 Then Return String.Empty
            Dim rest = line.Substring(idx + token.Length)
            Dim space = rest.IndexOf(" "c)
            Return If(space >= 0, rest.Substring(0, space), rest)
        End Function

        ' ================== DB support ==================

        Private Shared Function GetDatabaseFilePath() As String
            Dim dataDir As String = TryCast(AppDomain.CurrentDomain.GetData("DataDirectory"), String)
            If String.IsNullOrEmpty(dataDir) Then
                dataDir = AppDomain.CurrentDomain.BaseDirectory
            End If
            Return IO.Path.Combine(dataDir, "Datenbank", "ecnViessmann.mdf")
        End Function

        Private Shared Function BuildConnectionString() As String
            ' Delegiere an zentrales SqlQueries-Modul, um Attach-Konflikte zu vermeiden
            Return SqlQueries.BuildConnectionString()
        End Function

        Private Shared Function TryGetDevicesFromDb(ByRef devices As List(Of DeviceInfo)) As Boolean
            devices = New List(Of DeviceInfo)()
            Dim baseSql As String =
                "SELECT ID, Identification, IdentificationExtension, IdentificationExtensionTill, F0, F0Till FROM {0}"
            Dim sql As String = Nothing
            Try
                Dim mdfPath = GetDatabaseFilePath()
                Dim fileExists = IO.File.Exists(mdfPath)
                Debug.WriteLine($"DB attach file: {mdfPath}; exists={fileExists}")
                If Not fileExists Then
                    Return False
                End If

                Debug.WriteLine($"Connecting with: {BuildConnectionString()}")
                Using cn As New SqlConnection(BuildConnectionString())
                    cn.Open()
                    Debug.WriteLine("SQL connected successfully.")
                    ' Discover available tables and pick the correct one (with schema)
                    Dim candidates As String() = {"ecnDataPointType", "ecnDatapointType"}
                    Dim tableMap As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                    Using listCmd As New SqlCommand("SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) AS FullName, t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id ORDER BY t.name", cn)
                        Using lr = listCmd.ExecuteReader()
                            Dim allTables As New List(Of String)()
                            While lr.Read()
                                Dim fullName As String = Convert.ToString(lr("FullName"))
                                Dim shortName As String = Convert.ToString(lr("name"))
                                allTables.Add(fullName)
                                If Not tableMap.ContainsKey(shortName) Then
                                    tableMap(shortName) = fullName
                                End If
                            End While
                            Debug.WriteLine("Available tables:")
                            For Each n In allTables
                                Debug.WriteLine(" - " & n)
                            Next
                        End Using
                    End Using

                    Dim chosenFullName As String = Nothing
                    For Each c In candidates
                        Dim full As String = Nothing
                        If tableMap.TryGetValue(c, full) Then
                            chosenFullName = full
                            Exit For
                        End If
                    Next

                    If String.IsNullOrEmpty(chosenFullName) Then
                        Debug.WriteLine("No ecnDataPointType table found. Falling back to XML.")
                        Return False
                    End If

                    ' Inspect columns in chosen table
                    Dim mainCols As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                    Using colCmd As New SqlCommand("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@tbl)", cn)
                        Dim unused3 = colCmd.Parameters.AddWithValue("@tbl", chosenFullName)
                        Using cr = colCmd.ExecuteReader()
                            Debug.WriteLine("Columns in " & chosenFullName & ":")
                            While cr.Read()
                                Dim colName = Convert.ToString(cr("name"))
                                Dim unused2 = mainCols.Add(colName)
                                Debug.WriteLine(" - " & colName)
                            End While
                        End Using
                    End Using

                    Dim canDirectSelect As Boolean = mainCols.Contains("Identification") AndAlso
                        mainCols.Contains("IdentificationExtension")
                    Dim useJoin As Boolean = False
                    Dim extFullName As String = Nothing

                    If Not canDirectSelect Then
                        ' Try extension table
                        If tableMap.TryGetValue("vsmDatapointTypeExtension", extFullName) Then
                            Dim extCols As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                            Using colCmd As New SqlCommand("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@tbl)", cn)
                                Dim unused1 = colCmd.Parameters.AddWithValue("@tbl", extFullName)
                                Using cr = colCmd.ExecuteReader()
                                    Debug.WriteLine("Columns in " & extFullName & ":")
                                    While cr.Read()
                                        Dim colName = Convert.ToString(cr("name"))
                                        Dim unused = extCols.Add(colName)
                                        Debug.WriteLine(" - " & colName)
                                    End While
                                End Using
                            End Using
                            If extCols.Contains("Identification") AndAlso extCols.Contains("IdentificationExtension") Then
                                useJoin = mainCols.Contains("Id") AndAlso extCols.Contains("DataPointTypeId")
                            End If
                        End If
                    End If

                    If canDirectSelect Then
                        sql = String.Format(baseSql, chosenFullName)
                    ElseIf useJoin AndAlso Not String.IsNullOrEmpty(extFullName) Then
                        sql = $"SELECT d.Id AS ID, x.Identification, x.IdentificationExtension, x.IdentificationExtensionTill, x.F0, x.F0Till FROM {chosenFullName} d JOIN {extFullName} x ON x.DataPointTypeId = d.Id"
                    Else
                        ' As a last resort: enrich DB rows with XML ecnDataPointType.xml by joining on Address=ID
                        Debug.WriteLine("No suitable columns in DB. Trying DB+XML enrichment (Address -> XML.ID)...")
                        Dim enrichOk As Boolean = EnrichDevicesFromDbWithXml(cn, chosenFullName, devices)
                        If enrichOk Then
                            Debug.WriteLine($"Device source: SQL (base) + XML (ident) ({devices.Count} items)")
                            Return True
                        Else
                            Debug.WriteLine("DB+XML enrichment failed. Falling back to XML only.")
                            Return False
                        End If
                    End If

                    Using cmd As New SqlCommand(sql, cn)
                        Using rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection)
                            While rdr.Read()
                                Dim id As String = Convert.ToString(rdr("ID")).Trim()
                                Dim identification As String = If(rdr("Identification") Is DBNull.Value,
                                    Nothing, Convert.ToString(rdr("Identification")).Trim())
                                Dim identExt As String = If(rdr("IdentificationExtension") Is DBNull.Value,
                                    Nothing, Convert.ToString(rdr("IdentificationExtension")).Trim())
                                ' Mirror XML filters
                                If String.IsNullOrEmpty(identification) OrElse Not identification.StartsWith("20") Then
                                    Continue While
                                End If
                                If Not String.IsNullOrEmpty(identExt) AndAlso identExt.Length <> 4 Then
                                    Continue While
                                End If

                                Dim dev As New DeviceInfo With {
                                    .EcnsysDeviceIdent = identification,
                                    .SoftwareIndexIdent = If(identExt, String.Empty),
                                    .DeviceName = id
                                }
                                devices.Add(dev)
                            End While
                        End Using
                    End Using
                End Using
                ' If we loaded any, return True
                If devices.Count > 0 Then
                    Debug.WriteLine($"Device source: SQL database ({devices.Count} items)")
                    Return True
                End If
                Return False
            Catch ex As SqlException
                Debug.WriteLine($"SQL error: {ex.Message}")
                If Not String.IsNullOrEmpty(sql) Then Debug.WriteLine($"Failed SQL: {sql}")
                For Each err As SqlError In ex.Errors
                    Debug.WriteLine($" -> Number={err.Number}, State={err.State}, Class={err.Class}, Line={err.LineNumber}, Procedure={err.Procedure}, Message={err.Message}")
                Next
                devices.Clear()
                Return False
            Catch ex As Exception
                Debug.WriteLine($"Non-SQL exception: {ex.Message}")
                devices.Clear()
                Return False
            End Try
        End Function

        Private Shared Function EnrichDevicesFromDbWithXml(cn As SqlConnection,
                                                           tableFullName As String,
                                                           ByRef devices As List(Of DeviceInfo)) As Boolean
            Try
                ' Load XML map: ID -> fields
                Dim xmlPath = GetXmlPath()
                If Not IO.File.Exists(xmlPath) Then
                    Debug.WriteLine("XML ecnDataPointType.xml not found for enrichment: " & xmlPath)
                    Return False
                End If
                Dim xmlDoc As XDocument = XDocument.Load(xmlPath)
                Dim xmlMap As New Dictionary(Of String, Dictionary(Of String, String))(StringComparer.OrdinalIgnoreCase)
                For Each nodes In xmlDoc.Root.Elements()
                    Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                    For Each c In nodes.Elements()
                        d(c.Name.LocalName) = c.Value
                    Next
                    Dim key As String = Nothing
                    If d.TryGetValue("ID", key) AndAlso Not String.IsNullOrWhiteSpace(key) Then
                        xmlMap(key.Trim()) = d
                    End If
                Next

                Dim sql = $"SELECT Id, Address FROM {tableFullName} ORDER BY Address"
                Dim temp As New List(Of DeviceInfo)()
                Using cmd As New SqlCommand(sql, cn)
                    Using rdr = cmd.ExecuteReader()
                        While rdr.Read()
                            Dim dpId As Integer = If(rdr.IsDBNull(0), 0, Convert.ToInt32(rdr(0)))
                            Dim address As String = If(rdr.IsDBNull(1), Nothing, Convert.ToString(rdr(1)).Trim())
                            If String.IsNullOrEmpty(address) Then Continue While
                            Dim x As Dictionary(Of String, String) = Nothing
                            If Not xmlMap.TryGetValue(address, x) Then Continue While
                            Dim identification As String = Nothing
                            Dim unused1 = x.TryGetValue("Identification", identification)
                            If String.IsNullOrEmpty(identification) OrElse
                                Not identification.StartsWith("20") Then Continue While
                            Dim identExt As String = Nothing
                            Dim unused = x.TryGetValue("IdentificationExtension", identExt)
                            If Not String.IsNullOrEmpty(identExt) AndAlso identExt.Length <> 4 Then Continue While

                            temp.Add(New DeviceInfo With {
                                .EcnsysDeviceIdent = identification,
                                .SoftwareIndexIdent = If(identExt, String.Empty),
                                .DeviceName = address
                            })
                        End While
                    End Using
                End Using
                devices = temp
                Return devices.Count > 0
            Catch ex As Exception
                Debug.WriteLine("EnrichDevicesFromDbWithXml error: " & ex.Message)
                Return False
            End Try
        End Function

    End Class
End Namespace