# Using-Block Review & IOException-Fixes

## Problem
Wiederholte `System.IO.IOException` deuten auf **Ressourcen-Lecks** hin:
- Zu viele offene DB-Verbindungen
- Fehlende `Using`-Blocks für IDisposable-Objekte
- Connection-Pool-Erschöpfung

---

## Analyse-Ergebnis

### ? KORREKT: Verwenden bereits Using-Blocks
1. **SqlQueryService.vb**
   - ? `GetDeviceEventsAsync`: Using cn, cmd, rdr
   - ? `GetConfigAddressMappingAsync`: Using cn, cmd, rdr

2. **DataGridExportService.vb**
   - ? `ExportToXml`: Using sfd
   - ? `ExportToXmlDataOnly`: Using sfd

3. **EcnDeviceParser.vb**
   - ? `TryGetDevicesFromDb`: Using cn, listCmd, lr, colCmd, cr, cmd, rdr
   - ? `EnrichDevicesFromDbWithXml`: Using cmd, rdr

---

## ?? PROBLEM-BEREICHE

### 1. SqlQueries.vb (BuildConnectionString)
**KEIN direktes Problem**, aber:
- Erstellt neue DB-Instanzen für jede Connection
- Könnte zu vielen parallelen Attachs führen

**Empfehlung**: Connection-Pooling durch einheitliche `AttachDbFilename` sicherstellen.

---

### 2. Form1.vb - btnAbfrage_Click

**PROBLEM**: Task.Run mit Übersetzungen blockiert möglicherweise zu lange.

```vb
' AKTUELL (potentiell problematisch):
Await Task.Run(Sub()
    Services.TextResourceService.TranslateColumn(dt, "Condition")
    ' ... weitere Übersetzungen
End Sub)
```

**LÖSUNG**: Statt `Sub()` ein `Function()` verwenden, um Exceptions korrekt zu propagieren:

```vb
Await Task.Run(Function() As Boolean
    Try
        Services.TextResourceService.TranslateColumn(dt, "Condition")
    Services.TextResourceService.AddDescriptionColumn(dt, "Parameter")
        ' ... weitere Übersetzungen
        Return True
 Catch ex As Exception
        Debug.WriteLine($"[ERROR] Übersetzung fehlgeschlagen: {ex.Message}")
        Return False
    End Try
End Function)
```

---

### 3. Fehlende Fehlerbehandlung in TextResourceService

**PROBLEM**: `LoadDictionaryFast` fängt alle Exceptions, loggt sie aber nur.

```vb
' AKTUELL:
Catch ex As Exception
    Debug.WriteLine($"Fehler: {ex.Message}")
End Try
Return result ' Könnte leer sein!
```

**EMPFEHLUNG**: Fehlerhafte Dateien explizit melden oder Fallback bereitstellen.

---

## ?? KONKRETE FIXES

### Fix 1: SqlQueries.vb - Robustere Connection
```vb
Public Function BuildConnectionString() As String
  ' ... (bestehender Code)
    
    Dim dbName As String = $"OptolinkEcn_{ComputeHashHex(workingMdf, 8)}"
    
    ' WICHTIG: Pooling explizit aktivieren (Standard: True, aber explizit besser)
    Return $"Data Source=(LocalDB)\MSSQLLocalDB;" &
           $"AttachDbFilename={workingMdf};" &
     $"Initial Catalog={dbName};" &
 $"Integrated Security=True;" &
           $"Connect Timeout=30;" &
     $"Encrypt=False;" &
         $"TrustServerCertificate=True;" &
         $"Pooling=True;" &         ' Explizit aktivieren
           $"Max Pool Size=50;" &        ' Limit setzen
           $"Min Pool Size=5" ' Mindest-Pool
End Function
```

---

### Fix 2: Form1.vb - Robustere Übersetzung
```vb
Private Async Sub btnAbfrage_Click(sender As Object, e As EventArgs) Handles btnAbfrage.Click
    ' ... (bestehender Code bis dt.Rows.Count Check)
    
    Try
        ' ... (Abfrage)
        
   lblProgress.Text = "Übersetze Textressourcen..."
        
        ' WICHTIG: Function() statt Sub() für korrekte Exception-Propagierung
        Dim translationOk = Await Task.Run(Function() As Boolean
         Try
  ' Beschreibung ZUERST (benötigt Original-Parameter)
    Services.TextResourceService.AddDescriptionColumn(dt, "Parameter")
 
          ' Original-Parameter sichern (VOR Übersetzung!)
   Services.TextResourceService.AddOriginalParameterColumn(dt, "Parameter")
 
       ' Übersetzungen durchführen
     Services.TextResourceService.TranslateColumn(dt, "Condition")
      Services.TextResourceService.TranslateParameterColumn(dt, "Parameter")
   Services.TextResourceService.TranslateUnitColumn(dt, "Unit")
     Services.TextResourceService.TranslateColumn(dt, "Gruppe")
         Services.TextResourceService.TranslateColumn(dt, "Kategorie")
          
     ' MQTT-Commands
                Services.MqttCommandGenerator.AddMqttCommandColumns(dt)
 
     Return True
      Catch ex As Exception
   Debug.WriteLine($"[ERROR] Übersetzung fehlgeschlagen: {ex.Message}")
   Debug.WriteLine($"[STACK] {ex.StackTrace}")
      Return False
      End Try
        End Function)
        
        If Not translationOk Then
     MessageBox.Show("Warnung: Textübersetzung unvollständig.", 
     "Warnung", 
              MessageBoxButtons.OK, 
                  MessageBoxIcon.Warning)
        End If
    
        ' ... (Rest des Codes)
        
    Catch ex As Exception
        ' ... (Fehlerbehandlung)
    Finally
        prgDb.Visible = False
        btnAbfrage.Enabled = True
 End Try
End Sub
```

---

### Fix 3: TextResourceService - Robustere Fehlerbehandlung
```vb
Private Shared Function LoadDictionaryFast(path As String) As Dictionary(Of String, String)
    Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    
    If Not File.Exists(path) Then
        Debug.WriteLine($"[TextResourceService] Datei nicht gefunden: {path}")
        Return result ' Leeres Dictionary
    End If
    
    Try
        Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            ' Automatische Encoding-Erkennung
            Using sr As New StreamReader(fs, detectEncodingFromByteOrderMarks:=True)
                Dim lineCount As Integer = 0
            Dim successCount As Integer = 0
     
   While Not sr.EndOfStream
 lineCount += 1
Dim line = sr.ReadLine()
     
         If String.IsNullOrEmpty(line) Then Continue While
          If line.IndexOf("<TextResource", StringComparison.OrdinalIgnoreCase) < 0 Then Continue While
   
      Try
           ' Label erfassen
      Dim mLabel = Regex.Match(line, "Label=""(.*?)""")
       If Not mLabel.Success Then Continue While
      
            Dim label As String = WebUtility.HtmlDecode(mLabel.Groups(1).Value)
     
        ' Value erfassen (letzter Match)
        Dim mVals = Regex.Matches(line, "Value=""(.*?)""")
      If mVals Is Nothing OrElse mVals.Count = 0 Then Continue While
  
         Dim sValue = WebUtility.HtmlDecode(mVals(mVals.Count - 1).Groups(1).Value)
       
        If Not String.IsNullOrWhiteSpace(label) Then
      ' Normalisiere Label
               Dim normalized = If(label.StartsWith("@@"), label.Substring(2), label)
  If sValue Is Nothing Then sValue = String.Empty
      
        ' Beide Varianten speichern
       result.TryAdd(label, sValue)
   If Not String.Equals(label, normalized, StringComparison.Ordinal) Then
          result.TryAdd(normalized, sValue)
    End If
  
           successCount += 1
 End If
  
        Catch lineEx As Exception
           ' Fehlerhafte Zeile überspringen (nicht die ganze Datei!)
Debug.WriteLine($"[TextResourceService] Zeile {lineCount} fehlerhaft: {lineEx.Message}")
     End Try
          End While
          
      Debug.WriteLine($"[TextResourceService] Geladen: {successCount} von {lineCount} Zeilen")
  End Using
        End Using
        
    Catch ex As IOException
        Debug.WriteLine($"[TextResourceService] IO-Fehler: {ex.Message}")
    Catch ex As UnauthorizedAccessException
      Debug.WriteLine($"[TextResourceService] Zugriff verweigert: {ex.Message}")
    Catch ex As Exception
        Debug.WriteLine($"[TextResourceService] Unerwarteter Fehler: {ex.Message}")
    End Try
    
    Return result
End Function
```

---

## ?? PRIORITY FIXES (IOExceptions verhindern)

### 1. **SOFORT**: Connection-Pooling optimieren
- ? Max/Min Pool Size setzen
- ? Connection-Timeout erhöhen (von 30s auf 60s)

### 2. **WICHTIG**: Task.Run korrekt verwenden
- ? `Function()` statt `Sub()` für Exception-Propagierung
- ? Try-Catch in Task-Body

### 3. **OPTIONAL**: Retry-Logic für DB-Queries
```vb
Public Shared Async Function GetDeviceEventsAsyncWithRetry(
    deviceName As String, 
    Optional maxRetries As Integer = 3) As Task(Of DataTable)
    
    For attempt = 1 To maxRetries
    Try
         Return Await GetDeviceEventsAsync(deviceName)
        Catch ex As SqlException When attempt < maxRetries
 Debug.WriteLine($"[RETRY] Versuch {attempt} fehlgeschlagen, warte 500ms...")
        Await Task.Delay(500)
   End Try
    Next
    
    ' Letzter Versuch ohne Retry
    Return Await GetDeviceEventsAsync(deviceName)
End Function
```

---

## ? CHECKLISTE

- [x] SqlQueryService: Using-Blocks vorhanden
- [x] EcnDeviceParser: Using-Blocks vorhanden
- [x] DataGridExportService: Using-Blocks vorhanden
- [ ] **Fix 1**: Connection-Pooling optimieren (SqlQueries.vb)
- [ ] **Fix 2**: Task.Run robuster machen (Form1.vb)
- [ ] **Fix 3**: TextResourceService Fehlerbehandlung (TextResourceService.vb)
- [ ] **Optional**: Retry-Logic für DB-Queries

---

## ?? ERWARTETER EFFEKT

**Nach Implementierung der Fixes**:
- ? Keine IOExceptions mehr (Connection-Pool exhausted)
- ? Bessere Fehler-Logs (wo genau schlägt was fehl?)
- ? Graceful Degradation (leere Übersetzungen statt Crash)
