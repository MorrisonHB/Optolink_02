# Python-Format Export

## Übersicht

Diese Funktionalität exportiert Viessmann-Gerätedaten im gleichen Format wie das Python-Skript `PrintEventsForDatapoint.py` aus dem `InsideViessmannVitosoft`-Projekt.

## Verwendung

1. Wählen Sie ein Gerät aus der Dropdown-Liste
2. Klicken Sie auf "Abfrage starten"
3. Nach erfolgreichem Laden der Daten: **Datei ? Export ? Python-Format**
4. Wählen Sie einen Speicherort
5. Die Datei wird im Python-Format gespeichert

## Format-Struktur

```
VScot HO1A
==========

# Kategorie1
- Gruppe1 (GroupId)
    - Parameter1 (EventId) [ParameterName~0xAddress (DataType)] HIDDEN:(condition)
    - Parameter2 (EventId) [ParameterName~0xAddress (DataType)]
- Gruppe2 (GroupId) HIDDEN:(group_condition)
    - Parameter3 (EventId) [ParameterName~0xAddress (DataType)]

# Kategorie2
- Gruppe3 (GroupId)
  - Parameter4 (EventId) [ParameterName~0xAddress (DataType)]
```

## Unterschiede zur XML-Ausgabe

### 1. **Hidden-Conditions**
- **XML**: Verwendet vollständige Namen in doppelten Klammern
  ```xml
  <Condition>HIDDEN:(("(30) Kennung Interne Umwälzpumpe"="0 stufig"))</Condition>
  ```
- **Python-Format**: Verwendet Shortcuts (bereits in SQL-Query definiert)
  ```
  HIDDEN:(30_KennIntUmwPumpe="0 stufig")
  ```

### 2. **Hierarchie**
- **XML**: Flache Struktur mit `<Gruppe>` und `<Kategorie>` Spalten
- **Python-Format**: Hierarchische Struktur mit Einrückungen
  ```
  # Kategorie (Parent Group)
  - Gruppe (Child Group)
      - Parameter
```

### 3. **Adressinformation**
- **XML**: Separate Spalten für Address, Parameter, DataType
- **Python-Format**: Kombiniert als `[Name~Address (Type)]`
  ```
  [InternePumpeDrehzahl~0x7660 (Int)]
  ```

## Shortcuts

Die folgenden Shortcuts werden für bessere Lesbarkeit verwendet (aus `SqlQueries.vb`):

| Vollständiger Name | Shortcut |
|-------------------|----------|
| `(00) Heizkreis-Warmwasserschema` | `00_WWS` |
| `(54) Solarregelung` | `54_SR` |
| `(7F) Unterscheidung Einfamilienhaus - Mehrparteienhaus` | `7F_EFH` |
| `(76) Kommunikationsmodul` | `76_KommMod` |
| `(A0) Kennung Fernbedienung A1M1` | `A0_KennFBA1M1` |
| `(E5) Kennung Pumpe Heizkreis A1` | `E5_KennPumpHzkA1` |
| `(30) Kennung Interne Umwälzpumpe` | `30_KennIntUmwPumpe` |
| `(91) Zuordnung externe Betriebsarten-umschaltung` | `ZuordExtBetrUmsch` |
| ... | ... |

Die vollständige Liste befindet sich in `SQL\SqlQueries.vb` im `SQL_DeviceEventQuery`.

## Implementierung

### Hauptklasse
- **`Services\PythonFormatExporter.vb`**: Exportiert DataTable im Python-Format

### Wichtige Methoden
- `ExportToPythonFormat()`: Hauptmethode für Export
- `BuildHierarchy()`: Erstellt Kategorie ? Gruppe ? Parameter Hierarchie
- `WriteHierarchy()`: Schreibt hierarchische Struktur
- `SimplifyHiddenCondition()`: Entfernt doppelte Klammern aus HIDDEN-Strings

## Vergleich mit Python-Ausgabe

Die Ausgabe sollte identisch mit der Python-Ausgabe sein:
- **Python**: `DP_VScotHO1_20.txt` (generiert durch `PrintEventsForDatapoint.py`)
- **VB.NET**: `DP_VScotHO1_20_yyyyMMdd_HHmmss.txt` (generiert durch diese Implementierung)

## Bekannte Einschränkungen

1. **Gruppen-Level HIDDEN-Conditions**: 
   - Diese erfordern eine separate Abfrage aus `ecnDisplayConditionGroup` mit `EventTypeGroupIdDest`
   - Aktuell werden nur Parameter-Level Conditions unterstützt

2. **Original-Parameternamen**:
   - Die ursprünglichen Namen (vor Übersetzung) werden aus der `Beschreibung`-Spalte geholt
   - Falls nicht vorhanden, wird die übersetzte `Parameter`-Spalte verwendet

## Weitere Informationen

Siehe auch:
- `Documentation\VitosoftXML.md` - Datenbank-Struktur
- `PY-Quellcode\InsideViessmannVitosoft\PrintEventsForDatapoint.py` - Original Python-Implementierung
- `SQL\SqlQueries.vb` - SQL-Abfrage mit Shortcuts
