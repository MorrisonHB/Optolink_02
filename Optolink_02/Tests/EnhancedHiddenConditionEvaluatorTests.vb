Imports Optolink_02.Services

Namespace Tests
    ''' <summary>
    ''' Unit Tests für EnhancedHiddenConditionEvaluator
    ''' Diese Tests können manuell ausgeführt werden oder in ein Test-Framework integriert werden
    ''' </summary>
    Public Module EnhancedHiddenConditionEvaluatorTests

        Public Sub RunAllTests()
            Console.WriteLine("=== EnhancedHiddenConditionEvaluator - Unit Tests ===")
            Console.WriteLine()

            Dim totalTests = 0
            Dim passedTests = 0

            ' Test Suite 1: Einfache Gleichheit
            RunTestSuite1(totalTests, passedTests)

            ' Test Suite 2: OR-Bedingungen
            RunTestSuite2(totalTests, passedTests)

            ' Test Suite 3: AND-Bedingungen
            RunTestSuite3(totalTests, passedTests)

            ' Test Suite 4: NOT-Bedingungen (!=, ≠)
            RunTestSuite4(totalTests, passedTests)

            ' Test Suite 5: Komplexe Bedingungen
            RunTestSuite5(totalTests, passedTests)

            ' Test Suite 6: Edge Cases
            RunTestSuite6(totalTests, passedTests)

            ' Zusammenfassung
            Console.WriteLine()
            Console.WriteLine($"=== Test-Zusammenfassung ===")
            Console.WriteLine($"Gesamt: {totalTests} Tests")
            Console.WriteLine($"Bestanden: {passedTests} Tests")
            Console.WriteLine($"Fehlgeschlagen: {totalTests - passedTests} Tests")
            Console.WriteLine($"Erfolgsrate: {If(totalTests > 0, (passedTests * 100.0 / totalTests).ToString("F1"), "0")}%")
        End Sub

        Private Sub RunTestSuite1(ByRef totalTests As Integer, ByRef passedTests As Integer)
            Console.WriteLine("--- Test Suite 1: Einfache Gleichheit ---")

            ' Test 1.1: Numerische Gleichheit
            Test("Test 1.1: Numerische Gleichheit",
                 "HIDDEN:(54_SR=""0"")",
                 New Dictionary(Of String, String) From {{"54_SR", "0"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 1.2: Text-Gleichheit
            Test("Test 1.2: Text-Gleichheit",
                 "HIDDEN:(54_SR=""0 ohne"")",
                 New Dictionary(Of String, String) From {{"54_SR", "0 ohne"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 1.3: Numerisch vs. Text (sollte übereinstimmen)
            Test("Test 1.3: Numerisch vs. Text",
                 "HIDDEN:(54_SR=""0 ohne"")",
                 New Dictionary(Of String, String) From {{"54_SR", "0"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 1.4: Keine Übereinstimmung
            Test("Test 1.4: Keine Übereinstimmung",
                 "HIDDEN:(54_SR=""0"")",
                 New Dictionary(Of String, String) From {{"54_SR", "1"}},
                 False,
                 totalTests,
                 passedTests)

            Console.WriteLine()
        End Sub

        Private Sub RunTestSuite2(ByRef totalTests As Integer, ByRef passedTests As Integer)
            Console.WriteLine("--- Test Suite 2: OR-Bedingungen ---")

            ' Test 2.1: Erste Bedingung wahr
            Test("Test 2.1: OR - Erste wahr",
                 "HIDDEN:(00_WWS=""1"" OR 00_WWS=""2"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "1"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 2.2: Zweite Bedingung wahr
            Test("Test 2.2: OR - Zweite wahr",
                 "HIDDEN:(00_WWS=""1"" OR 00_WWS=""2"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "2"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 2.3: Beide Bedingungen falsch
            Test("Test 2.3: OR - Beide falsch",
                 "HIDDEN:(00_WWS=""1"" OR 00_WWS=""2"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "3"}},
                 False,
                 totalTests,
                 passedTests)

            ' Test 2.4: Mehrere OR-Bedingungen
            Test("Test 2.4: OR - Mehrere Bedingungen",
                 "HIDDEN:(00_WWS=""1"" OR 00_WWS=""2"" OR 00_WWS=""3"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "2"}},
                 True,
                 totalTests,
                 passedTests)

            Console.WriteLine()
        End Sub

        Private Sub RunTestSuite3(ByRef totalTests As Integer, ByRef passedTests As Integer)
            Console.WriteLine("--- Test Suite 3: AND-Bedingungen ---")

            ' Test 3.1: Beide Bedingungen wahr
            Test("Test 3.1: AND - Beide wahr",
                 "HIDDEN:(A=""1"" AND B=""2"")",
                 New Dictionary(Of String, String) From {{"A", "1"}, {"B", "2"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 3.2: Erste Bedingung falsch
            Test("Test 3.2: AND - Erste falsch",
                 "HIDDEN:(A=""1"" AND B=""2"")",
                 New Dictionary(Of String, String) From {{"A", "0"}, {"B", "2"}},
                 False,
                 totalTests,
                 passedTests)

            ' Test 3.3: Zweite Bedingung falsch
            Test("Test 3.3: AND - Zweite falsch",
                 "HIDDEN:(A=""1"" AND B=""2"")",
                 New Dictionary(Of String, String) From {{"A", "1"}, {"B", "0"}},
                 False,
                 totalTests,
                 passedTests)

            Console.WriteLine()
        End Sub

        Private Sub RunTestSuite4(ByRef totalTests As Integer, ByRef passedTests As Integer)
            Console.WriteLine("--- Test Suite 4: NOT-Bedingungen (!=, ≠, <>) ---")

            ' Test 4.1: != Operator - nicht gleich
            Test("Test 4.1: != - nicht gleich",
                 "HIDDEN:(00_WWS!=""1"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "2"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 4.2: != Operator - gleich
            Test("Test 4.2: != - gleich",
                 "HIDDEN:(00_WWS!=""1"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "1"}},
                 False,
                 totalTests,
                 passedTests)

            ' Test 4.3: ≠ Operator (Unicode)
            Test("Test 4.3: ≠ (Unicode)",
                 "HIDDEN:(00_WWS≠""1"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "2"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 4.4: <> Operator
            Test("Test 4.4: <> Operator",
                 "HIDDEN:(00_WWS<>""1"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "2"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 4.5: AND mit mehreren !=
            Test("Test 4.5: AND mit !=",
                 "HIDDEN:(00_WWS!=""1"" AND 00_WWS!=""2"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "3"}},
                 True,
                 totalTests,
                 passedTests)

            Console.WriteLine()
        End Sub

        Private Sub RunTestSuite5(ByRef totalTests As Integer, ByRef passedTests As Integer)
            Console.WriteLine("--- Test Suite 5: Komplexe Bedingungen ---")

            ' Test 5.1: Reale Bedingung aus DP-Datei
            Test("Test 5.1: Heizkreis M2",
                 "HIDDEN:(00_WWS=""1 A1"" OR 00_WWS=""2 A1  + WW"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "1"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 5.2: Warmwasser Bedingung
            Test("Test 5.2: Warmwasser",
                 "HIDDEN:(00_WWS≠""2 A1  + WW"" AND 00_WWS≠""4 M2 +  WW"" AND 00_WWS≠""6 A1  + M2 + WW"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "2"}},
                 False,
                 totalTests,
                 passedTests)

            ' Test 5.3: Mehrere Variablen mit OR
            Test("Test 5.3: Mehrere Variablen OR",
                 "HIDDEN:(FBA1M1=""nicht vorhanden"" OR A0_KennFBA1M1=""0 ohne"")",
                 New Dictionary(Of String, String) From {{"FBA1M1", "vorhanden"}, {"A0_KennFBA1M1", "0"}},
                 True,
                 totalTests,
                 passedTests)

            Console.WriteLine()
        End Sub

        Private Sub RunTestSuite6(ByRef totalTests As Integer, ByRef passedTests As Integer)
            Console.WriteLine("--- Test Suite 6: Edge Cases ---")

            ' Test 6.1: Leere Bedingung
            Test("Test 6.1: Leere Bedingung",
                 "",
                 New Dictionary(Of String, String) From {{"00_WWS", "1"}},
                 False,
                 totalTests,
                 passedTests)

            ' Test 6.2: Fehlender Config-Wert
            Test("Test 6.2: Fehlender Config-Wert",
                 "HIDDEN:(UNKNOWN=""1"")",
                 New Dictionary(Of String, String) From {{"00_WWS", "1"}},
                 False,
                 totalTests,
                 passedTests)

            ' Test 6.3: Case-Insensitive Vergleich
            Test("Test 6.3: Case-Insensitive",
                 "HIDDEN:(54_SR=""OHNE"")",
                 New Dictionary(Of String, String) From {{"54_SR", "ohne"}},
                 True,
                 totalTests,
                 passedTests)

            ' Test 6.4: Whitespace handling
            Test("Test 6.4: Whitespace",
                 "HIDDEN:(00_WWS=""  1  "")",
                 New Dictionary(Of String, String) From {{"00_WWS", "1"}},
                 True,
                 totalTests,
                 passedTests)

            Console.WriteLine()
        End Sub

        Private Sub Test(testName As String,
                        condition As String,
                        configValues As Dictionary(Of String, String),
                        expectedResult As Boolean,
                        ByRef totalTests As Integer,
                        ByRef passedTests As Integer)

            totalTests += 1

            Try
                Dim actualResult = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)

                If actualResult = expectedResult Then
                    Console.WriteLine($"  ✓ {testName}")
                    passedTests += 1
                Else
                    Console.WriteLine($"  ✗ {testName}")
                    Console.WriteLine($"    Erwartet: {expectedResult}, Erhalten: {actualResult}")
                End If
            Catch ex As Exception
                Console.WriteLine($"  ✗ {testName}")
                Console.WriteLine($"    Fehler: {ex.Message}")
            End Try
        End Sub

    End Module
End Namespace
