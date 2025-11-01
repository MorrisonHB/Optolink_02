Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions

Namespace Services
    ' Schneller, robuster Übersetzer für @@Textresource-Tokens basierend auf Textresource_de.xml
    Public NotInheritable Class TextResourceService
        Private Sub New()
        End Sub

        Private Shared ReadOnly tokenRegex As New Regex("@@(?<key>[A-Za-z0-9_.~\-]+)", RegexOptions.Compiled)
        ' Zusätzlich: beliebiger Inhalt in Anführungszeichen erfassen und prüfen
        Private Shared ReadOnly quotedContentRegex As New Regex("""(?<content>[^""]+)""",
                                                                RegexOptions.Compiled Or
                                                                RegexOptions.IgnoreCase)
        ' "~0" oder "~0x..." Suffix am Ende erkennen (z. B. viessmann.eventvaluetype.*~0 / ~0x006F)
        Private Shared ReadOnly tildeSuffixRegex As New Regex("~(0x[0-9A-Fa-f]+|[0-9]+)$", RegexOptions.Compiled)

        Private Shared ReadOnly keyStructureRegex As New Regex("^(?<prefix>(viessmann|ecn)\.(?<kind>eventtype|eventvaluetype|eventtypegroup)\.)((?<name>name)\.)?(?<rest>[^~]+?)(?<suffix>~(0x[0-9A-Fa-f]+|[0-9]+))?$",
                                                               RegexOptions.Compiled Or RegexOptions.IgnoreCase)

        Private Shared ReadOnly syncObj As New Object()
        Private Shared loaded As Boolean = False
        Private Shared dict As Dictionary(Of String, String)
        Private Shared lastPath As String
        Private Shared lastWrite As Date

        ' Öffentliche API: Inline-Übersetzung eines beliebigen Textes mit @@Schlüsseln
        Public Shared Function TranslateInline(input As String) As String
            If String.IsNullOrEmpty(input) Then Return input
            EnsureLoaded()

            Dim result As String = input

            ' SPEZIALFALL: Direkter Schlüssel ohne @@ 
            ' (z.B. ecnsysEventTypeGroup~..., viessmann.eventtypegroup..., ecnUnit....)
            If input.StartsWith("ecnsysEventTypeGroup~", StringComparison.OrdinalIgnoreCase) OrElse
       input.StartsWith("viessmann.eventtypegroup", StringComparison.OrdinalIgnoreCase) OrElse
       input.StartsWith("ecnUnit.", StringComparison.OrdinalIgnoreCase) Then
                Dim directTranslation = ResolveKey(input)
                If directTranslation IsNot Nothing Then
                    Return directTranslation
                End If
                ' Falls keine Übersetzung gefunden: Original zurückgeben
                Return input
            End If

            Do
                Dim changed As Boolean = False
                '1) @@token ersetzen
                Dim step1 =
          tokenRegex.Replace(result, New MatchEvaluator(Function(m As Match)
                                                            Dim key = m.Groups("key").Value
                                                            Dim repl = ResolveKey(key)
                                                            If repl Is Nothing Then Return m.Value
                                                            changed = True
                                                            Return repl
                                                        End Function))
                '2) Inhalte in Anführungszeichen prüfen/ersetzen
                Dim step2 =
     quotedContentRegex.Replace(step1,
      New MatchEvaluator(Function(m As Match)
                             Dim content = m.Groups("content").Value
                             Dim keyCandidate As String = Nothing
                             If content.StartsWith("@@") Then
                                 keyCandidate = content.Substring(2)
                             Else
                                 ' "viessmann.*" oder "ecn.*" als Schlüssel akzeptieren
                                 If Regex.IsMatch(content,
        "^(viessmann|ecn)[A-Za-z0-9_.~\-]+$",
      RegexOptions.IgnoreCase) Then
                                     keyCandidate = content
                                 End If
                             End If
                             If String.IsNullOrEmpty(keyCandidate) Then Return m.Value
                             Dim repl = ResolveKey(keyCandidate)
                             If repl Is Nothing Then Return m.Value
                             changed = True
                             Return """" & repl & """"
                         End Function))

                '3) KEINE Lokalisierung von HIDDEN/AND/OR durchführen (vom Nutzer nicht gewünscht)
                result = step2
                If Not changed Then Exit Do
            Loop

            Return result
        End Function

        Private Shared Function ResolveKey(key As String) As String
            If String.IsNullOrWhiteSpace(key) Then Return Nothing
            If dict Is Nothing Then Return Nothing

            ' Führendes @@ im Key entfernen (Normalisierung)
            If key.StartsWith("@@") Then key = key.Substring(2)

            Dim repl As String = Nothing

            '0) Versuche EXAKT direkten Treffer (wichtigste Methode!)
            If dict.TryGetValue(key, repl) Then
                ' Debug.WriteLine($"[ResolveKey] ? Direkter Treffer: '{key}' = '{repl}'")
                Return repl
            End If

            '0a) SPEZIELLE BEHANDLUNG FÜR ecnsysEventTypeGroup (Kategorie!)
            ' Format in DB:  ecnsysEventTypeGroup~VDensHC1_4~DiagnosisDiagnosis1
            ' Format in XML kann sein:
            '   1. Gerätespezifisch: ecnsysEventTypeGroup~VDensHC1_4~DiagnosisDiagnosis1 (exakt)
            '   2. Gerätespezifisch: ecnsysEventTypeGroup~CU401B_A~DiagnosisDiagnosis1 (anderes Gerät)
            '   3. Generisch:        ecnsysEventTypeGroup~_VITODATA~DiagnosisDiagnosis1 (Fallback)
            If key.StartsWith("ecnsysEventTypeGroup~", StringComparison.OrdinalIgnoreCase) Then
                Dim parts = key.Split("~"c)
                If parts.Length = 3 Then
                    ' Strategie: Erst gerätespezifisch suchen, dann generisch

                    ' 1. Versuche EXAKT (wurde schon oben gemacht, aber zur Sicherheit)
                    If dict.TryGetValue(key, repl) Then
                        ' Debug.WriteLine($"[ResolveKey] ? ecnsysEventTypeGroup exakt: '{key}' = '{repl}'")
                        Return repl
                    End If

                    ' 2. Versuche generischen Schlüssel: ecnsysEventTypeGroup~_VITODATA~{parts(2)}
                    Dim genericKey = $"ecnsysEventTypeGroup~_VITODATA~{parts(2)}"
                    If dict.TryGetValue(genericKey, repl) Then
                        ' Debug.WriteLine($"[ResolveKey] ? ecnsysEventTypeGroup generisch: '{key}' -> '{genericKey}' = '{repl}'")
                        Return repl
                    End If

                    ' Falls nicht gefunden: Keine Debug-Ausgabe
                End If
            End If

            '0b) SPEZIELLE BEHANDLUNG FÜR viessmann.eventtypegroup (Gruppe!)
            ' Wichtig: Kategorie und Gruppe haben unterschiedlich viele Tilden!
            ' Kategorie: viessmann.eventtypegroup.name.VScotHO1_20~35_Information (1 Tilde)
            ' Gruppe:    viessmann.eventtypegroup.name.VScotHO1_20~35_Information~10_Kessel (2 Tilden)
            If key.Contains("viessmann.eventtypegroup", StringComparison.OrdinalIgnoreCase) AndAlso key.Contains("~"c) Then
                ' Falls nicht gefunden: Keine Debug-Ausgabe
            End If

            '0c) EINFACHE LÖSUNG für andere Schlüssel mit Tilde
            ' Beispiel: "viessmann.eventtype.name.XYZ~0x1234" -> suche "XYZ"
            If key.Contains("~"c) Then
                Dim lastTildeIndex As Integer = key.LastIndexOf("~"c)
                Dim lastWord As String = key.Substring(lastTildeIndex + 1)
                If dict.TryGetValue(lastWord, repl) Then Return repl
                ' Versuche auch mit führender Tilde
                Dim withTilde As String = "~" & lastWord
                If dict.TryGetValue(withTilde, repl) Then Return repl
            End If

            ' Hilfsfunktion um .name.-Varianten eines Schlüssels zu erzeugen
            Dim addNameVariantsForKey =
    Function(k As String) As IEnumerable(Of String)
        Dim cands As New List(Of String)()
        Const evtValPrefix As String = "viessmann.eventvaluetype."
        Const evtTypePrefix As String = "viessmann.eventtype."
        Const evtTypeGroupPrefix As String = "viessmann.eventtypegroup."

        Dim addNameVariant =
             Sub(prefix As String)
                 If k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then
                     Dim rest = k.Substring(prefix.Length)
                     Const nameSeg As String = ".name."
                     If rest.Contains(nameSeg, StringComparison.OrdinalIgnoreCase) Then
                         ' .name. entfernen
                         Dim withoutName = prefix & Regex.Replace(rest, "(?i)\.name\.", ".")
                         cands.Add(withoutName)
                         ' Alternative Form: Name am Ende
                         cands.Add(prefix.TrimEnd("."c) & "." & rest.Replace(".name.", ".") & ".name")
                     Else
                         ' .name. einfügen nach Prefix
                         Dim withName = prefix & "name." & rest
                         cands.Add(withName)
                         ' Alternative Form: Name am Ende
                         cands.Add(prefix & rest & ".name")
                     End If
                 End If
             End Sub

        addNameVariant(evtValPrefix)
        addNameVariant(evtTypePrefix)
        addNameVariant(evtTypeGroupPrefix)
        Return cands
    End Function

            '1) Auch .name.-Varianten des Originalschlüssels testen (z. B. mit Suffix ~34)
            For Each cand In addNameVariantsForKey(key)
                If dict.TryGetValue(cand, repl) Then Return repl
            Next

            '2) Suffix ~0 oder ~0x... nur am Ende entfernen (NICHT Strukturteile wie bei ecnsysEventTypeGroup)
            Dim baseKey As String = key
            Dim mSuffix = tildeSuffixRegex.Match(key)
            If mSuffix.Success Then
                baseKey = key.Substring(0, mSuffix.Index)
                '2a) Erst Basis-Schlüssel ohne Suffix testen
                If dict.TryGetValue(baseKey, repl) Then Return repl
                '2b) .name.-Varianten des Basis-Schlüssels testen
                For Each cand In addNameVariantsForKey(baseKey)
                    If dict.TryGetValue(cand, repl) Then Return repl
                Next
            End If

            '3) Fallback: wenn EventValueType mit Ziffern-/Hex-Suffix und Basistext existiert, kombiniere mit Nummer
            If mSuffix.Success Then
                Dim numText As String = mSuffix.Value.TrimStart("~"c)
                Dim numDisplay As String
                Try
                    If numText.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                        Dim num As Integer = Convert.ToInt32(numText.Substring(2), 16)
                        numDisplay = num.ToString()
                    Else
                        numDisplay = Convert.ToInt32(numText).ToString()
                    End If
                Catch
                    numDisplay = numText
                End Try

                ' Versuche Basistexte aus Varianten zu ermitteln
                Dim baseText As String = Nothing
                If dict.TryGetValue(baseKey, baseText) Then
                    Return $"({numDisplay}) {baseText}"
                End If
                For Each cand In addNameVariantsForKey(baseKey)
                    If dict.TryGetValue(cand, baseText) Then
                        Return $"({numDisplay}) {baseText}"
                    End If
                Next
            End If

            '4) Letzter Fallback: Schlüssel heuristisch lesbar machen (unterstriche -> Leerzeichen) um @@ zu vermeiden
            Dim m = keyStructureRegex.Match(key)
            If m.Success Then
                Dim rest = m.Groups("rest").Value.Replace("_", " ")
                Dim suffix = m.Groups("suffix").Value
                If suffix.StartsWith("~"c) Then
                    Dim sfx = suffix.Substring(1)
                    Return $"({sfx}) {rest}"
                Else
                    Return rest
                End If
            End If

            '5) Keine weiteren Treffer -> Nothing
            Return Nothing
        End Function

        ' Kann genutzt werden, um mehrere Zeilen/Spalten schnell zu übersetzen
        Public Shared Sub TranslateColumn(dt As DataTable, columnName As String)
            If dt Is Nothing OrElse String.IsNullOrWhiteSpace(columnName) Then Return
            If Not dt.Columns.Contains(columnName) Then Return
            EnsureLoaded()

            Dim sourceCol As DataColumn = dt.Columns(columnName)

            ' ReadOnly-Spalten können nicht überschrieben werden -> Übersetzung überspringen
            If sourceCol.ReadOnly Then
                Return
            End If

            ' Direkt in die Spalte schreiben (nur wenn beschreibbar)
            Dim translatedCount As Integer = 0

            For Each row As DataRow In dt.Rows
                If row.IsNull(sourceCol) Then Continue For
                Dim s As String = Convert.ToString(row(sourceCol))
                If String.IsNullOrWhiteSpace(s) Then Continue For

                Dim translated = TranslateInline(s)

                ' Nur schreiben, wenn es überhaupt Änderungen gibt
                If Not Equals(s, translated) Then
                    row(sourceCol) = translated
                    translatedCount += 1
                End If
            Next
        End Sub

        ' Übersetzt die Parameter-Spalte: Wert bis zur Tilde (~) extrahieren und über viessmann.eventtype.name.{value} übersetzen
        Public Shared Sub TranslateParameterColumn(dt As DataTable, columnName As String)
            If dt Is Nothing OrElse String.IsNullOrWhiteSpace(columnName) Then Return
            If Not dt.Columns.Contains(columnName) Then Return
            EnsureLoaded()

            Dim sourceCol As DataColumn = dt.Columns(columnName)

            ' ReadOnly-Spalten können nicht überschrieben werden -> Übersetzung überspringen
            If sourceCol.ReadOnly Then
                Return
            End If

            ' Direkt in die Spalte schreiben (nur wenn beschreibbar)
            Dim translatedCount As Integer = 0
            For Each row As DataRow In dt.Rows
                If row.IsNull(sourceCol) Then Continue For
                Dim s As String = Convert.ToString(row(sourceCol))
                If String.IsNullOrWhiteSpace(s) Then Continue For

                ' Wert bis zur Tilde extrahieren
                Dim tildeIndex As Integer = s.IndexOf("~"c)
                If tildeIndex < 0 Then Continue For ' Keine Tilde gefunden -> überspringen

                Dim parameterName As String = s.Substring(0, tildeIndex)

                ' Schlüssel für Textressource erstellen: viessmann.eventtype.name.{parameterName}
                Dim key As String = $"viessmann.eventtype.name.{parameterName}"

                ' Versuche den Schlüssel aufzulösen
                Dim translated As String = ResolveKey(key)

                ' Wenn Übersetzung gefunden wurde, Zelle ersetzen
                If translated IsNot Nothing Then
                    row(sourceCol) = translated
                    translatedCount += 1
                End If
            Next
        End Sub

        ' Fügt eine neue Spalte "Beschreibung" hinzu und befüllt sie mit viessmann.eventtype.{ORIGINAL_parameterName}.description
        ' WICHTIG: Diese Methode MUSS VOR TranslateParameterColumn aufgerufen werden, da sie den Original-Namen benötigt!
        Public Shared Sub AddDescriptionColumn(dt As DataTable, parameterColumnName As String)
            If dt Is Nothing OrElse String.IsNullOrWhiteSpace(parameterColumnName) Then Return
            If Not dt.Columns.Contains(parameterColumnName) Then Return
            EnsureLoaded()

            ' Prüfe, ob Beschreibung-Spalte bereits existiert
            Const descriptionColumnName As String = "Beschreibung"
            If Not dt.Columns.Contains(descriptionColumnName) Then
                ' Neue Spalte hinzufügen
                Dim unused = dt.Columns.Add(descriptionColumnName, GetType(String))
            End If

            Dim paramCol As DataColumn = dt.Columns(parameterColumnName)
            Dim descCol As DataColumn = dt.Columns(descriptionColumnName)
            Dim populatedCount As Integer = 0

            For Each row As DataRow In dt.Rows
                If row.IsNull(paramCol) Then Continue For
                Dim s As String = Convert.ToString(row(paramCol))
                If String.IsNullOrWhiteSpace(s) Then Continue For

                ' Wert bis zur Tilde extrahieren (das ist der ORIGINAL-Parametername)
                Dim tildeIndex As Integer = s.IndexOf("~"c)
                If tildeIndex < 0 Then Continue For

                Dim originalParameterName As String = s.Substring(0, tildeIndex)

                ' Mehrere Schlüssel-Varianten versuchen
                Dim key1 As String = $"viessmann.eventtype.{originalParameterName}.description"
                Dim key2 As String = $"viessmann.eventtype.{originalParameterName}"
                Dim key3 As String = $"{originalParameterName}.description"
                Dim key4 As String = originalParameterName

                ' Versuche den Schlüssel aufzulösen (probiere alle Varianten)
                Dim description As String = Nothing
                description = ResolveKey(key1)
                If description Is Nothing Then description = ResolveKey(key2)
                If description Is Nothing Then description = ResolveKey(key3)
                If description Is Nothing Then description = ResolveKey(key4)

                ' Beschreibung in neue Spalte schreiben
                If description IsNot Nothing AndAlso Not String.IsNullOrEmpty(description) Then
                    Dim textFertig = description _
                        .Replace("##ecnnewline##", Environment.NewLine) _
                        .Replace("##ecntab##", vbTab)
                    row(descCol) = textFertig
                    populatedCount += 1
                End If
            Next
        End Sub

        ''' <summary>
        ''' Fügt eine neue Spalte "OriginalParameter" hinzu und speichert den KOMPLETTEN Original-Wert VOR der Übersetzung
        ''' WICHTIG: Diese Methode MUSS VOR TranslateParameterColumn aufgerufen werden!
        ''' </summary>
        Public Shared Sub AddOriginalParameterColumn(dt As DataTable, parameterColumnName As String)
            If dt Is Nothing OrElse String.IsNullOrWhiteSpace(parameterColumnName) Then Return
            If Not dt.Columns.Contains(parameterColumnName) Then Return

            ' Prüfe, ob OriginalParameter-Spalte bereits existiert
            Const originalColumnName As String = "OriginalParameter"
            If Not dt.Columns.Contains(originalColumnName) Then
                ' Neue Spalte hinzufügen
                Dim unused = dt.Columns.Add(originalColumnName, GetType(String))
            End If

            Dim paramCol As DataColumn = dt.Columns(parameterColumnName)
            Dim originalCol As DataColumn = dt.Columns(originalColumnName)

            ' Kopiere den KOMPLETTEN Original-Wert (z.B. "GWG_Flamme~0x55D3")
            For Each row As DataRow In dt.Rows
                If row.IsNull(paramCol) Then Continue For
                Dim originalValue As String = Convert.ToString(row(paramCol))
                row(originalCol) = originalValue
            Next

            Debug.WriteLine($"[AddOriginalParameterColumn] Original-Parameter-Spalte erstellt für {dt.Rows.Count} Zeilen")
        End Sub

        ''' <summary>
      ''' Übersetzt die Unit-Spalte: "ecnUnit." wird durch die Übersetzung ersetzt
        ''' </summary>
     Public Shared Sub TranslateUnitColumn(dt As DataTable, columnName As String)
       If dt Is Nothing OrElse String.IsNullOrWhiteSpace(columnName) Then Return
  If Not dt.Columns.Contains(columnName) Then Return
 EnsureLoaded()

   Dim sourceCol As DataColumn = dt.Columns(columnName)

    ' ReadOnly-Spalten können nicht überschrieben werden
  If sourceCol.ReadOnly Then
        Return
        End If

         Dim translatedCount As Integer = 0
            For Each row As DataRow In dt.Rows
    If row.IsNull(sourceCol) Then Continue For
 Dim s As String = Convert.ToString(row(sourceCol))
If String.IsNullOrWhiteSpace(s) Then Continue For

         ' ecnUnit. Prefix entfernen und als Schlüssel verwenden
     If s.StartsWith("ecnUnit.", StringComparison.OrdinalIgnoreCase) Then
      Dim unitKey = s ' z.B. "ecnUnit.Minuten"

       ' Versuche Übersetzung mit vollem Schlüssel
     Dim translated = ResolveKey(unitKey)

       ' Fallback: Nur den Teil nach "ecnUnit." verwenden
        If translated Is Nothing Then
 Dim shortKey = s.Substring(8) ' "Minuten"
    translated = ResolveKey(shortKey)
          End If

     ' Wenn Übersetzung gefunden wurde, ersetzen
         If translated IsNot Nothing Then
   row(sourceCol) = translated
   translatedCount += 1
      Else
  ' Fallback: Entferne einfach "ecnUnit." Prefix
     row(sourceCol) = s.Substring(8)
    translatedCount += 1
 End If
   End If
Next

 Debug.WriteLine($"[TranslateUnitColumn] {translatedCount} von {dt.Rows.Count} Units übersetzt")
      End Sub

        Private Shared Sub EnsureLoaded()
            SyncLock syncObj
                Dim path = TryResolvePath()
                If String.IsNullOrEmpty(path) Then
                    If Not loaded Then
                        dict = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                        loaded = True
                    End If
                    Return
                End If
                Dim fi As New FileInfo(path)
                Dim needsReload As Boolean =
                    (Not loaded) OrElse
                    (Not String.Equals(path, lastPath, StringComparison.OrdinalIgnoreCase)) OrElse
                    (fi.LastWriteTimeUtc <> lastWrite)
                If Not needsReload Then Return

                dict = LoadDictionaryFast(path)
                loaded = True
                lastPath = path
                lastWrite = fi.LastWriteTimeUtc
            End SyncLock
        End Sub

        ' Robustes, zeilenbasiertes Parsen einzelner TextResource-Zeilen. Toleriert fehlerhafte XML-Zeilen.
        Private Shared Function LoadDictionaryFast(path As String) As Dictionary(Of String, String)
            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

            Try
                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    ' Automatische Encoding-Erkennung: UTF-8 oder UTF-16
                    Using sr As New StreamReader(fs, detectEncodingFromByteOrderMarks:=True)
                        While Not sr.EndOfStream
                            Dim line = sr.ReadLine()
                            If String.IsNullOrEmpty(line) Then Continue While
                            If line.IndexOf("<TextResource", StringComparison.OrdinalIgnoreCase) < 0 Then Continue While

                            ' Label erfassen
                            Dim label As String = Nothing
                            Dim mLabel = Regex.Match(line, "Label=""(.*?)""")
                            If mLabel.Success Then
                                label = WebUtility.HtmlDecode(mLabel.Groups(1).Value)
                            End If

                            ' Value: im Zweifel den letzten Value= nehmen (manche Zeilen enthalten versehentlich zwei Value-Attribute)
                            Dim sValue As String = Nothing
                            Dim mVals = Regex.Matches(line, "Value=""(.*?)""")
                            If mVals IsNot Nothing AndAlso mVals.Count > 0 Then
                                sValue = WebUtility.HtmlDecode(mVals(mVals.Count - 1).Groups(1).Value)
                            End If

                            If Not String.IsNullOrWhiteSpace(label) Then
                                ' Normalisiere Label: führendes @@ entfernen für hauptsächlichen Lookup
                                Dim normalized = If(label.StartsWith("@@"), label.Substring(2), label)
                                If sValue Is Nothing Then sValue = String.Empty

                                ' Speichere BEIDE Varianten
                                Dim unused1 = result.TryAdd(label, sValue)
                                If Not String.Equals(label, normalized, StringComparison.Ordinal) Then
                                    Dim unused2 = result.TryAdd(normalized, sValue)
                                End If
                            End If
                        End While
                    End Using
                End Using
            Catch ex As Exception
                ' Bei Encoding-Fehlern: Fehler abfangen und leeres Dictionary zurückgeben
                Debug.WriteLine($"[TextResourceService] Fehler beim Laden von Textresource_de.xml: {ex.Message}")
            End Try

            Return result
        End Function

        ' Sucht die Datei an einigen naheliegenden Orten relativ zum Startverzeichnis
        Private Shared Function TryResolvePath() As String
            Try
                Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
                Dim candidates As String() = {
                    Path.Combine(baseDir, "XML", "Textresource_de.xml"),
                    Path.Combine(baseDir, "Optolink_02", "XML", "Textresource_de.xml"),
                    Path.Combine(baseDir, "..", "XML", "Textresource_de.xml"),
                    Path.Combine(baseDir, "..", "..", "XML", "Textresource_de.xml"),
                    Path.Combine(baseDir, "..", "..", "..", "XML", "Textresource_de.xml"),
                    Path.Combine(baseDir, "..", "..", "..", "..", "XML", "Textresource_de.xml")
                }
                For Each p In candidates
                    Dim full = Path.GetFullPath(p)
                    If File.Exists(full) Then Return full
                Next

                ' Letzter Fallback: rekursiv nach Textresource_de.xml in übergeordneten Ordnern suchen
                Dim dir As New DirectoryInfo(baseDir)
                For i As Integer = 1 To 6
                    If dir Is Nothing Then Exit For
                    Dim guess = Path.Combine(dir.FullName, "XML", "Textresource_de.xml")
                    If File.Exists(guess) Then Return guess
                    dir = dir.Parent
                Next
            Catch
            End Try

            Return Nothing
        End Function

    End Class
End Namespace