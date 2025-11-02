# MQTT Query Workflow - Visueller Ablauf

## Gesamtübersicht

```
┌─────────────────────────────────────────────────────────────────────┐
│                    MQTT HIDDEN Condition Query                      │
│                                                                       │
│  DP-Datei → Hierarchie → Config-Abfrage → Evaluation → Wert-Abfrage │
└─────────────────────────────────────────────────────────────────────┘
```

## Detaillierter Ablauf

```
╔═══════════════════════════════════════════════════════════════════════╗
║ SCHRITT 1: DP-Datei Parsing                                          ║
╚═══════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────┐
│ DP_VScotHO1_20.txt                  │
├─────────────────────────────────────┤
│ # Überblick (13790)                 │
│   - Kessel (13744)                  │
│   - Heizkreis A1 (13745)            │
│     HIDDEN:(00_WWS="3 M2")          │
│   - Heizkreis M2 (13746)            │
│     HIDDEN:(00_WWS="1" OR "2")      │
│   - Warmwasser (13747)              │
│     HIDDEN:(00_WWS≠"2" AND ≠"4")    │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ DeviceHierarchyBuilder              │
│ BuildHierarchy(DataTable)           │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ DeviceNode                          │
│  ├─ Categories                      │
│  │   ├─ Groups                      │
│  │   │   ├─ Parameters              │
│  │   │   │   ├─ Address             │
│  │   │   │   ├─ HiddenCondition     │
│  │   │   │   └─ CurrentValue        │
└─────────────────────────────────────┘


╔═══════════════════════════════════════════════════════════════════════╗
║ SCHRITT 2: Phase 1 - Enum-Werte sammeln                              ║
╚═══════════════════════════════════════════════════════════════════════╝

Durchsuche alle Parameter nach "(XX) Name" Format
         │
         ▼
┌─────────────────────────────────────┐
│ Gefundene Enum-Parameter:           │
│  (00) Heizkreis-Warmwasserschema    │
│  (54) Solarregelung                 │
│  (76) Kommunikationsmodul           │
│  ...                                │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ MQTT-Abfragen (parallel):           │
│  read;0x7700;1  → "3"               │
│  read;0x7754;1  → "1"               │
│  read;0x7776;1  → "0"               │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ _valueCache:                        │
│  0x7700 → "3"                       │
│  0x7754 → "1"                       │
│  0x7776 → "0"                       │
└─────────────────────────────────────┘


╔═══════════════════════════════════════════════════════════════════════╗
║ SCHRITT 3: Phase 2/3 - Config-Keys extrahieren & abfragen            ║
╚═══════════════════════════════════════════════════════════════════════╝

Durchsuche alle HIDDEN-Conditions
         │
         ▼
┌─────────────────────────────────────┐
│ Extrahierte Config-Keys:            │
│  00_WWS (aus 3 Conditions)          │
│  54_SR  (aus 1 Condition)           │
│  76_KommMod (aus 2 Conditions)      │
└─────────────────────────────────────┘
         │
         ▼ Deduplizierung
┌─────────────────────────────────────┐
│ Eindeutige Keys:                    │
│  00_WWS, 54_SR, 76_KommMod          │
│  (3 statt 6 Abfragen!)              │
└─────────────────────────────────────┘
         │
         ▼ SQL: Mappe Keys → HexAdresse
┌─────────────────────────────────────┐
│ Address-Mapping:                    │
│  00_WWS    → 0x7700                 │
│  54_SR     → 0x7754                 │
│  76_KommMod → 0x7776                │
└─────────────────────────────────────┘
         │
         ▼ MQTT-Abfrage (mit Cache-Check!)
┌─────────────────────────────────────┐
│ Cache-Hits:                         │
│  0x7700 ✓ (bereits in Phase 1)     │
│  0x7754 ✓ (bereits in Phase 1)     │
│  0x7776 ✓ (bereits in Phase 1)     │
│                                     │
│ Keine neuen MQTT-Abfragen nötig!   │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ ConfigValues Dictionary:            │
│  00_WWS    → "3"                    │
│  54_SR     → "1"                    │
│  76_KommMod → "0"                   │
└─────────────────────────────────────┘


╔═══════════════════════════════════════════════════════════════════════╗
║ SCHRITT 4: Phase 4 - Gruppen-Sichtbarkeit evaluieren                 ║
╚═══════════════════════════════════════════════════════════════════════╝

Für jede Gruppe mit HIDDEN-Condition:

┌─────────────────────────────────────┐
│ Heizkreis A1                        │
│ HIDDEN:(00_WWS="3 M2")              │
└─────────────────────────────────────┘
         │
         ▼ Ersetze 00_WWS → "3"
┌─────────────────────────────────────┐
│ Evaluiere: ("3"="3")                │
│ Ergebnis: TRUE                      │
│                                     │
│ IsVisible = NOT TRUE = FALSE        │
│ → Gruppe VERSTECKT                  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Heizkreis M2                        │
│ HIDDEN:(00_WWS="1" OR 00_WWS="2")   │
└─────────────────────────────────────┘
         │
         ▼ Ersetze 00_WWS → "3"
┌─────────────────────────────────────┐
│ Evaluiere: ("3"="1" OR "3"="2")     │
│ Ergebnis: FALSE                     │
│                                     │
│ IsVisible = NOT FALSE = TRUE        │
│ → Gruppe SICHTBAR                   │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Warmwasser                          │
│ HIDDEN:(00_WWS≠"2" AND 00_WWS≠"4")  │
└─────────────────────────────────────┘
         │
         ▼ Ersetze 00_WWS → "3"
┌─────────────────────────────────────┐
│ Evaluiere:                          │
│  ("3"≠"2") = TRUE                   │
│  ("3"≠"4") = TRUE                   │
│  TRUE AND TRUE = TRUE               │
│                                     │
│ IsVisible = NOT TRUE = FALSE        │
│ → Gruppe VERSTECKT                  │
└─────────────────────────────────────┘


╔═══════════════════════════════════════════════════════════════════════╗
║ SCHRITT 5: Phase 5 - Parameter-Werte abfragen                        ║
╚═══════════════════════════════════════════════════════════════════════╝

Nur für SICHTBARE Gruppen:

┌─────────────────────────────────────────────────┐
│ Heizkreis M2 (SICHTBAR)                         │
│  ├─ Aktuelle Betriebsart M2                     │
│  │   Address: 0x3500 → read;0x3500;1 → "1"      │
│  ├─ Vorlauftemperatur M2                        │
│  │   Address: 0x3900 → read;0x3900;2 → "385"    │
│  │   Conversion: /10 → "38.5"                   │
│  └─ Heizkreispumpe M2                           │
│      Address: 0x7665 → read;0x7665;1 → "1"      │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│ Heizkreis A1 (VERSTECKT)                        │
│  ├─ Alle Parameter übersprungen!                │
│  └─ Keine MQTT-Abfragen                         │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│ Warmwasser (VERSTECKT)                          │
│  ├─ Alle Parameter übersprungen!                │
│  └─ Keine MQTT-Abfragen                         │
└─────────────────────────────────────────────────┘


╔═══════════════════════════════════════════════════════════════════════╗
║ ERGEBNIS: Gefilterte Hierarchie                                      ║
╚═══════════════════════════════════════════════════════════════════════╝

DeviceNode
  ├─ Category: Überblick
  │   ├─ Group: Kessel (SICHTBAR)
  │   │   ├─ Aussentemperatur: 12.5 °C
  │   │   ├─ Kesseltemperatur: 45.0 °C
  │   │   └─ Brenner: 0 Aus
  │   │
  │   ├─ Group: Heizkreis A1 (VERSTECKT)
  │   │   └─ [Alle Parameter markiert als HIDDEN]
  │   │
  │   ├─ Group: Heizkreis M2 (SICHTBAR)
  │   │   ├─ Aktuelle Betriebsart M2: 1 Automatik
  │   │   ├─ Vorlauftemperatur M2: 38.5 °C
  │   │   └─ Heizkreispumpe M2: 1 Ein
  │   │
  │   └─ Group: Warmwasser (VERSTECKT)
  │       └─ [Alle Parameter markiert als HIDDEN]
  └─ ...


╔═══════════════════════════════════════════════════════════════════════╗
║ CACHE-STATISTIK                                                      ║
╚═══════════════════════════════════════════════════════════════════════╝

Ohne Cache:
  ┌────────────────────────────────────┐
  │ Phase 1: 12 Enum-Abfragen          │
  │ Phase 3: 8 Config-Abfragen         │
  │   (davon 6 Duplikate!)             │
  │ Phase 5: 85 Parameter-Abfragen     │
  │                                    │
  │ GESAMT: 105 MQTT-Abfragen          │
  └────────────────────────────────────┘

Mit Cache:
  ┌────────────────────────────────────┐
  │ Phase 1: 12 Enum-Abfragen          │
  │   → Alle im Cache gespeichert      │
  │ Phase 3: 0 Config-Abfragen!        │
  │   → Alle aus Cache (Phase 1)       │
  │ Phase 5: 85 Parameter-Abfragen     │
  │   → Neue Adressen                  │
  │                                    │
  │ GESAMT: 97 MQTT-Abfragen           │
  │ ERSPARNIS: 8 Abfragen (7.6%)       │
  └────────────────────────────────────┘

Bei mehreren ähnlichen Devices:
  ┌────────────────────────────────────┐
  │ Device 1: 97 MQTT-Abfragen         │
  │ Device 2: 20 MQTT-Abfragen         │
  │   (77 bereits im Cache!)           │
  │                                    │
  │ ERSPARNIS: 79% weniger Abfragen!   │
  └────────────────────────────────────┘
```

## EnhancedHiddenConditionEvaluator - Interne Logik

```
╔═══════════════════════════════════════════════════════════════════════╗
║ EvaluateCondition(condition, configValues)                           ║
╚═══════════════════════════════════════════════════════════════════════╝

Input: "HIDDEN:(00_WWS="1" OR 00_WWS="2")"
       configValues = {00_WWS: "3"}

Step 1: Bereinigung
  ┌────────────────────────────────────┐
  │ Remove "HIDDEN:(" und ")"          │
  │ Normalize ≠ → !=                   │
  │                                    │
  │ Result: 00_WWS="1" OR 00_WWS="2"   │
  └────────────────────────────────────┘

Step 2: EvaluateExpression
  ┌────────────────────────────────────┐
  │ ReplaceConfigValues                │
  │  00_WWS → "3"                      │
  │                                    │
  │ Result: "3"="1" OR "3"="2"         │
  └────────────────────────────────────┘

Step 3: Split by OR (außerhalb von Quotes)
  ┌────────────────────────────────────┐
  │ Parts:                             │
  │  1. "3"="1"                        │
  │  2. "3"="2"                        │
  └────────────────────────────────────┘

Step 4: EvaluateAndExpression (für jede Part)
  ┌────────────────────────────────────┐
  │ Part 1: "3"="1"                    │
  │  → EvaluateComparison              │
  │  → CompareValues("3", "1", true)   │
  │  → Result: FALSE                   │
  │                                    │
  │ Part 2: "3"="2"                    │
  │  → EvaluateComparison              │
  │  → CompareValues("3", "2", true)   │
  │  → Result: FALSE                   │
  └────────────────────────────────────┘

Step 5: OR-Aggregation
  ┌────────────────────────────────────┐
  │ FALSE OR FALSE                     │
  │ = FALSE                            │
  │                                    │
  │ Return: FALSE                      │
  └────────────────────────────────────┘

Caller (OptimizedMqttQueryService):
  ┌────────────────────────────────────┐
  │ isHidden = FALSE                   │
  │ IsVisible = NOT FALSE = TRUE       │
  │                                    │
  │ → Gruppe ist SICHTBAR              │
  └────────────────────────────────────┘
```

## Performance-Vergleich

```
╔═══════════════════════════════════════════════════════════════════════╗
║ Szenario: Großes Device mit 200 Parametern                           ║
╚═══════════════════════════════════════════════════════════════════════╝

Ohne Optimierungen:
  ┌────────────────────────────────────────────┐
  │ Keine HIDDEN-Filterung                     │
  │ → 200 Parameter werden abgefragt           │
  │ → Zeit: ~60 Sekunden (300ms pro Parameter) │
  └────────────────────────────────────────────┘

Mit HIDDEN-Filterung (ohne Cache):
  ┌────────────────────────────────────────────┐
  │ 50 Gruppen versteckt (100 Parameter)       │
  │ 50 Config-Abfragen (Duplikate!)            │
  │ → 150 MQTT-Abfragen gesamt                 │
  │ → Zeit: ~45 Sekunden                       │
  │ → Ersparnis: 25%                           │
  └────────────────────────────────────────────┘

Mit HIDDEN-Filterung + Cache:
  ┌────────────────────────────────────────────┐
  │ 50 Gruppen versteckt (100 Parameter)       │
  │ 15 Config-Abfragen (dedupliziert!)         │
  │ → 115 MQTT-Abfragen gesamt                 │
  │ → Zeit: ~35 Sekunden                       │
  │ → Ersparnis: 42%                           │
  └────────────────────────────────────────────┘

Mit allen Optimierungen:
  ┌────────────────────────────────────────────┐
  │ 50 Gruppen versteckt (100 Parameter)       │
  │ 15 Config-Abfragen (aus Enum-Cache)        │
  │ 10 Batch-Enum-Abfragen                     │
  │ → 110 MQTT-Abfragen gesamt                 │
  │ → Zeit: ~33 Sekunden                       │
  │ → Ersparnis: 45%                           │
  └────────────────────────────────────────────┘
```
