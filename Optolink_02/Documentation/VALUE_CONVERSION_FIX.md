# ValueConversionService Fix - Response bereits konvertiert

## Problem

Nach der Implementierung des korrekten MQTT-Kommando-Formats trat ein neuer Fehler auf:

```
[DIRECT-QUERY] MQTT Command: read;0x5525;2;0.1;true
[MQTT] Empfangen auf 'vitodens/resp': 1;0x5525;13.0
Ausnahme ausgelöst: "System.FormatException" in System.Private.CoreLib.dll
[PARSE ERROR] HexValue='13.0', Error: FormatException: Could not find any recognizable digits.
```

### Ursache

Der Code versuchte, den Response-Wert **NOCHMAL** zu konvertieren, obwohl der Optolink-Splitter bereits die Konvertierung durchgeführt hatte:

1. **MQTT-Kommando gesendet:**
   ```
   read;0x5525;2;0.1;true
   ```
   - `0.1` = Scale-Faktor
   - `true` = Signed

2. **Optolink-Splitter antwortet:**
   ```
   1;0x5525;13.0
   ```
 - Der Wert `13.0` ist **bereits konvertiert**!
   - Optolink-Splitter hat die Rohdaten mit Scale 0.1 multipliziert

3. **Unser Code (VORHER - FALSCH):**
   ```vb
   Dim rawValue = parts(2).Trim()  ' "13.0"
   
   ' FALSCH: Versucht "13.0" als Hex zu parsen!
   Dim convertedValue = Services.ValueConversionService.ConvertValue(
       rawValue,     ' "13.0"
       param.Conversion, ' "Div10"
   param.ConversionFactor,
    param.ConversionOffset)
   ```

4. **ValueConversionService.ParseHexToDecimal:**
   ```vb
   Private Shared Function ParseHexToDecimal(hexValue As String) As Double
   ' Versucht "13.0" als Hex-String zu parsen
       Dim cleanHex = hexValue.Replace("0x", "").Trim()  ' "13.0"
       ' Convert.ToInt32("13.0", 16) ? FormatException!
   ```

## Lösung

Die Response des Optolink-Splitters enthält **bereits den konvertierten Wert** - keine weitere Konvertierung nötig!

### Änderungen in Form1.vb

**Vorher (FALSCH):**
```vb
If parts.Length >= 3 AndAlso parts(0) = "1" Then
    Dim rawValue = parts(2).Trim()

 ' FALSCH: Conversion mit Factor und Offset anwenden
    Dim convertedValue = rawValue
    If Not String.IsNullOrWhiteSpace(param.Conversion) Then
        convertedValue = Services.ValueConversionService.ConvertValue(
        rawValue,
         param.Conversion,
         param.ConversionFactor,
 param.ConversionOffset)
    End If

    param.CurrentValue = convertedValue
    selectedItem.SubItems(1).Text = convertedValue
```

**Nachher (RICHTIG):**
```vb
If parts.Length >= 3 AndAlso parts(0) = "1" Then
    Dim rawValue = parts(2).Trim()

    ' RICHTIG: Optolink-Splitter liefert bereits konvertierte Werte!
    ' Keine weitere Konvertierung nötig - direkt verwenden
    Dim convertedValue = rawValue
    
  Debug.WriteLine($"[DIRECT-QUERY] Response value: {convertedValue}")

    param.CurrentValue = convertedValue
    selectedItem.SubItems(1).Text = convertedValue
```

### Änderungen in ParameterQueryDialog.vb

**Vorher (FALSCH):**
```vb
If parts.Length >= 3 AndAlso parts(0) = "1" Then
    Dim rawValue = parts(2).Trim()

    ' FALSCH: Conversion anwenden
    Dim conversion = cmbConversion.Text
    Dim convertedValue = rawValue
    If Not String.IsNullOrWhiteSpace(conversion) AndAlso conversion <> "NoConversion" Then
        convertedValue = Services.ValueConversionService.ConvertValue(rawValue, conversion)
    End If

    txtResult.Text = $"? Abfrage erfolgreich{Environment.NewLine}" &
        $"Command: {command}{Environment.NewLine}" &
        $"Response: {response}{Environment.NewLine}" &
        $"Raw: {rawValue}{Environment.NewLine}" &
    $"Converted: {convertedValue}"
```

**Nachher (RICHTIG):**
```vb
If parts.Length >= 3 AndAlso parts(0) = "1" Then
    Dim rawValue = parts(2).Trim()

    ' RICHTIG: Optolink-Splitter liefert bereits konvertierte Werte!
 ' Keine weitere Konvertierung nötig - direkt verwenden
    Dim convertedValue = rawValue
    
    Debug.WriteLine($"[PARAM-QUERY] Response value: {convertedValue}")

    txtResult.Text = $"? Abfrage erfolgreich{Environment.NewLine}" &
     $"Command: {command}{Environment.NewLine}" &
        $"Response: {response}{Environment.NewLine}" &
    $"Value: {convertedValue}"
```

## Workflow - Wie es RICHTIG funktioniert

### Schritt 1: Kommando generieren (mit Scale/Signed)

```vb
Dim command = Services.MqttCommandGenerator.GenerateReadCommand(
    "0x5525",    ' Address
    "2",   ' ByteLength
    "Div10",     ' Conversion ? Scale: 0.1
    "Int")       ' DataType ? Signed: true

' Result: "read;0x5525;2;0.1;true"
```

### Schritt 2: MQTT-Kommando senden

```
MQTT Publish:
Topic: vitodens/cmnd
Payload: read;0x5525;2;0.1;true
```

### Schritt 3: Optolink-Splitter verarbeitet

Der Optolink-Splitter:
1. Liest Rohdaten von Adresse 0x5525 (2 Bytes)
2. Interpretiert als Signed Integer (true)
3. Multipliziert mit Scale 0.1
4. Sendet konvertierten Wert zurück

```
Rohdaten: 0x0082 (hex) = 130 (decimal signed)
Mit Scale: 130 * 0.1 = 13.0
```

### Schritt 4: MQTT-Response empfangen

```
MQTT Response:
Topic: vitodens/resp
Payload: 1;0x5525;13.0
         ^  ^      ^
         |  |      ?? Konvertierter Wert (FERTIG!)
       |  ?? Adresse (zur Zuordnung)
         ?? Status (1 = OK)
```

### Schritt 5: Wert direkt verwenden

```vb
Dim parts = response.Split(";"c)
' parts(0) = "1" (Status OK)
' parts(1) = "0x5525" (Adresse)
' parts(2) = "13.0" (FERTIGER Wert)

Dim value = parts(2).Trim()  ' "13.0"
param.CurrentValue = value   ' Direkt übernehmen - KEINE Konvertierung!
```

## Warum ValueConversionService NICHT verwendet wird

### Zweck des ValueConversionService

Der `ValueConversionService` wurde ursprünglich erstellt für **RAW MQTT-Responses ohne Scale/Signed**:

**Altes Format (ohne Optolink-Splitter Konvertierung):**
```
Command: read;0x5525;2  (OHNE scale/signed)
Response: 1;0x5525;0082  (RAW Hex-Daten)
       ^^^^
    Hex-Wert muss konvertiert werden!
```

In diesem Fall:
```vb
' Hex ? Decimal
Dim value = ParseHexToDecimal("0082")  ' = 130

' Dann Conversion anwenden
value = value / 10.0  ' Div10 ? 13.0
```

### Neues Format (mit Optolink-Splitter Konvertierung)

**Aktuell (MIT scale/signed):**
```
Command: read;0x5525;2;0.1;true  (MIT scale/signed)
Response: 1;0x5525;13.0  (Bereits konvertiert!)
      ^^^^
  Fertiger Decimal-Wert!
```

Keine Konvertierung nötig:
```vb
Dim value = parts(2)  ' = "13.0" ? Fertig!
```

## Zusammenfassung der Änderungen

| Komponente | Vorher | Nachher |
|------------|--------|---------|
| **Form1.vb** (Direkt-Abfrage) | `ValueConversionService.ConvertValue()` | Direkte Verwendung des Response-Werts |
| **ParameterQueryDialog.vb** | `ValueConversionService.ConvertValue()` | Direkte Verwendung des Response-Werts |
| **ValueConversionService.vb** | Aktiv verwendet | NICHT mehr verwendet (aber behalten für Legacy-Fälle) |

## Debug-Ausgabe

### Vorher (mit Fehler):
```
[DIRECT-QUERY] MQTT Command: read;0x5525;2;0.1;true
[MQTT] Empfangen auf 'vitodens/resp': 1;0x5525;13.0
[PARSE ERROR] HexValue='13.0', Error: FormatException: Could not find any recognizable digits.
```

### Nachher (korrekt):
```
[DIRECT-QUERY] MQTT Command: read;0x5525;2;0.1;true
[MQTT] Empfangen auf 'vitodens/resp': 1;0x5525;13.0
[DIRECT-QUERY] Response value: 13.0
? Direkt-Abfrage: Aussentemperatur = 13.0
```

## Testing

### Test 1: Aussentemperatur
```
Input:  read;0x5525;2;0.1;true
Output: 1;0x5525;13.0
Result: 13.0 °C ?
```

### Test 2: Warmwasser-Solltemperatur
```
Input:  read;0x6300;1;0.1;true
Output: 1;0x6300;50.0
Result: 50.0 °C ?
```

### Test 3: Raw-Daten (ohne Konvertierung)
```
Input:  read;0xF8;8
Output: 1;0xF8;20CB1FC900000114
Result: 20CB1FC900000114 ? (Hex-String bleibt Hex-String)
```

## Wichtige Erkenntnisse

1. **Optolink-Splitter ist intelligent:**
   - Verarbeitet Scale-Faktor
   - Verarbeitet Signed/Unsigned
   - Liefert **fertige** Werte

2. **Client-seitige Konvertierung nicht nötig:**
   - Response enthält bereits konvertierte Werte
   - Direkte Übernahme ausreichend

3. **ValueConversionService obsolet:**
   - Wird NICHT mehr verwendet
   - Kann für Legacy-Szenarien behalten werden
   - Sollte dokumentiert bleiben für Verständnis

4. **Zwei-Stufen-Prozess:**
   - **Stufe 1:** MqttCommandGenerator erstellt korrektes Kommando (MIT scale/signed)
   - **Stufe 2:** Response direkt verwenden (OHNE weitere Konvertierung)

## Status

? **ValueConversionService-Aufrufe entfernt**  
? **Response-Werte werden direkt verwendet**  
? **FormatException behoben**  
? **Korrekte Werte werden angezeigt**  
? **Build erfolgreich**  

Die Implementierung ist jetzt vollständig korrekt!
