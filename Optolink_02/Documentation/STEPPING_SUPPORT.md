# Stepping-Unterstützung im ParameterQueryDialog

## Übersicht

Der `ParameterQueryDialog` wurde erweitert, um die Schrittweite (Stepping) von Parametern anzuzeigen und zu bearbeiten. Diese Information stammt aus der Vitosoft-Datenbank und gibt an, in welchen Schritten ein Wert geändert werden kann.

## Änderungen

### 1. **Domain-Modell** (`Domain\DeviceHierarchyModels.vb`)
- Neue Eigenschaft `Stepping As String` zur `ParameterNode`-Klasse hinzugefügt

### 2. **DeviceHierarchyBuilder** (`Services\DeviceHierarchyBuilder.vb`)
- `Stepping`-Spalte wird aus der DataTable ausgelesen
- `Stepping`-Wert wird beim Erstellen der `ParameterNode`-Objekte gesetzt

### 3. **ParameterQueryDialog** (`Dialogs\ParameterQueryDialog.vb`)
- Neues Label `lblStepping` für die Beschriftung
- Neue TextBox `txtStepping` zur Anzeige und **Bearbeitung** der Schrittweite (editierbar!)
- ComboBox `cmbConversion` ist editierbar und wird mit vordefinierten Conversion-Typen gefüllt
- Alle Werte (Adresse, ByteLength, Conversion, Stepping) können individuell angepasst werden
- Layout angepasst:
  - Dialog-Höhe: 350 ? 400 Pixel
  - Stepping-Controls bei Y=160
  - Result-Label bei Y=195
  - Result-TextBox bei Y=192
  - ProgressBar bei Y=285
  - Buttons bei Y=305

## Verwendung

### Editierbare Felder im Dialog "Abfragen...":

- **Adresse**: Kann manuell geändert werden (z.B. `0x6300`)
- **Byte-Länge**: NumericUpDown (1-32 Bytes)
- **Conversion**: Dropdown mit vordefinierten Typen ODER freie Eingabe
  - Vordefinierte Typen: `NoConversion`, `Div2`, `Div10`, `Div100`, `Div1000`, `Mult2`, `Mult5`, `Mult10`, `Mult100`, `MultOffset`, `HexByte2DecimalByte`, `IPAddress`, `DateBCD`, `DateTimeBCD`, `Time53`, `Sec2Minute`, `Sec2Hour`, `Sec2Day`, `Sec2Week`, `RotateBytes`, `Phone2BCD`
- **Stepping**: Freie Texteingabe (z.B. `1`, `0.1`, `0.5`)

### Beispiel-Workflow:

```
Parameter:   Solltemperatur Warmwasser  (read-only, Original-Wert)
Adresse:     [0x6300] ? editierbar
Byte-Länge:  [2] ? editierbar (1-32)
Conversion:  [Div10] ? editierbar (Dropdown oder freie Eingabe)
Stepping:  [1] ? editierbar
Ergebnis:    Aktueller Wert: 50.0°C
             [Multiline-Textbox für Abfrageergebnis]

[Abfragen] [Abbrechen]
```

### Änderungen gegenüber Original:

**Vorher** (nur Anzeige):
- Conversion war leer oder vorbelegt, aber nicht editierbar
- Stepping war read-only mit grauem Hintergrund

**Nachher** (editierbar):
- Conversion: Dropdown mit 21 vordefinierten Typen + freie Eingabe möglich
- Stepping: Vollständig editierbar (weißer Hintergrund)
- Alle bearbeiteten Werte werden im ParameterNode gespeichert

## Datenquelle

Die Stepping-Information kommt aus der SQL-Abfrage in `SqlQueries.vb`:

```sql
SELECT ...
  ev.Stepping,
  ...
FROM ecnEventType et
LEFT JOIN ecnEventValueType ev ON etvl.EventValueId = ev.Id
...
```

Die `ecnEventValueType`-Tabelle in der Vitosoft-Datenbank enthält das `Stepping`-Feld, das die minimale Schrittweite für Wertänderungen definiert.

## Beispielwerte

| Parameter | Stepping | Bedeutung |
|-----------|----------|-----------|
| Solltemperatur | 1 | Änderbar in 1°-Schritten |
| Warmwasser-Solltemperatur | 1 | Änderbar in 1°-Schritten |
| Kennlinie-Steigung | 0.1 | Änderbar in 0.1-Schritten |
| Brenner Ein/Aus | (leer) | Binärer Wert, kein Stepping |

## Vorteile der Editierbarkeit

1. **Experimentieren**: Testen verschiedener Conversion-Methoden ohne Neustart
2. **Debugging**: Schnelles Anpassen von Parametern für Testzwecke
3. **Flexibilität**: Manuelle Eingabe bei unbekannten oder fehlenden Datenbankwerten
4. **Dokumentation**: Stepping kann manuell gesetzt werden für spätere Referenz

## Gespeicherte Werte

Nach erfolgreicher Abfrage werden folgende Werte im `ParameterNode` aktualisiert:
- `CurrentValue` (konvertierter Wert)
- `Address` (verwendete Adresse)
- `ByteLength` (verwendete Byte-Länge)
- `Conversion` (verwendete Conversion-Methode)
- `Stepping` (eingegebener Stepping-Wert)

Diese Werte bleiben für die aktuelle Session erhalten und werden in der ListView angezeigt.

## Zukünftige Erweiterungen

Mögliche zukünftige Funktionen:
- Validierung bei manuellen Wertänderungen (Schreibbefehle)
- NumericUpDown mit automatischer Schrittweite basierend auf Stepping
- Hinweis bei ungültigen Werten (nicht Vielfaches des Stepping-Werts)
- Persistierung der bearbeiteten Werte in einer Konfigurationsdatei
