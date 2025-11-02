# MQTT HIDDEN Condition Query Implementation

## Übersicht

Diese Implementierung erweitert das Optolink_02-Projekt um eine vollständige Unterstützung für HIDDEN-Conditions in Viessmann-Gerätedatenlisten mit intelligenter MQTT-Abfrage und Caching.

## Neue Komponenten

### 1. EnhancedHiddenConditionEvaluator.vb
Verbesserte Auswertung von HIDDEN-Conditions mit vollständiger Unterstützung für:
- Vergleichsoperatoren: `=`, `!=`, `<>`, `≠`
- Logische Operatoren: `OR`, `AND`
- String- und numerischer Vergleich
- Korrekte Operator-Präzedenz

**Verwendung:**
```vb.net
Dim configValues As New Dictionary(Of String, String)
configValues("00_WWS") = "1"
configValues("54_SR") = "0"

Dim isHidden = EnhancedHiddenConditionEvaluator.EvaluateCondition(
    "HIDDEN:(00_WWS=""1 A1"" OR 00_WWS=""2 A1  + WW"")",
    configValues
)
' isHidden = True → Element sollte versteckt werden
```

### 2. Aktualisierte Query-Services

#### OptimizedMqttQueryService.vb
Optimierter Service für MQTT-Abfragen mit:
- Enum-Wert-Vorladung
- Deduplizierung von Config-Abfragen
- Hierarchische Filterung (Gruppen → Parameter)
- Caching aller MQTT-Antworten

#### HierarchicalMqttQueryService.vb
Hierarchischer Service für MQTT-Abfragen mit:
- Phasenweise Abfrage (Config → Gruppen → Parameter)
- Retry-Logik für fehlgeschlagene Abfragen
- Progress-Callbacks für UI-Updates

Beide Services verwenden jetzt den `EnhancedHiddenConditionEvaluator` für korrekte Condition-Auswertung.

### 3. Dokumentation und Tests

- **Documentation/HIDDEN_Conditions_README.md**: Vollständige Dokumentation der HIDDEN-Condition-Syntax und -Logik
- **Examples/HiddenConditionExamples.vb**: Verwendungsbeispiele und Workflow-Demo
- **Tests/EnhancedHiddenConditionEvaluatorTests.vb**: Unit Tests für alle unterstützten Szenarien

## Workflow der MQTT-Abfrage

### Phase 1: Konfigurationswerte sammeln
```
Gruppen durchsuchen → HIDDEN-Conditions finden → Config-Keys extrahieren → Deduplizieren
```

### Phase 2: Konfigurationswerte abfragen
```
Config-Keys → HexAdressen mappen → MQTT-Abfragen → Cache speichern
```

### Phase 3: Gruppen-Sichtbarkeit evaluieren
```
Für jede Gruppe: HIDDEN-Condition evaluieren → IsVisible setzen
```

### Phase 4: Parameter-Sichtbarkeit evaluieren
```
Für jeden Parameter (nur in sichtbaren Gruppen): HIDDEN-Condition evaluieren → IsVisible setzen
```

### Phase 5: Parameterwerte abfragen
```
Nur sichtbare Parameter abfragen → Cache nutzen → Conversion anwenden
```

## Verwendung im Code

### Beispiel: Device-Hierarchie abfragen

```vb.net
' 1. Device-Hierarchie aus DataTable erstellen
Dim deviceTable As DataTable = Await SqlQueryService.GetDeviceEventsAsync("VScot HO1A")
Dim device As DeviceNode = DeviceHierarchyBuilder.BuildHierarchy(deviceTable)

' 2. MQTT-Service initialisieren
Dim mqttService As New MqttService()
Await mqttService.ConnectAsync("broker.address", 1883)

' 3. Hierarchische Abfrage durchführen
Dim resultDevice = Await OptimizedMqttQueryService.QueryDevHierarchyAsync(
    device,
    mqttService,
    "viessmann/write",
    "viessmann/read",
    3000,
    Sub(msg) Console.WriteLine(msg)
)

' 4. Ergebnisse verarbeiten
For Each category In resultDevice.Categories
    For Each group In category.Groups
        If group.IsVisible Then
            Console.WriteLine($"Gruppe: {group.GroupName}")
            For Each param In group.Parameters.Where(Function(p) p.IsVisible)
                Console.WriteLine($"  {param.ParameterName}: {param.CurrentValue} {param.Unit}")
            Next
        End If
    Next
Next
```

### Beispiel: Einzelne HIDDEN-Condition testen

```vb.net
' Test-Daten vorbereiten
Dim configValues As New Dictionary(Of String, String)
configValues("00_WWS") = "3"  ' Heizkreis-Warmwasserschema

' Condition evaluieren
Dim condition = "HIDDEN:(00_WWS=""1 A1"" OR 00_WWS=""2 A1  + WW"")"
Dim isHidden = EnhancedHiddenConditionEvaluator.EvaluateCondition(condition, configValues)

If isHidden Then
    Console.WriteLine("Element ist versteckt")
Else
    Console.WriteLine("Element ist sichtbar")
End If
' Output: "Element ist sichtbar" (da 00_WWS=3, nicht 1 oder 2)
```

## Caching-Mechanismus

Der `_valueCache` verhindert doppelte MQTT-Abfragen:

```vb.net
' Beim ersten Zugriff wird MQTT-Abfrage durchgeführt
Dim value1 = Await QuerySingleValueAsync("0x7700", ...)
' _valueCache("0x7700") = value1

' Beim zweiten Zugriff wird Cache verwendet
Dim value2 = Await QuerySingleValueAsync("0x7700", ...)
' Keine MQTT-Abfrage! value2 kommt aus Cache
```

**Vorteile:**
- Drastische Reduzierung der MQTT-Abfragen
- Schnellere Gesamtabfrage
- Geringere Netzwerklast

## Unterstützte HIDDEN-Condition-Formate

### Einfache Gleichheit
```
HIDDEN:(54_SR="0 ohne")
```

### OR-Verknüpfung
```
HIDDEN:(00_WWS="1 A1" OR 00_WWS="2 A1  + WW" OR 00_WWS="3 M2")
```

### AND-Verknüpfung
```
HIDDEN:(A="1" AND B="2" AND C="3")
```

### NOT mit Ungleichheit
```
HIDDEN:(00_WWS≠"2 A1  + WW" AND 00_WWS≠"4 M2 +  WW")
```

### Komplexe Bedingungen
```
HIDDEN:(FBA1M1="nicht vorhanden" OR A0_KennFBA1M1="0 ohne" OR A0_KennFBA1M1="nicht vorhanden")
```

## Tests ausführen

```vb.net
' Unit Tests
Optolink_02.Tests.EnhancedHiddenConditionEvaluatorTests.RunAllTests()

' Beispiele
Optolink_02.Examples.HiddenConditionExamples.RunExamples()
Optolink_02.Examples.HiddenConditionExamples.DemonstrateWorkflow()
```

## Performance-Optimierungen

1. **Deduplizierung**: Config-Keys werden vor der Abfrage dedupliziert
2. **Caching**: Jede HexAdresse wird maximal einmal abgefragt
3. **Hierarchische Filterung**: Parameter in versteckten Gruppen werden gar nicht erst abgefragt
4. **Batch-Operationen**: Enum-Werte werden vorab in Batch abgefragt

## Bekannte Limitierungen

- Windows Forms-Projekt kann nicht auf Linux gebaut werden
- MQTT-Tests erfordern echtes Viessmann-Gerät
- Sehr komplexe Bedingungen mit gemischten AND/OR könnten Edge Cases haben

## Weiterführende Dokumentation

- **HIDDEN_Conditions_README.md**: Vollständige Syntax-Referenz und Beispiele
- **HiddenConditionExamples.vb**: Lauffähige Code-Beispiele
- **EnhancedHiddenConditionEvaluatorTests.vb**: Umfassende Unit Tests

## Support und Feedback

Bei Fragen oder Problemen:
1. Prüfen Sie die Dokumentation in `Documentation/HIDDEN_Conditions_README.md`
2. Testen Sie mit den Beispielen in `Examples/HiddenConditionExamples.vb`
3. Führen Sie die Unit Tests aus: `Tests/EnhancedHiddenConditionEvaluatorTests.vb`
4. Aktivieren Sie Debug-Ausgaben in den Services für detaillierte Logs
