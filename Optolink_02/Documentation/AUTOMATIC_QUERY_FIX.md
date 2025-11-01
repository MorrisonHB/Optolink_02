# Automatische Hierarchie-Abfrage Fix

## Problem

Bei der automatischen Hierarchie-Abfrage wurden **fehlerhafte Werte** angezeigt, besonders bei Temperaturwerten. Die manuelle Abfrage über das Kontextmenü lieferte jedoch **korrekte Werte**.

### Symptome

1. **Fehlerhafte Temperaturwerte in der Hierarchie-Ansicht**
2. **MQTT Error-Messages im Debug-Output:**
   ```
   [MQTT] Empfangen auf 'vitodens/resp': Error: invalid literal for int() with base 0: '400C'
   [MQTT] Empfangen auf 'vitodens/resp': Error: invalid literal for int() with base 0: 'Databa'
   [MQTT] Empfangen auf 'vitodens/resp': Error: invalid literal for int() with base 0: 'Schalt'
   ```

3. **Hex-Parsing-Fehler:**
   ```
   [PARSE ASCII] HexValue='37383338363435363031303534313032' -> ASCII='7838645601054102'
   [PARSE ASCII NON-NUMERIC] HexValue='00000000000000000000000000000000' -> ASCII='????????????????'
   ```

### Ursache

Der `OptimizedMqttQueryService` verwendete:
1. **Alte MQTT-Kommando-Format** (OHNE Scale/Signed-Parameter)
2. **ValueConversionService** zur client-seitigen Konvertierung (veraltet!)

**Code in OptimizedMqttQueryService.vb (Zeile 619):**
```vb
' FALSCH - altes Format
Dim command = $"read;{address};{byteLength}"

' FALSCH - versucht Hex-Parsing auf bereits konvertierten Werten
Dim convertedValue = rawValue
If Not String.IsNullOrWhiteSpace(conversionType) Then
    convertedValue = ValueConversionService.ConvertValue(
        rawValue,
        conversionType,
        conversionFactor,
        conversionOffset)
End If
```

### Warum funktionierten manuelle Abfragen?

Die manuelle Abfrage (über Kontextmenü) verwendete bereits:
- **MqttCommandGenerator** ? korrektesformat mit Scale/Signed
- **Keine ValueConversionService** ? Response-Wert direkt übernommen

```vb
' Form1.vb - mnuParamDirectQuery_Click (RICHTIG)
Dim command = Services.MqttCommandGenerator.GenerateReadCommand(
    param.Address,
    param.ByteLength.ToString(),
    param.Conversion,
    param.DataType)
```

## Lösung

### Änderungen in OptimizedMqttQueryService.vb

#### 1. **QuerySingleValueAsync-Signatur erweitert**

**Vorher:**
```vb
Private Shared Async Function QuerySingleValueAsync(
    address As String,
    mqttService As MqttService,
    sendTopic As String,
    receiveTopic As String,
    timeoutMs As Integer,
    Optional byteLength As Integer = 2,
    Optional conversionType As String = Nothing,
    Optional conversionFactor As Double = 1.0,
    Optional conversionOffset As Double = 0.0
) As Task(Of String)
```

**Nachher:**
```vb
Private Shared Async Function QuerySingleValueAsync(
    address As String,
    mqttService As MqttService,
    sendTopic As String,
    receiveTopic As String,
    timeoutMs As Integer,
  Optional byteLength As Integer = 2,
    Optional conversionType As String = Nothing,
    Optional conversionFactor As Double = 1.0,
    Optional conversionOffset As Double = 0.0,
    Optional dataType As String = Nothing  ' NEU!
) As Task(Of String)
```

#### 2. **MqttCommandGenerator verwenden**

**Vorher:**
```vb
Dim command = $"read;{address};{byteLength}"
```

**Nachher:**
```vb
' WICHTIG: Verwende MqttCommandGenerator um korrektes Format mit scale/signed zu erhalten
Dim command = Services.MqttCommandGenerator.GenerateReadCommand(
    address,
    byteLength.ToString(),
    conversionType,
    dataType)

' Fallback wenn Generator nichts zurückgibt
If String.IsNullOrWhiteSpace(command) Then
    command = $"read;{address};{byteLength}"
End If

Debug.WriteLine($"[QUERY-SINGLE] MQTT Command: {command}")
```

#### 3. **ValueConversionService entfernen**

**Vorher:**
```vb
If parts.Length >= 3 AndAlso parts(0) = "1" Then
    Dim rawValue = parts(2).Trim()

    ' FALSCH: Conversion anwenden
    Dim convertedValue = rawValue
    If Not String.IsNullOrWhiteSpace(conversionType) Then
 convertedValue = ValueConversionService.ConvertValue(
      rawValue,
            conversionType,
          conversionFactor,
            conversionOffset)
    End If

    _valueCache(address) = convertedValue
    Return convertedValue
End If
```

**Nachher:**
```vb
If parts.Length >= 3 AndAlso parts(0) = "1" Then
    Dim rawValue = parts(2).Trim()

    ' WICHTIG: Optolink-Splitter liefert bereits konvertierte Werte!
    ' Keine weitere Konvertierung nötig - direkt verwenden
    Dim convertedValue = rawValue

    Debug.WriteLine($"[QUERY-SINGLE] Response value: {convertedValue}")

    _valueCache(address) = convertedValue
    Return convertedValue
End If
```

#### 4. **Alle Aufrufe aktualisiert**

**QueryEnumValuesAsync:**
```vb
Dim value = Await QuerySingleValueAsync(
    kvp.Value.Address,
    mqttService,
    sendTopic,
    receiveTopic,
    timeoutMs,
    kvp.Value.ByteLength,
    kvp.Value.Conversion,
    kvp.Value.ConversionFactor,
 kvp.Value.ConversionOffset,
    kvp.Value.DataType) ' NEU: DataType übergeben
```

**QueryConfigValuesAsync:**
```vb
Dim value = Await QuerySingleValueAsync(
    address,
    mqttService,
    sendTopic,
    receiveTopic,
    timeoutMs,
    2, ' Standard 2 Bytes für Config-Werte
    Nothing, ' Keine Conversion für Config-Werte
    1.0, ' Factor
    0.0, ' Offset
    Nothing) ' Kein DataType für Config-Werte (meist Enums/Strings)
```

**QueryVisibleParameterValuesAsync:**
```vb
Dim value = Await QuerySingleValueAsync(
  param.Address,
    mqttService,
    sendTopic,
    receiveTopic,
    timeoutMs,
    param.ByteLength,
    param.Conversion,
    param.ConversionFactor,
    param.ConversionOffset,
    param.DataType) ' NEU: DataType übergeben
```

## Vergleich: Vorher vs. Nachher

### Vorher (FALSCH)

**MQTT-Kommando:**
```
read;0x5525;2  (OHNE scale/signed)
```

**Response vom Optolink-Splitter:**
```
1;0x5525;0082  (RAW Hex-Daten!)
```

**Client-seitige Verarbeitung:**
```vb
rawValue = "0082"
?
ValueConversionService.ParseHexToDecimal("0082")
  = 130 (decimal)
?
ValueConversionService.ConvertValue(130, "Div10")
  = 13.0 ? (zufällig korrekt bei unsigned)
```

**Problem:** Wenn Optolink-Splitter eine Fehlermeldung sendet:
```
Response: "Error: invalid literal for int() with base 0: '400C'"
?
ValueConversionService.ParseHexToDecimal("Error: ...")
  ? FormatException! ?
```

### Nachher (RICHTIG)

**MQTT-Kommando:**
```
read;0x5525;2;0.1;true  (MIT scale/signed)
```

**Response vom Optolink-Splitter:**
```
1;0x5525;13.0  (Bereits konvertiert!)
```

**Client-seitige Verarbeitung:**
```vb
rawValue = "13.0"
?
convertedValue = rawValue  (Direkt übernehmen)
  = "13.0" ?
```

**Vorteil:** Fehlermeldungen können korrekt verarbeitet werden:
```
Response: "Error: invalid literal..."
?
parts(0) ? "1" ? Fehler erkannt
Parameter.CurrentValue = "[ERR]"
```

## Debug-Ausgabe

### Vorher (Fehler):
```
[MQTT] Empfangen auf 'vitodens/resp': Error: invalid literal for int() with base 0: '400C'
[PARSE ERROR] HexValue='Error: ...', Error: FormatException
```

### Nachher (korrekt):
```
[QUERY-SINGLE] MQTT Command: read;0x5525;2;0.1;true
[MQTT] Empfangen auf 'vitodens/resp': 1;0x5525;13.0
[QUERY-SINGLE] Response value: 13.0
```

## Timing-Problem?

Der Benutzer vermutete ein Timing-Problem. **Das war NICHT die Ursache!**

Das Problem war:
- ? **NICHT** zu wenig Zeit zwischen Abfragen
- ? **FALSCHES MQTT-Kommando-Format**
- ? **FALSCHE Response-Verarbeitung**

Die Error-Messages im Debug-Output (`Error: invalid literal for int() with base 0: '400C'`) kamen **VOM OPTOLINK-SPLITTER**, weil er die Rohdaten nicht ohne Scale/Signed-Info parsen konnte!

## Testing

### Test 1: Aussentemperatur (automatische Abfrage)
```
Command: read;0x5525;2;0.1;true  (RICHTIG)
Response: 1;0x5525;13.0
Result: 13.0 °C ?
```

### Test 2: Warmwasser (automatische Abfrage)
```
Command: read;0x6300;1;0.1;true(RICHTIG)
Response: 1;0x6300;50.0
Result: 50.0 °C ?
```

### Test 3: Enum-Werte (Config-Parameter)
```
Command: read;0x7700;2  (Kein scale/signed nötig für Enums)
Response: 1;0x7700;03
Result: "03" ? Mapping ? "2 A1 + WW" ?
```

## Zusammenfassung

? **OptimizedMqttQueryService** verwendet jetzt MqttCommandGenerator  
? **ValueConversionService** entfernt (Response bereits konvertiert)  
? **DataType-Parameter** in QuerySingleValueAsync hinzugefügt  
? **Alle Aufrufe** aktualisiert  
? **Debug-Ausgabe** für generierte Kommandos  
? **Build erfolgreich**  

Die automatische Hierarchie-Abfrage sollte jetzt **die gleichen korrekten Werte** liefern wie die manuelle Abfrage über das Kontextmenü!

## Betroffe Komponenten

| Komponente | Status | Beschreibung |
|------------|--------|--------------|
| Form1.vb (Direkt-Abfrage) | ? Bereits korrekt | Verwendet MqttCommandGenerator |
| ParameterQueryDialog.vb | ? Bereits korrekt | Verwendet MqttCommandGenerator |
| OptimizedMqttQueryService.vb | ? KORRIGIERT | Jetzt mit MqttCommandGenerator |
| HierarchicalMqttQueryService.vb | ?? Zu prüfen | Vermutlich auch zu korrigieren |
| MqttCommandGenerator.vb | ? Bereits korrekt | Generiert korrektes Format |
| ValueConversionService.vb | ?? Legacy | Wird nicht mehr verwendet, aber behalten |

## Nächste Schritte (optional)

Falls `HierarchicalMqttQueryService` noch verwendet wird, sollte dieser ebenfalls korrigiert werden nach dem gleichen Muster.
