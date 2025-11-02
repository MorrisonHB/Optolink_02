# HIDDEN Conditions - Dokumentation

## Übersicht

Die Viessmann-Gerätedatenlisten enthalten HIDDEN-Conditions, die bestimmen, ob bestimmte Gruppen oder Parameter sichtbar sein sollen oder nicht. Diese Conditions sind hierarchisch aufgebaut und ermöglichen eine intelligente, geräteabhängige Anzeige der verfügbaren Optionen.

## Struktur der DP-Listen

Die DP-Listen (z.B. `DP_VScotHO1_20.txt`) haben folgende hierarchische Struktur:

```
Gerätename (z.B. VScot HO1A)
  # Kategorie (ID)
    - Gruppe (ID) [optional: HIDDEN:(...)]
      - Parameter (ID) [Adresse~HexAdresse (DataType)] [optional: HIDDEN:(...)]
```

### Beispiel:

```
# Überblick (13790)
- Heizkreis M2 (13746) HIDDEN:(00_WWS="1 A1" OR 00_WWS="2 A1  + WW")
    - Aktuelle Betriebsart M2 (709) [HK_AktuelleBetriebsartM2~0x3500 (Byte)]
    - Vorlauftemperatur M2 (6053) [VorlauftemperaturM2~0x3900 (Int)]
```

## HIDDEN-Condition Syntax

### Grundstruktur
```
HIDDEN:(Bedingung)
```

### Unterstützte Operatoren

#### Vergleichsoperatoren:
- `=` - Gleichheit
- `!=` oder `<>` oder `≠` - Ungleichheit

#### Logische Operatoren:
- `OR` - Logisches ODER (mindestens eine Bedingung muss erfüllt sein)
- `AND` - Logisches UND (alle Bedingungen müssen erfüllt sein)

### Beispiele

#### 1. Einfache Gleichheit
```
HIDDEN:(54_SR="0 ohne")
```
Die Gruppe ist versteckt, wenn `54_SR` den Wert "0" (numerisch) oder "0 ohne" (textuell) hat.

#### 2. OR-Verknüpfung
```
HIDDEN:(00_WWS="1 A1" OR 00_WWS="2 A1  + WW")
```
Die Gruppe ist versteckt, wenn `00_WWS` entweder "1" oder "2" ist.

#### 3. AND-Verknüpfung mit Ungleichheit
```
HIDDEN:(00_WWS≠"2 A1  + WW" AND 00_WWS≠"4 M2 +  WW" AND 00_WWS≠"6 A1  + M2 + WW")
```
Die Gruppe ist versteckt, wenn `00_WWS` NICHT "2", "4" oder "6" ist.

#### 4. Komplexe Bedingungen
```
HIDDEN:(FBA1M1="nicht vorhanden" OR A0_KennFBA1M1="0 ohne" OR A0_KennFBA1M1="nicht vorhanden")
```

## Konfigurationswerte

Die in den HIDDEN-Conditions verwendeten Werte (z.B. `00_WWS`, `54_SR`) beziehen sich auf Konfigurationsparameter des Geräts. Diese sind in der Regel in den Kategorien:
- "Anlagenausstattung"
- "Allgemein"
- "Gerätedaten"
- "Codierung 1"
- "Codierung 2"
- "Inbetriebnahme"

### Format der Konfigurationsparameter:
```
(XX) Parametername (ID) [Kurzname~HexAdresse (DataType)]
```

Beispiel:
```
(00) Heizkreis-Warmwasserschema (791) [K00_KonfiAnlagenschemaGWG_W~0x7700 (Byte)]
```

Der Kurzname `00_WWS` wird in HIDDEN-Conditions verwendet und bezieht sich auf:
- `(00)` = Identifikator
- `K00_KonfiAnlagenschemaGWG_W` = Voller Parameter-Name
- `0x7700` = HexAdresse für MQTT-Abfrage

## Ablauf der MQTT-Abfrage

Die Implementierung in `HierarchicalMqttQueryService.vb` und `OptimizedMqttQueryService.vb` folgt diesem Ablauf:

### Phase 1: Konfigurationswerte sammeln
1. Durchsuche alle Gruppen nach HIDDEN-Conditions
2. Extrahiere alle benötigten Konfigurationsschlüssel (z.B. `00_WWS`, `54_SR`)
3. Dedupliziere die Schlüssel (verhindert mehrfache Abfragen)

### Phase 2: Konfigurationswerte abfragen
1. Mappe Konfigurationsschlüssel zu HexAdressen
2. Führe MQTT-Abfragen durch (Format: `read;HexAdresse;ByteLength`)
3. Speichere Ergebnisse im Cache (`_valueCache`)

### Phase 3: Gruppen-Sichtbarkeit evaluieren
1. Für jede Gruppe mit HIDDEN-Condition:
2. Evaluiere die Condition mit den abgefragten Konfigurationswerten
3. Setze `group.IsVisible = NOT EvaluateCondition()`
4. Nur sichtbare Gruppen werden weiterverarbeitet

### Phase 4: Parameter-Sichtbarkeit evaluieren
1. Für jeden Parameter in sichtbaren Gruppen:
2. Wenn Parameter eigene HIDDEN-Condition hat, evaluiere diese
3. Setze `param.IsVisible` entsprechend

### Phase 5: Parameterwerte abfragen
1. Frage nur Werte von sichtbaren Parametern ab
2. Nutze den Cache, um doppelte Abfragen zu vermeiden
3. Wende Conversion (Faktor, Offset) an

## Caching-Mechanismus

Der `_valueCache` (Dictionary) speichert bereits abgefragte Werte:

```vb.net
Private Shared ReadOnly _valueCache As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
```

**Vorteile:**
- Vermeidet mehrfache MQTT-Abfragen derselben Adresse
- Reduziert Netzwerkverkehr
- Beschleunigt die Abfrage erheblich

**Cache-Struktur:**
- Key: HexAdresse (z.B. "0x7700")
- Value: Abgefragter Wert (z.B. "1" oder "1 A1")

## Implementierung

### EnhancedHiddenConditionEvaluator

Die Klasse `EnhancedHiddenConditionEvaluator.vb` bietet eine robuste Implementierung:

**Features:**
- Korrekte Auswertung von OR/AND-Verknüpfungen
- Unterstützung aller Vergleichsoperatoren (`=`, `!=`, `≠`, `<>`)
- String- und numerischer Vergleich
- Korrekte Behandlung von Anführungszeichen
- Rekursive Auswertung komplexer Ausdrücke

**Verwendung:**
```vb.net
Dim isHidden = EnhancedHiddenConditionEvaluator.EvaluateCondition(
    condition, 
    configValues
)
```

### Integration in Services

Beide Query-Services verwenden jetzt den Enhanced Evaluator:

**OptimizedMqttQueryService.vb:**
```vb.net
group.IsVisible = Not EvaluateHiddenCondition(group.HiddenCondition, configValues)
```

**HierarchicalMqttQueryService.vb:**
```vb.net
group.IsVisible = Not EvaluateHiddenCondition(group.HiddenCondition, configValues)
```

## Beispiel-Ablauf

### Szenario: Heizkreis M2 Sichtbarkeit

1. **HIDDEN-Condition:** `HIDDEN:(00_WWS="1 A1" OR 00_WWS="2 A1  + WW")`

2. **Phase 1 - Schlüssel extrahieren:**
   - Extrahierte Schlüssel: `["00_WWS"]`

3. **Phase 2 - Adresse ermitteln:**
   - Suche in Datenbank: `00_WWS` → `K00_KonfiAnlagenschemaGWG_W~0x7700`
   - HexAdresse: `0x7700`

4. **Phase 3 - MQTT-Abfrage:**
   - Command: `read;0x7700;1`
   - Response: `1;0x7700;3` (ReturnCode;Address;Value)
   - Wert: `3`

5. **Phase 4 - Condition evaluieren:**
   - Ersetze `00_WWS` durch `"3"`
   - Evaluiere: `("3"="1" OR "3"="2")`
   - Ergebnis: `False` (beide Vergleiche sind falsch)
   
6. **Phase 5 - Sichtbarkeit setzen:**
   - `IsVisible = NOT False = True`
   - **Heizkreis M2 ist sichtbar!**

### Alternative: Wert ist "1"

4. **Phase 4 - Condition evaluieren:**
   - Ersetze `00_WWS` durch `"1"`
   - Evaluiere: `("1"="1" OR "1"="2")`
   - Ergebnis: `True` (erster Vergleich ist wahr)
   
5. **Phase 5 - Sichtbarkeit setzen:**
   - `IsVisible = NOT True = False`
   - **Heizkreis M2 ist versteckt!**

## Best Practices

### 1. Cacheing nutzen
Der Cache verhindert redundante MQTT-Abfragen. Bei mehreren Gruppen/Parametern mit denselben Conditions werden Konfigurationswerte nur einmal abgefragt.

### 2. Reihenfolge beachten
Erst Gruppen evaluieren, dann Parameter. Dadurch werden Parameter in versteckten Gruppen gar nicht erst abgefragt.

### 3. Fehlerbehandlung
Bei fehlerhaften Conditions oder fehlenden Werten wird standardmäßig `False` zurückgegeben (Element wird angezeigt).

### 4. Debug-Ausgaben
Die Services loggen ausführlich:
```vb.net
Debug.WriteLine($"[CONDITION GROUP] {group.GroupName}: {group.HiddenCondition}")
Debug.WriteLine($"[OPTIMIZED] Phase 4: {hiddenGroupCount} Gruppen ausgeblendet")
```

## Zusammenfassung

Die HIDDEN-Condition-Logik ermöglicht:
- ✅ Geräteabhängige Anzeige von Parametern
- ✅ Reduzierung irrelevanter Optionen für Benutzer
- ✅ Effiziente MQTT-Abfragen durch Caching
- ✅ Korrekte Auswertung komplexer logischer Bedingungen
- ✅ Hierarchische Filterung (Gruppen → Parameter)

Die Implementierung ist robust, effizient und vollständig getestet für alle in den DP-Listen vorkommenden Condition-Typen.
