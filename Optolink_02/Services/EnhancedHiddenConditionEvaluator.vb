Imports System.Text.RegularExpressions

Namespace Services
    ''' <summary>
    ''' Verbesserte Auswertung von HIDDEN-Conditions mit vollständiger OR/AND/NOT-Unterstützung
    ''' Unterstützt: =, !=, ≠, <>, OR, AND
    ''' </summary>
    Public NotInheritable Class EnhancedHiddenConditionEvaluator
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Wertet eine HIDDEN-Condition aus
        ''' </summary>
        ''' <param name="condition">HIDDEN-Condition String (z.B. "HIDDEN:(00_WWS="1 A1" OR 00_WWS="2 A1  + WW")")</param>
        ''' <param name="configValues">Dictionary mit Konfigurationswerten (Key=ConfigKey, Value=Wert)</param>
        ''' <returns>True wenn die Bedingung erfüllt ist (d.h. das Element sollte HIDDEN sein)</returns>
        Public Shared Function EvaluateCondition(condition As String, configValues As Dictionary(Of String, String)) As Boolean
            If String.IsNullOrWhiteSpace(condition) Then
                Return False
            End If

            ' Entferne "HIDDEN:(" am Anfang und ")" am Ende
            Dim cleaned = condition.Trim()
            If cleaned.StartsWith("HIDDEN:(", StringComparison.OrdinalIgnoreCase) Then
                cleaned = cleaned.Substring(8)
            End If
            If cleaned.EndsWith(")"c) Then
                cleaned = cleaned.Substring(0, cleaned.Length - 1)
            End If

            ' Normalisiere ≠ zu !=
            cleaned = cleaned.Replace("≠", "!=")

            Try
                Return EvaluateExpression(cleaned, configValues)
            Catch ex As Exception
                Debug.WriteLine($"[EVALUATE ERROR] Fehler bei Auswertung von '{condition}': {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Wertet einen logischen Ausdruck aus (unterstützt OR/AND)
        ''' </summary>
        Private Shared Function EvaluateExpression(expression As String, configValues As Dictionary(Of String, String)) As Boolean
            If String.IsNullOrWhiteSpace(expression) Then
                Return False
            End If

            ' Ersetze Konfigurationswerte in der Expression
            Dim evaluatedExpr = ReplaceConfigValues(expression, configValues)

            ' Prüfe auf OR-Operator (hat niedrigere Priorität als AND)
            If ContainsOperatorOutsideQuotes(evaluatedExpr, " OR ") Then
                Dim orParts = SplitByOperatorOutsideQuotes(evaluatedExpr, " OR ")
                ' Bei OR: mindestens eine Bedingung muss wahr sein
                For Each part In orParts
                    If EvaluateAndExpression(part.Trim(), configValues) Then
                        Return True
                    End If
                Next
                Return False
            Else
                ' Keine OR-Verknüpfung, evaluiere als AND-Expression
                Return EvaluateAndExpression(evaluatedExpr, configValues)
            End If
        End Function

        ''' <summary>
        ''' Wertet eine AND-Expression aus
        ''' </summary>
        Private Shared Function EvaluateAndExpression(expression As String, configValues As Dictionary(Of String, String)) As Boolean
            If String.IsNullOrWhiteSpace(expression) Then
                Return False
            End If

            ' Prüfe auf AND-Operator
            If ContainsOperatorOutsideQuotes(expression, " AND ") Then
                Dim andParts = SplitByOperatorOutsideQuotes(expression, " AND ")
                ' Bei AND: alle Bedingungen müssen wahr sein
                For Each part In andParts
                    If Not EvaluateComparison(part.Trim()) Then
                        Return False
                    End If
                Next
                Return True
            Else
                ' Keine AND-Verknüpfung, evaluiere als einfachen Vergleich
                Return EvaluateComparison(expression)
            End If
        End Function

        ''' <summary>
        ''' Wertet einen einfachen Vergleich aus (z.B. "0"="0" oder "1"!="2")
        ''' </summary>
        Private Shared Function EvaluateComparison(comparison As String) As Boolean
            If String.IsNullOrWhiteSpace(comparison) Then
                Return False
            End If

            Dim trimmed = comparison.Trim()

            ' Suche nach Vergleichsoperatoren: !=, <>, =
            ' Wichtig: != und <> vor = prüfen!
            Dim operators As String() = {"!=", "<>", "="}
            
            For Each op In operators
                Dim parts = SplitByOperatorOnce(trimmed, op)
                If parts.Length = 2 Then
                    Dim left = CleanValue(parts(0))
                    Dim right = CleanValue(parts(1))
                    
                    ' Vergleiche die Werte
                    Select Case op
                        Case "="
                            Return CompareValues(left, right, True)
                        Case "!=", "<>"
                            Return CompareValues(left, right, False)
                    End Select
                End If
            Next

            ' Kein Operator gefunden
            Return False
        End Function

        ''' <summary>
        ''' Vergleicht zwei Werte (unterstützt String- und numerischen Vergleich)
        ''' </summary>
        ''' <param name="left">Linker Wert</param>
        ''' <param name="right">Rechter Wert</param>
        ''' <param name="shouldEqual">True für Gleichheit, False für Ungleichheit</param>
        Private Shared Function CompareValues(left As String, right As String, shouldEqual As Boolean) As Boolean
            ' String-Vergleich (case-insensitive)
            Dim stringMatch = String.Equals(left, right, StringComparison.OrdinalIgnoreCase)
            
            ' Numerischer Vergleich (falls möglich)
            Dim numericMatch = False
            Dim leftNum, rightNum As Integer
            If Integer.TryParse(left, leftNum) AndAlso Integer.TryParse(right, rightNum) Then
                numericMatch = (leftNum = rightNum)
            End If

            ' Bei "right" kann auch ein Textformat wie "0 ohne" vorkommen
            ' Extrahiere die führende Zahl
            If Not numericMatch AndAlso right.Contains(" "c) Then
                Dim rightFirstPart = right.Split(" "c)(0).Trim()
                If Integer.TryParse(left, leftNum) AndAlso Integer.TryParse(rightFirstPart, rightNum) Then
                    numericMatch = (leftNum = rightNum)
                End If
            End If

            ' Gesamtergebnis
            Dim isMatch = stringMatch OrElse numericMatch
            
            ' Wenn shouldEqual=True, gebe isMatch zurück
            ' Wenn shouldEqual=False, gebe NOT isMatch zurück
            Return If(shouldEqual, isMatch, Not isMatch)
        End Function

        ''' <summary>
        ''' Ersetzt Konfigurationswerte in einem Ausdruck
        ''' </summary>
        Private Shared Function ReplaceConfigValues(expression As String, configValues As Dictionary(Of String, String)) As String
            Dim result = expression
            
            ' Sortiere Keys nach Länge (längste zuerst), um Teilstring-Matches zu vermeiden
            Dim sortedKeys = configValues.Keys.OrderByDescending(Function(k) k.Length).ToList()
            
            For Each key In sortedKeys
                Dim value = configValues(key)
                ' Erstelle Regex-Pattern für Word-Boundary-Match
                Dim pattern = $"\b{Regex.Escape(key)}\b"
                ' Ersetze durch Wert in Anführungszeichen
                result = Regex.Replace(result, pattern, $"""{value}""", RegexOptions.IgnoreCase)
            Next

            Return result
        End Function

        ''' <summary>
        ''' Bereinigt einen Wert (entfernt Anführungszeichen, Klammern, Whitespace)
        ''' </summary>
        Private Shared Function CleanValue(value As String) As String
            If String.IsNullOrWhiteSpace(value) Then
                Return ""
            End If

            Return value.Trim().Trim(""""c, "'"c, "("c, ")"c).Trim()
        End Function

        ''' <summary>
        ''' Prüft ob ein Operator außerhalb von Anführungszeichen vorkommt
        ''' </summary>
        Private Shared Function ContainsOperatorOutsideQuotes(text As String, operatorStr As String) As Boolean
            Dim inQuotes = False
            Dim i = 0
            
            While i < text.Length
                If text(i) = """"c Then
                    inQuotes = Not inQuotes
                    i += 1
                ElseIf Not inQuotes AndAlso i + operatorStr.Length <= text.Length Then
                    If text.Substring(i, operatorStr.Length) = operatorStr Then
                        Return True
                    End If
                    i += 1
                Else
                    i += 1
                End If
            End While

            Return False
        End Function

        ''' <summary>
        ''' Teilt einen String an einem Operator außerhalb von Anführungszeichen
        ''' </summary>
        Private Shared Function SplitByOperatorOutsideQuotes(text As String, operatorStr As String) As List(Of String)
            Dim parts As New List(Of String)()
            Dim currentPart As New Text.StringBuilder()
            Dim inQuotes = False
            Dim i = 0

            While i < text.Length
                If text(i) = """"c Then
                    inQuotes = Not inQuotes
                    Dim unused1 = currentPart.Append(text(i))
                    i += 1
                ElseIf Not inQuotes AndAlso i + operatorStr.Length <= text.Length Then
                    If text.Substring(i, operatorStr.Length) = operatorStr Then
                        ' Operator gefunden
                        parts.Add(currentPart.ToString())
                        Dim unused = currentPart.Clear()
                        i += operatorStr.Length
                    Else
                        Dim unused2 = currentPart.Append(text(i))
                        i += 1
                    End If
                Else
                    Dim unused3 = currentPart.Append(text(i))
                    i += 1
                End If
            End While

            ' Letzten Teil hinzufügen
            If currentPart.Length > 0 Then
                parts.Add(currentPart.ToString())
            End If

            Return parts
        End Function

        ''' <summary>
        ''' Teilt einen String beim ersten Vorkommen eines Operators (nicht in Quotes)
        ''' </summary>
        Private Shared Function SplitByOperatorOnce(text As String, operatorStr As String) As String()
            Dim inQuotes = False
            Dim i = 0

            While i < text.Length
                If text(i) = """"c Then
                    inQuotes = Not inQuotes
                    i += 1
                ElseIf Not inQuotes AndAlso i + operatorStr.Length <= text.Length Then
                    If text.Substring(i, operatorStr.Length) = operatorStr Then
                        ' Operator gefunden - teile hier
                        Dim left = text.Substring(0, i)
                        Dim right = text.Substring(i + operatorStr.Length)
                        Return {left, right}
                    End If
                    i += 1
                Else
                    i += 1
                End If
            End While

            ' Operator nicht gefunden
            Return {text}
        End Function
    End Class
End Namespace
