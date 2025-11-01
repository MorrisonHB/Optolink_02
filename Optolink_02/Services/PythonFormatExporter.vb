Imports System.IO
Imports System.Text

Namespace Services
    Public NotInheritable Class PythonFormatExporter
        Private Sub New()
        End Sub

        Public Shared Sub ExportToPythonFormat(dt As DataTable, deviceName As String, outputPath As String)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then Throw New ArgumentException("DataTable ist leer")

            Try
                Dim groupConditions = GetGroupConditions(deviceName)
                Dim groupNameMap = BuildGroupNameMap(deviceName)
                Dim hierarchy = BuildHierarchy(dt)

                ' WICHTIG: Schlie�e alle offenen FileStreams BEVOR wir schreiben!
                GC.Collect()
                GC.WaitForPendingFinalizers()

                ' Schreibe in tempor�re Datei und benenne dann um (atomare Operation)
                Dim tempPath = outputPath & ".tmp"

                Try
                    Using writer As New StreamWriter(tempPath, False, Encoding.UTF8)
                        writer.WriteLine(deviceName)
                        writer.WriteLine(New String("="c, deviceName.Length))
                        writer.WriteLine()
                        WriteHierarchy(writer, hierarchy, dt, groupConditions, groupNameMap)
                    End Using

                    ' Versuche alte Datei zu l�schen - wenn das fehlschl�gt, ist sie vermutlich ge�ffnet
                    Try
                        If File.Exists(outputPath) Then
                            File.Delete(outputPath)
                        End If
                        File.Move(tempPath, outputPath)
                        Debug.WriteLine($"[Export] Erfolgreich: {groupConditions.Count} Gruppen-Conditions nach {outputPath}")
                    Catch deleteEx As IOException
                        ' Datei ist vermutlich in einem Editor ge�ffnet - verwende eindeutigen Namen
                        Dim timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss")
                        Dim dir = Path.GetDirectoryName(outputPath)
                        Dim fileNameWithoutExt = Path.GetFileNameWithoutExtension(outputPath)
                        Dim ext = Path.GetExtension(outputPath)
                        Dim uniquePath = Path.Combine(dir, $"{fileNameWithoutExt}_{timestamp}{ext}")

                        File.Move(tempPath, uniquePath)
                        Debug.WriteLine($"[Export] WARNUNG: Original-Datei konnte nicht �berschrieben werden (vermutlich ge�ffnet)")
                        Debug.WriteLine($"[Export] Neue Datei erstellt: {uniquePath}")

                        MessageBox.Show($"Die Datei konnte nicht �berschrieben werden (vermutlich ge�ffnet).{Environment.NewLine}{Environment.NewLine}Neue Datei erstellt:{Environment.NewLine}{uniquePath}",
                                        "Export erfolgreich",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information)
                    End Try

                Catch ioEx As IOException
                    Debug.WriteLine($"[Export IO-ERROR] {ioEx.Message}")
                    Debug.WriteLine($"[Export IO-STACK] {ioEx.StackTrace}")

                    ' Versuche temp-Datei zu l�schen
                    Try
                        If File.Exists(tempPath) Then File.Delete(tempPath)
                    Catch
                    End Try

                    Throw New IOException($"Fehler beim Schreiben der Datei: {ioEx.Message}", ioEx)
                End Try

            Catch ex As Exception
                Debug.WriteLine($"[Export ERROR] {ex.Message}")
                Throw
            End Try
        End Sub

        Private Shared Function BuildGroupNameMap(deviceName As String) As Dictionary(Of String, String)
            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim sql = "SELECT etg.[Name] FROM ecnEventTypeGroup etg INNER JOIN ecnDatapointType dp ON dp.Id=etg.DataPointTypeId WHERE dp.Address=@dev AND etg.ParentId IS NOT NULL AND etg.ParentId<>-1"
            Using conn As New SqlConnection(SqlQueries.BuildConnectionString()), cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@dev", deviceName)
                conn.Open()
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim orig = reader("Name").ToString()
                        Dim trans = TextResourceService.TranslateInline(orig)
                        result(trans) = orig
                    End While
                End Using
            End Using
            Return result
        End Function

        Private Shared Function GetGroupConditions(deviceName As String) As Dictionary(Of String, String)
            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            ' Use COALESCE to try multiple sources for the condition value:
            ' 1. EnumReplaceValue (primary source)
            ' 2. evIn.[Name] if it starts with @@ (fallback for untranslated values)
            ' 3. EnumAddressValue as string (final fallback)
            Dim sql = "SELECT etg.[Name] AS G, g.[Type] AS T, etCond.[Name] AS C, dc.[Condition] AS O, " &
                      "COALESCE(NULLIF(evIn.EnumReplaceValue, ''), " &
                      "CASE WHEN evIn.[Name] LIKE '@@%' THEN evIn.[Name] END, " &
                      "CAST(evIn.EnumAddressValue AS nvarchar(100)), '') AS V " &
                      "FROM ecnEventTypeGroup etg " &
                      "INNER JOIN ecnDatapointType dp ON dp.Id=etg.DataPointTypeId " &
                      "INNER JOIN ecnDisplayConditionGroup g ON g.EventTypeGroupIdDest=etg.Id " &
                      "INNER JOIN ecnDisplayCondition dc ON dc.ConditionGroupId=g.Id " &
                      "INNER JOIN ecnEventType etCond ON etCond.Id=dc.EventTypeIdCondition " &
                      "LEFT JOIN ecnEventValueType evIn ON evIn.Id=dc.EventTypeValueCondition " &
                      "WHERE dp.Address=@dev " &
                      "ORDER BY etg.[Name],g.Id"
            Using conn As New SqlConnection(SqlQueries.BuildConnectionString()), cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@dev", deviceName)
                conn.Open()
                Using reader = cmd.ExecuteReader()
                    Dim curG As String = Nothing, conds As New List(Of String)(), logOp = "OR"
                    While reader.Read()
                        Dim g = reader("G").ToString(), t = If(IsDBNull(reader("T")), 1, CInt(reader("T")))
                        If curG IsNot Nothing AndAlso g <> curG Then
                            If conds.Count > 0 Then result(curG) = $" HIDDEN:({String.Join($" {logOp} ", conds)})"
                            conds.Clear()
                        End If
                        curG = g : logOp = If(t = 1, "AND", "OR")

                        ' Condition-Name �bersetzen
                        Dim conditionName = reader("C").ToString()
                        Dim translatedCondName = TextResourceService.TranslateInline(conditionName)

                        ' Wert �bersetzen
                        Dim conditionValue = If(IsDBNull(reader("V")), "", reader("V").ToString())
                        Dim translatedValue = TextResourceService.TranslateInline(conditionValue)

                        ' Debug-Logging nur bei tats�chlich fehlgeschlagener �bersetzung
                        ' (d.h. wenn der Wert mit @@ anfing UND nach �bersetzung immer noch @@ enth�lt)
                        If conditionValue.StartsWith("@@") AndAlso translatedValue.StartsWith("@@") AndAlso
                           conditionValue.Equals(translatedValue, StringComparison.Ordinal) Then
                            ' �bersetzung ist komplett fehlgeschlagen (Wert unver�ndert)
                            Debug.WriteLine($"[GetGroupConditions] Warnung: �bersetzung nicht gefunden: {conditionValue}")
                        End If

                        ' Operator
                        Dim op = If(IsDBNull(reader("O")), 0, CInt(reader("O")))
                        Dim opStr = GetOperatorString(op)

                        ' Baue Bedingung: "Name"="Wert" (OHNE doppelte Anf�hrungszeichen!)
                        ' WICHTIG: Keine zus�tzlichen Anf�hrungszeichen um translatedCondName/translatedValue!
                        conds.Add($"{translatedCondName}{opStr}""{translatedValue}""")
                    End While

                    If curG IsNot Nothing AndAlso conds.Count > 0 Then
                        result(curG) = $" HIDDEN:({String.Join($" {logOp} ", conds)})"
                    End If
                End Using
            End Using

            Debug.WriteLine($"[GetGroupConditions] {result.Count} Gruppen-Conditions erstellt")
            Return result
        End Function


        ''' <summary>
        ''' Mappt Operator-Codes auf Symbole (IDENTISCH zu SqlQueries.vb!)
        ''' Unicode-Zeichen als Escapes f�r bessere Kompatibilit�t
        ''' </summary>
        Private Shared Function GetOperatorString(op As Integer) As String
            Select Case op
                Case 0 : Return "="
                Case 1 : Return ChrW(&H2260) ' ? (Not Equal To)
                Case 2 : Return "greater than"
                Case 3 : Return ChrW(&H2265) ' ? (Greater Than Or Equal To)
                Case 4 : Return "<"
                Case 5 : Return ChrW(&H2264) ' ? (Less Than Or Equal To)
                Case Else : Return "="
            End Select
        End Function

        Private Shared Function BuildHierarchy(dt As DataTable) _
            As Dictionary(Of String, Dictionary(Of String, List(Of DataRow)))
            Dim result As New Dictionary(Of String,
                Dictionary(Of String, List(Of DataRow)))(StringComparer.OrdinalIgnoreCase)
            For Each row As DataRow In dt.Rows
                Dim kat = If(Convert.ToString(row("Kategorie")),
                    "Ohne").Trim(), grp = If(Convert.ToString(row("Gruppe")), "Ohne").Trim()

                ' TryGetValue statt ContainsKey + Indexer
                Dim katDict As Dictionary(Of String, List(Of DataRow)) = Nothing
                If Not result.TryGetValue(kat, katDict) Then
                    katDict = New Dictionary(Of String, List(Of DataRow))(StringComparer.OrdinalIgnoreCase)
                    result(kat) = katDict
                End If

                ' TryGetValue statt ContainsKey + Indexer
                Dim grpList As List(Of DataRow) = Nothing
                If Not katDict.TryGetValue(grp, grpList) Then
                    grpList = New List(Of DataRow)()
                    katDict(grp) = grpList
                End If

                grpList.Add(row)
            Next
            Return result
        End Function

        Private Shared Sub WriteHierarchy(w As StreamWriter, h As Dictionary(Of String, Dictionary(Of String, List(Of DataRow))), dt As DataTable, gc As Dictionary(Of String, String), gm As Dictionary(Of String, String))
            For Each kat In h.Keys.OrderBy(Function(k) k)
                w.WriteLine($"# {kat}")
                For Each grp In h(kat).Keys.OrderBy(Function(g) g)
                    ' TryGetValue statt ContainsKey + Indexer
                    Dim origGrp As String = Nothing
                    If Not gm.TryGetValue(grp, origGrp) Then
                        origGrp = grp
                    End If

                    Dim cond As String = ""
                    gc.TryGetValue(origGrp, cond)

                    w.WriteLine($"- {grp}{cond}")
                    For Each row In h(kat)(grp)
                        Dim pName = If(Convert.ToString(row("Parameter")), "?").Trim(), eId = If(dt.Columns.Contains("EventTypeId"), $" ({row("EventTypeId")})", "")
                        Dim orig = If(dt.Columns.Contains("OriginalParameter"), Convert.ToString(row("OriginalParameter")), "").Trim(), dType = If(dt.Columns.Contains("DataType"), Convert.ToString(row("DataType")), "").Trim()
                        Dim addr = If(String.IsNullOrWhiteSpace(orig), "", If(String.IsNullOrWhiteSpace(dType), $" [{orig}]", $" [{orig} ({dType})]"))
                        Dim pCond = If(Convert.ToString(row("Condition")), "").Trim(), hidden = If(String.IsNullOrWhiteSpace(pCond), "", If(pCond.StartsWith("HIDDEN:("), $" {pCond}", $" HIDDEN:({pCond})"))
                        w.WriteLine($"    - {pName}{eId}{addr}{hidden}")
                    Next
                Next
                w.WriteLine()
            Next
        End Sub
    End Class
End Namespace
