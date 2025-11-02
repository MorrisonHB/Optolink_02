Imports System.Text.RegularExpressions
Imports Optolink_02.Services

Namespace Examples
    ''' <summary>
    ''' Beispiele für die Verwendung des EnhancedHiddenConditionEvaluator
    ''' </summary>
    Public Module HiddenConditionExamples

        ''' <summary>
        ''' Testet verschiedene HIDDEN-Condition-Szenarien
        ''' </summary>
        Public Sub RunExamples()
            Console.WriteLine("=== HIDDEN Condition Evaluator - Beispiele ===")
            Console.WriteLine()

            ' Beispiel 1: Einfache Gleichheit
            Example1_SimpleEquality()

            ' Beispiel 2: OR-Verknüpfung
            Example2_OrCondition()

            ' Beispiel 3: AND-Verknüpfung mit Ungleichheit
            Example3_AndWithNotEquals()

            ' Beispiel 4: Komplexe OR-Bedingung
            Example4_ComplexOrCondition()

            ' Beispiel 5: Numerischer vs. Textvergleich
            Example5_NumericTextComparison()

            Console.WriteLine()
            Console.WriteLine("=== Alle Beispiele abgeschlossen ===")
        End Sub

        Private Sub Example1_SimpleEquality()
            Console.WriteLine("--- Beispiel 1: Einfache Gleichheit ---")
            
            Dim condition = "HIDDEN:(54_SR=""0 ohne"")"
            Dim configValues As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            
            ' Szenario 1: Wert ist "0" → sollte HIDDEN sein
            configValues("54_SR") = "0"
            Dim result1 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  54_SR = ""0"" → HIDDEN: {result1} (erwartet: True)")
            
            ' Szenario 2: Wert ist "1" → sollte NICHT HIDDEN sein
            configValues("54_SR") = "1"
            Dim result2 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  54_SR = ""1"" → HIDDEN: {result2} (erwartet: False)")
            
            Console.WriteLine()
        End Sub

        Private Sub Example2_OrCondition()
            Console.WriteLine("--- Beispiel 2: OR-Verknüpfung ---")
            
            Dim condition = "HIDDEN:(00_WWS=""1 A1"" OR 00_WWS=""2 A1  + WW"")"
            Dim configValues As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            
            ' Szenario 1: Wert ist "1" → sollte HIDDEN sein
            configValues("00_WWS") = "1"
            Dim result1 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  00_WWS = ""1"" → HIDDEN: {result1} (erwartet: True)")
            
            ' Szenario 2: Wert ist "2" → sollte HIDDEN sein
            configValues("00_WWS") = "2"
            Dim result2 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  00_WWS = ""2"" → HIDDEN: {result2} (erwartet: True)")
            
            ' Szenario 3: Wert ist "3" → sollte NICHT HIDDEN sein
            configValues("00_WWS") = "3"
            Dim result3 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  00_WWS = ""3"" → HIDDEN: {result3} (erwartet: False)")
            
            Console.WriteLine()
        End Sub

        Private Sub Example3_AndWithNotEquals()
            Console.WriteLine("--- Beispiel 3: AND mit Ungleichheit (≠) ---")
            
            Dim condition = "HIDDEN:(00_WWS≠""2 A1  + WW"" AND 00_WWS≠""4 M2 +  WW"" AND 00_WWS≠""6 A1  + M2 + WW"")"
            Dim configValues As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            
            ' Szenario 1: Wert ist "2" → sollte NICHT HIDDEN sein (eine Bedingung ist falsch)
            configValues("00_WWS") = "2"
            Dim result1 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  00_WWS = ""2"" → HIDDEN: {result1} (erwartet: False)")
            
            ' Szenario 2: Wert ist "1" → sollte HIDDEN sein (alle Bedingungen sind wahr)
            configValues("00_WWS") = "1"
            Dim result2 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  00_WWS = ""1"" → HIDDEN: {result2} (erwartet: True)")
            
            Console.WriteLine()
        End Sub

        Private Sub Example4_ComplexOrCondition()
            Console.WriteLine("--- Beispiel 4: Komplexe OR-Bedingung ---")
            
            Dim condition = "HIDDEN:(FBA1M1=""nicht vorhanden"" OR A0_KennFBA1M1=""0 ohne"" OR A0_KennFBA1M1=""nicht vorhanden"")"
            Dim configValues As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            
            ' Szenario 1: FBA1M1 ist "nicht vorhanden" → sollte HIDDEN sein
            configValues("FBA1M1") = "nicht vorhanden"
            configValues("A0_KennFBA1M1") = "1 mit"
            Dim result1 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  FBA1M1 = ""nicht vorhanden"" → HIDDEN: {result1} (erwartet: True)")
            
            ' Szenario 2: A0_KennFBA1M1 ist "0" → sollte HIDDEN sein
            configValues("FBA1M1") = "vorhanden"
            configValues("A0_KennFBA1M1") = "0"
            Dim result2 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  A0_KennFBA1M1 = ""0"" → HIDDEN: {result2} (erwartet: True)")
            
            ' Szenario 3: Alle Bedingungen falsch → sollte NICHT HIDDEN sein
            configValues("FBA1M1") = "vorhanden"
            configValues("A0_KennFBA1M1") = "1 mit"
            Dim result3 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  Alle vorhanden → HIDDEN: {result3} (erwartet: False)")
            
            Console.WriteLine()
        End Sub

        Private Sub Example5_NumericTextComparison()
            Console.WriteLine("--- Beispiel 5: Numerischer vs. Text-Vergleich ---")
            
            Dim condition = "HIDDEN:(30_KennIntUmwPumpe=""0 stufig"")"
            Dim configValues As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            
            ' Szenario 1: Numerischer Wert "0" → sollte HIDDEN sein (passt zu "0 stufig")
            configValues("30_KennIntUmwPumpe") = "0"
            Dim result1 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  30_KennIntUmwPumpe = ""0"" → HIDDEN: {result1} (erwartet: True)")
            
            ' Szenario 2: Textvergleich "0 stufig" → sollte HIDDEN sein
            configValues("30_KennIntUmwPumpe") = "0 stufig"
            Dim result2 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  30_KennIntUmwPumpe = ""0 stufig"" → HIDDEN: {result2} (erwartet: True)")
            
            ' Szenario 3: Wert "1" → sollte NICHT HIDDEN sein
            configValues("30_KennIntUmwPumpe") = "1"
            Dim result3 = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)
            Console.WriteLine($"  30_KennIntUmwPumpe = ""1"" → HIDDEN: {result3} (erwartet: False)")
            
            Console.WriteLine()
        End Sub

        ''' <summary>
        ''' Demonstriert den vollständigen Workflow von HIDDEN-Condition-Abfragen
        ''' </summary>
        Public Sub DemonstrateWorkflow()
            Console.WriteLine("=== HIDDEN Condition Workflow - Demonstration ===")
            Console.WriteLine()
            Console.WriteLine("Schritt 1: Konfigurationswerte sammeln")
            Console.WriteLine("  - Durchsuche alle Gruppen nach HIDDEN-Conditions")
            Console.WriteLine("  - Extrahiere benötigte Config-Keys: 00_WWS, 54_SR, 76_KommMod, ...")
            Console.WriteLine("  - Dedupliziere Keys (verhindert mehrfache Abfragen)")
            Console.WriteLine()

            Console.WriteLine("Schritt 2: HexAdressen ermitteln")
            Console.WriteLine("  - Mappe Config-Keys zu HexAdressen via SQL-Abfrage")
            Console.WriteLine("  - Beispiel: 00_WWS → K00_KonfiAnlagenschemaGWG_W~0x7700")
            Console.WriteLine()

            Console.WriteLine("Schritt 3: MQTT-Abfragen durchführen")
            Console.WriteLine("  - Command: read;0x7700;1")
            Console.WriteLine("  - Response: 1;0x7700;3")
            Console.WriteLine("  - Speichere im Cache: _valueCache(""0x7700"") = ""3""")
            Console.WriteLine()

            Console.WriteLine("Schritt 4: Gruppen-Sichtbarkeit evaluieren")
            Console.WriteLine("  - Gruppe: Heizkreis M2")
            Console.WriteLine("  - Condition: HIDDEN:(00_WWS=""1 A1"" OR 00_WWS=""2 A1  + WW"")")
            Console.WriteLine("  - Ersetze: 00_WWS → ""3""")
            Console.WriteLine("  - Evaluiere: (""3""=""1"" OR ""3""=""2"") = False")
            Console.WriteLine("  - IsVisible = NOT False = True")
            Console.WriteLine()

            Console.WriteLine("Schritt 5: Parameter abfragen")
            Console.WriteLine("  - Frage nur Parameter in sichtbaren Gruppen ab")
            Console.WriteLine("  - Nutze Cache für bereits abgefragte Adressen")
            Console.WriteLine()

            Console.WriteLine("=== Workflow abgeschlossen ===")
        End Sub

    End Module
End Namespace
