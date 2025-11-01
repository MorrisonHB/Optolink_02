Imports System.Data
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Namespace Services
    ''' <summary>
    ''' Exportiert Gerätedaten im Python-Format (wie PrintEventsForDatapoint.py)
    ''' </summary>
    Public NotInheritable Class PythonFormatExporter
      Private Sub New()
        End Sub

        ''' <summary>
        ''' Exportiert eine DataTable im Python-Format in eine Textdatei
        ''' </summary>
        Public Shared Sub ExportToPythonFormat(dt As DataTable, deviceName As String, outputPath As String)
      If dt Is Nothing OrElse dt.Rows.Count = 0 Then
       Throw New ArgumentException("DataTable ist leer oder Nothing.")
            End If

Using writer As New StreamWriter(outputPath, False, Encoding.UTF8)
  ' Header: Gerätename
    writer.WriteLine(deviceName)
 writer.WriteLine(New String("="c, deviceName.Length))
   writer.WriteLine()

       ' Gruppiere Daten hierarchisch: Kategorie > Gruppe > Parameter
              Dim hierarchy = BuildHierarchy(dt)

   ' Schreibe hierarchische Struktur
         WriteHierarchy(writer, hierarchy, dt)
            End Using
     End Sub

        ''' <summary>
        ''' Baut eine hierarchische Struktur: Kategorie -> Gruppen -> Parameter
        ''' </summary>
        Private Shared Function BuildHierarchy(dt As DataTable) As Dictionary(Of String,
            Dictionary(Of String, List(Of DataRow)))
            Dim result As New Dictionary(Of String, Dictionary(Of String, List(Of DataRow)))(StringComparer.OrdinalIgnoreCase)

            For Each row As DataRow In dt.Rows
                Dim kategorie As String = If(Convert.ToString(row("Kategorie")), "Ohne Kategorie").Trim()
                Dim gruppe As String = If(Convert.ToString(row("Gruppe")), "Ohne Gruppe").Trim()

                Dim kategorie_value As Dictionary(Of String, List(Of DataRow)) = Nothing
                ' Kategorie anlegen falls nicht vorhanden
                If Not result.TryGetValue(kategorie, kategorie_value) Then
                    kategorie_value = New Dictionary(Of String, List(Of DataRow))(StringComparer.OrdinalIgnoreCase)
                    result(kategorie) = kategorie_value
                End If

                Dim gruppe_value As List(Of DataRow) = Nothing
                ' Gruppe innerhalb Kategorie anlegen
                If Not kategorie_value.TryGetValue(gruppe, gruppe_value) Then
                    gruppe_value = New List(Of DataRow)()
                    kategorie_value(gruppe) = gruppe_value
                End If

                gruppe_value.Add(row)
            Next

            Return result
        End Function

        ''' <summary>
        ''' Schreibt die Hierarchie in den Writer
        ''' </summary>
        Private Shared Sub WriteHierarchy(writer As StreamWriter,
                                          hierarchy As Dictionary(Of String,
                                          Dictionary(Of String,
                                          List(Of DataRow))), dt As DataTable)

            ' Sortiere Kategorien alphabetisch
            Dim sortedKategorien = hierarchy.Keys.OrderBy(Function(k) k).ToList()

            For Each kategorie In sortedKategorien
                ' Kategorie-Überschrift mit #
                writer.WriteLine($"# {kategorie}")

                ' Hole Gruppen dieser Kategorie
                Dim gruppen = hierarchy(kategorie)
                Dim sortedGruppen = gruppen.Keys.OrderBy(Function(g) g).ToList()

                For Each gruppe In sortedGruppen
                    ' Gruppe-Überschrift mit -
                    ' Hole erste Row dieser Gruppe für EventTypeId (falls vorhanden)
                    Dim firstRow = gruppen(gruppe).FirstOrDefault()
                    Dim gruppeId As String = ""
                    If firstRow IsNot Nothing AndAlso dt.Columns.Contains("EventTypeId") Then
                        gruppeId = $" ({Convert.ToString(firstRow("EventTypeId"))})"
                    End If

                    ' Ermittle Gruppe-Condition (alle Parameter der Gruppe sollten gleiche Gruppen-Condition haben)
                    Dim gruppeCondition As String = GetGruppenCondition(gruppen(gruppe), dt)

                    writer.WriteLine($"- {gruppe}{gruppeId}{gruppeCondition}")

                    ' Parameter dieser Gruppe
                    For Each row In gruppen(gruppe)
                        WriteParameter(writer, row, dt)
                    Next
                Next

                writer.WriteLine() ' Leerzeile nach Kategorie
            Next
        End Sub

        ''' <summary>
        ''' Ermittelt die Gruppen-Condition (HIDDEN auf Gruppenebene)
        ''' Im Python-Code wird dies über EventTypeGroupIdDest ermittelt
        ''' </summary>
        Private Shared Function GetGruppenCondition(parameters As List(Of DataRow), dt As DataTable) As String
    ' TODO: Implementierung der Gruppen-Level Conditions
            ' Dies würde eine separate Abfrage aus DPDefinitions.xml oder der Datenbank erfordern
  ' Für jetzt: leer zurückgeben
            Return String.Empty
        End Function

        ''' <summary>
        ''' Schreibt einen einzelnen Parameter im Python-Format
        ''' </summary>
        Private Shared Sub WriteParameter(writer As StreamWriter,
                                     row As DataRow,
                                     dt As DataTable)
            Try
                ' Parameter-Name
                Dim parameterName As String = If(Convert.ToString(row("Parameter")), "Unbekannt").Trim()

                ' EventTypeId (falls vorhanden)
                Dim eventId As String = ""
                If dt.Columns.Contains("EventTypeId") Then
                    eventId = $" ({Convert.ToString(row("EventTypeId"))})"
                End If

                ' Address mit vollem Namen (z.B. [GWG_Flamme~0x55D3 (Byte)])
                Dim addressInfo As String = BuildAddressInfo(row, dt)

                ' Hidden-Condition - WICHTIG: Verwende die bereits prozessierte Condition aus der DB!
                Dim condition As String = If(Convert.ToString(row("Condition")), "").Trim()
                Dim hiddenStr As String = ""
                If Not String.IsNullOrWhiteSpace(condition) AndAlso condition.StartsWith("HIDDEN:(") Then
                    ' Entferne äußere Klammern: HIDDEN:((...)  -> HIDDEN:(...)
                    hiddenStr = " " & SimplifyHiddenCondition(condition)
                End If

                ' Schreibe Zeile: "    - Parametername (ID) [Adressinfo] HIDDEN:(condition)"
                writer.WriteLine($"    - {parameterName}{eventId}{addressInfo}{hiddenStr}")

            Catch ex As Exception
                Debug.WriteLine($"[ERROR] WriteParameter: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Baut die Adress-Information im Format [Name~Address (DataType)]
        ''' WICHTIG: Verwendet "OriginalParameter"-Spalte die den KOMPLETTEN Wert VOR der Übersetzung enthält
        ''' </summary>
        Private Shared Function BuildAddressInfo(row As DataRow, dt As DataTable) As String
            Try
                ' WICHTIG: Die "OriginalParameter"-Spalte enthält den KOMPLETTEN Original-Wert
                ' z.B. "GWG_Flamme~0x55D3" oder "Schaltzeiten_A1M1_HK~SchaltzeitenA1M1_HK"
                Dim originalParam As String = ""

                If dt.Columns.Contains("OriginalParameter") Then
                    originalParam = If(Convert.ToString(row("OriginalParameter")), "").Trim()
                End If

                ' Fallback: Wenn OriginalParameter nicht existiert, versuche aus Parameter-Spalte
                If String.IsNullOrWhiteSpace(originalParam) Then
                    originalParam = If(Convert.ToString(row("Parameter")), "").Trim()
                End If

                ' Prüfe ob überhaupt ein Wert vorhanden ist
                If String.IsNullOrWhiteSpace(originalParam) Then
                    Return ""
                End If

                ' DIREKTER ANSATZ: Verwende den KOMPLETTEN OriginalParameter-Wert
                ' Format ist bereits korrekt: "Parametername~Adresse"
                Dim addressName As String = originalParam

                ' DataType
                Dim dataType As String = If(Convert.ToString(row("DataType")), "").Trim()

                ' Kombiniere: [OriginalParameter (DataType)]
                If Not String.IsNullOrWhiteSpace(dataType) Then
                    Return $" [{addressName} ({dataType})]"
                Else
                    Return $" [{addressName}]"
                End If

            Catch ex As Exception
                Debug.WriteLine($"[ERROR] BuildAddressInfo: {ex.Message}")
                Return ""
            End Try
        End Function

        ''' <summary>
        ''' Vereinfacht die HIDDEN-Condition (entfernt doppelte Klammern)
        ''' WICHTIG: Die Condition kommt bereits mit Shortcuts aus der SQL-Query!
        ''' </summary>
        Private Shared Function SimplifyHiddenCondition(condition As String) As String
            Try
                If Not condition.StartsWith("HIDDEN:(") Then
                    Return condition
                End If

                ' Entferne "HIDDEN:(" am Anfang und ")" am Ende
                Dim innerContent = condition.Substring(8, condition.Length - 9)

                ' Prüfe ob doppelte Klammern: ((...)  -> (...)
                If innerContent.StartsWith("("c) AndAlso innerContent.EndsWith(")"c) Then
                    ' Zähle öffnende/schließende Klammern
                    Dim openCount = 0
                    Dim closeCount = 0
                    Dim isFullyWrapped = True

                    For i = 0 To innerContent.Length - 1
                        If innerContent(i) = "("c Then
                            openCount += 1
                        ElseIf innerContent(i) = ")"c Then
                            closeCount += 1
                            ' Wenn vor dem Ende ausgeglichen, nicht vollständig umhüllt
                            If i < innerContent.Length - 1 AndAlso openCount = closeCount Then
                                isFullyWrapped = False
                                Exit For
                            End If
                        End If
                    Next

                    ' Wenn vollständig in einer Klammer-Ebene, entferne äußere Klammern
                    If isFullyWrapped AndAlso openCount = closeCount Then
                        innerContent = innerContent.Substring(1, innerContent.Length - 2)
                    End If
                End If

                Return $"HIDDEN:({innerContent})"

            Catch ex As Exception
                Debug.WriteLine($"[ERROR] SimplifyHiddenCondition: {ex.Message}")
                Return condition
            End Try
        End Function

    End Class
End Namespace
