# MQTT-Kommando-Format Fix

## Problem

Bei der MQTT-Abfrage von Parametern wurden falsche Werte zurückgegeben, weil das MQTT-Kommando-Format unvollständig war.

### Beispiel: Aussentemperatur

**Problem:**
- **Erwartet**: 12.8 °C
- **Erhalten**: -3276.8 °C
- **Rohdaten**: 8000 (hex 0x8000)

**Ursache:**
Das MQTT-Kommando war:
```
read;0x5525;2
```

Aber es sollte sein:
```
read;0x5525;2;0.1;true
```

Die fehlenden Parameter:
- `0.1` = Skalierungsfaktor (Conversion)
- `true` = Signed-Flag (Wert ist vorzeichenbehaftet)

## Lösung

### 1. **MqttCommandGenerator verwenden**

Der `MqttCommandGenerator` wurde bereits korrekt implementiert und erstellt das richtige Format:

```vb
Public Shared Function GenerateReadCommand(
    address As String,
    byteLength As String,
    conversion As String,
    dataType As String) As String
```

**Beispiel-Ausgabe:**
```
read;0x5525;2;0.1;true
```

### 2. **Änderungen in Form1.vb (Direkt-Abfrage)**

**Vorher:**
```vb
Dim command = $"read;{param.Address};{param.ByteLength}"
```

**Nachher:**
```vb
Dim command = Services.MqttCommandGenerator.GenerateReadCommand(
    param.Address,
    param.ByteLength.ToString(),
    param.Conversion,
    param.DataType)
```

### 3. **Änderungen in ParameterQueryDialog.vb**

**Vorher:**
```vb
Dim command = $"read;{address};{byteLen}"
```

**Nachher:**
```vb
Dim command = Services.MqttCommandGenerator.GenerateReadCommand(
    address,
    byteLen.ToString(),
    conversion,
    dataType)
```

### 4. **Neues Feld im Dialog: DataType**

Der ParameterQueryDialog wurde um ein `DataType`-Feld erweitert:

```
Parameter:   Aussentemperatur  (read-only)
Adresse:     [0x5525] ? editierbar
Byte-Länge:  [2] ? editierbar
Conversion:  [Div10] ? editierbar ? ergibt 0.1
DataType:    [Int] ? NEU, editierbar ? ergibt signed=true
Stepping:    [] ? editierbar
Ergebnis:    [Aktueller Wert]
```

**Verfügbare DataTypes:**
- `Int`, `Float`, `Double` ? signed=true
- `Byte`, `UInt`, `Int16`, `Int32`, `Long` ? signed=false oder true je nach Typ

## Wie es funktioniert

### Conversion ? Scale

Der `MqttCommandGenerator` mappt die Conversion-Typen auf Skalierungsfaktoren:

| Conversion | Scale |
|------------|-------|
| Div2 | 0.5 |
| Div10 | 0.1 |
| Div100 | 0.01 |
| Div1000 | 0.001 |
| Mult2 | 2 |
| Mult10 | 10 |
| Mult100 | 100 |

### DataType ? Signed

Der DataType bestimmt das Signed-Flag:

| DataType | Signed |
|----------|--------|
| Float, Double | true |
| Int, Integer, Int16, Int32, Long | true |
| Byte, UInt, UInt16, UInt32, ULong | false |

## MQTT-Kommando-Beispiele

### Beispiel 1: Aussentemperatur (signed, skaliert)

**Parameter:**
- Adresse: 0x5525
- ByteLength: 2
- Conversion: Div10 ? Scale: 0.1
- DataType: Int ? Signed: true

**MQTT-Kommando:**
```
read;0x5525;2;0.1;true
```

**Rohdaten:** 8000 (hex) = 128 (decimal signed 16-bit) = 0x0080
**Nach Skalierung:** 128 * 0.1 = 12.8 °C

### Beispiel 2: Warmwasser-Solltemperatur (signed, skaliert)

**Parameter:**
- Adresse: 0x6300
- ByteLength: 1
- Conversion: Div10 ? Scale: 0.1
- DataType: Int ? Signed: true

**MQTT-Kommando:**
```
read;0x6300;1;0.1;true
```

### Beispiel 3: Gerät-Identifikation (raw, keine Skalierung)

**Parameter:**
- Adresse: 0xF8
- ByteLength: 8
- Conversion: NoConversion ? (keine Scale)
- DataType: Byte ? (kein Signed-Flag)

**MQTT-Kommando:**
```
read;0xf8;8
```

**Response:**
```
1;248;20CB1FC900000114
```

## Debug-Ausgabe

Im Debug-Output werden jetzt die generierten MQTT-Kommandos angezeigt:

```
[DIRECT-QUERY] MQTT Command: read;0x5525;2;0.1;true
[PARAM-QUERY] MQTT Command: read;0x6300;1;0.1;true
```

## Wichtige Hinweise

### 1. Signed vs. Unsigned

**Problem mit 0x8000:**
- Unsigned 16-bit: 0x8000 = 32768
- Signed 16-bit: 0x8000 = -32768 (Two's Complement)

**Mit Skalierung 0.1:**
- Unsigned: 32768 * 0.1 = 3276.8 ? FALSCH
- Signed: -32768 * 0.1 = -3276.8 ? FALSCH

**Richtig (0x0080 = 128 signed):**
- 128 * 0.1 = 12.8 ? RICHTIG

### 2. Fallback-Mechanismus

Falls der `MqttCommandGenerator` ein leeres Kommando zurückgibt, wird ein Fallback verwendet:

```vb
If String.IsNullOrWhiteSpace(command) Then
    command = $"read;{address};{byteLen}"
End If
```

Dies stellt sicher, dass die Abfrage auch ohne Scale/Signed funktioniert (falls der Wert in der Datenbank fehlt).

### 3. ValueConversionService

**WICHTIG:** Die `ValueConversionService.ConvertValue()` Methode sollte **NICHT MEHR** verwendet werden für die Konvertierung der MQTT-Response, da der Optolink-Splitter bereits die Konvertierung durchführt!

Die Conversion-Parameter werden NUR für die Generierung des MQTT-Kommandos verwendet, NICHT für die Client-seitige Nachbearbeitung der Response!

**Implementiert:** ?
- Form1.vb - Direkt-Abfrage: ValueConversionService-Aufruf entfernt
- ParameterQueryDialog.vb - Dialog-Abfrage: ValueConversionService-Aufruf entfernt  
- Response-Wert wird direkt verwendet ohne weitere Konvertierung

## Testing

### Test 1: Aussentemperatur
```
Command: read;0x5525;2;0.1;true
Expected Response: 1;0x5525;13.0
Actual Value: 13.0 °C ? (direkt verwendet, keine Konvertierung)
```

**Wichtig:** Der Optolink-Splitter antwortet mit:
```
1;0x5525;13.0
```
Der Wert `13.0` ist bereits konvertiert (mit scale 0.1 und signed=true)!

**Fehler VORHER:**
```
Response: 1;0x5525;13.0
ParseHexToDecimal('13.0') ? FormatException: Could not find any recognizable digits
```

**Korrekt NACHHER:**
```
Response: 1;0x5525;13.0  
Value: 13.0 (direkt verwendet) ?
```

### Test 2: Vergleich mit MQTT Explorer
