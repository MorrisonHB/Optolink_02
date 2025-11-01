# Vergleich Python vs. VB.NET Export

## Hauptunterschiede zwischen den Ausgaben

### 1. ? **Parameter-Namen** (GELÖST)
**Python (korrekt):**
```
- Brenner (600) [GWG_Flamme~0x55D3 (Byte)]
```

**VB.NET (vorher - Problem):**
```
- Brenner (600) [Unknown~0x55D3 (Byte)]
```

**Lösung:** Die `Beschreibung`-Spalte enthält den Original-Parameternamen vor der Übersetzung. Diese wird jetzt verwendet.

---

### 2. ? **HIDDEN-Conditions - Shortcuts** (GELÖST)
**Python (korrekt - mit Shortcuts):**
```
HIDDEN:(30_KennIntUmwPumpe="0 stufig")
HIDDEN:(00_WWS="3 M2" OR 00_WWS="4 M2 +  WW")
```

**VB.NET (vorher - Problem):**
```
HIDDEN:("(30) Kennung Interne Umwälzpumpe"="0 stufig")
HIDDEN:("(00) Heizkreis-Warmwasserschema"="3 M2" OR "(00) Heizkreis-Warmwasserschema"="4 M2 +  WW")
```

**Status:** Die SQL-Query in `SqlQueries.vb` macht bereits die Ersetzung mit REPLACE-Statements. Die HIDDEN-Conditions sollten bereits mit Shortcuts kommen.

**Prüfung erforderlich:**
1. Ist die `Condition`-Spalte in der DataTable bereits mit Shortcuts?
2. Debug-Ausgabe im `btnAbfrage_Click` prüfen
3. Falls nein: SQL-Query `SQL_DeviceEventQuery` überprüfen

---

### 3. ?? **Doppelte Anführungszeichen in HIDDEN** (Zu prüfen)
**Python (korrekt - ohne doppelte Quotes):**
```
HIDDEN:(00_WWS="3 M2" OR 00_WWS="4 M2 +  WW")
```

**VB.NET (möglicherweise):**
```
HIDDEN:("00_WWS"="3 M2" OR "00_WWS"="4 M2 +  WW")
```

**Lösung:** Falls vorhanden, in `SimplifyHiddenCondition()` die doppelten Quotes entfernen:
```vb
innerContent = Regex.Replace(innerContent, """(\d+_\w+)""", "$1")
```

---

### 4. ? **Gruppen-IDs** (IMPLEMENTIERT)
**Python:**
```
- Heizkreis A1 (13745) HIDDEN:(00_WWS="3 M2" OR 00_WWS="4 M2 +  WW")
```

**VB.NET:**
```
- Heizkreis A1 (13745)
```

**Status:** Die EventTypeId wird bereits aus der ersten Row der Gruppe geholt. Gruppen-Level HIDDEN-Conditions sind in `GetGruppenCondition()` noch nicht implementiert.

**TODO für vollständige Implementierung:**
```vb
Private Shared Function GetGruppenCondition(parameters As List(Of DataRow), dt As DataTable) As String
    ' Hole EventTypeGroupIdDest aus erster Row
    Dim firstRow = parameters.FirstOrDefault()
    If firstRow IsNot Nothing AndAlso dt.Columns.Contains("GruppenCondition") Then
        Dim gruppenHidden = Convert.ToString(firstRow("GruppenCondition"))
     If Not String.IsNullOrWhiteSpace(gruppenHidden) Then
     Return " " & SimplifyHiddenCondition(gruppenHidden)
        End If
    End If
    Return String.Empty
End Function
```

---

## Testing-Checkliste

### ? Schritt 1: Prüfe Original-Parameternamen
```sql
SELECT TOP 5 
    et.Name AS Parameter,
    et.Address
FROM ecnEventType et
WHERE et.Address LIKE '%Flamme%'
```
Erwartung: `GWG_Flamme~0x55D3`

### ? Schritt 2: Prüfe Shortcuts in Conditions
Debug-Ausgabe in `btnAbfrage_Click`:
```vb
' Nach der Übersetzung
For i As Integer = 0 To Math.Min(dt.Rows.Count - 1, 10)
    Dim cond = Convert.ToString(dt.Rows(i)("Condition"))
    If Not String.IsNullOrEmpty(cond) AndAlso cond.Contains("HIDDEN:") Then
        Debug.WriteLine($"[CONDITION {i}] {cond}")
    End If
Next
```

Erwartung: `HIDDEN:(30_KennIntUmwPumpe="0 stufig")`

### ?? Schritt 3: Vergleiche Ausgaben
```powershell
# Vergleiche erste 50 Zeilen
Compare-Object `
    (Get-Content "DP_VScotHO1_20.txt" -TotalCount 50) `
    (Get-Content "DP_VScotHO1_20_20251101_102051.txt" -TotalCount 50)
```

---

## Bekannte Probleme

### Problem 1: Encoding (Umlaute)
**Python:** `Ferien Rückreisetag A1M1`
**VB.NET:** `Ferien RÃ¼ckreisetag A1M1`

**Lösung:** UTF-8 Encoding wird bereits verwendet in `ExportToPythonFormat()`:
```vb
Using writer As New StreamWriter(outputPath, False, Encoding.UTF8)
```

### Problem 2: Sonderzeichen in HIDDEN
**Python:** `00_WWSâ‰ "2 A1  + WW"`
**Bedeutung:** `â‰ ` = `?` (Ungleich)

**Status:** Sollte aus der Datenbank korrekt kommen. Falls nicht, in SQL-Query prüfen.

---

## Nächste Schritte

1. ? **Original-Parameternamen verwenden** - IMPLEMENTIERT
2. ?? **Prüfe ob Shortcuts bereits in DB vorhanden** - ZU TESTEN
3. ?? **Gruppen-Level HIDDEN ergänzen** - Optional
4. ?? **Export testen und vergleichen** - ERFORDERLICH

