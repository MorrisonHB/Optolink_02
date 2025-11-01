Imports System.Text


Public Class Form1

    Friend ReadOnly _mqtt As New MqttService()
    Friend ReadOnly _ssh As New RaspberryPiSshService()
    Private updatingTopics As Boolean = False
    Private Shared ReadOnly Separator As Char() = {" "c}
    Private Shared ReadOnly RPi_CommandValue As String() = {
 "set -e;",
 "echo '=== Running/failed services (filtered) ===';",
 "systemctl list-units --type=service --state=running,failed | sed -n '1,3p';",
 "systemctl list-units --type=service --state=running,failed | grep -Ei 'vito|optolink|vcontrol|mosquitto|mqtt' || true;",
 "echo; echo '=== Installed service states (filtered) ===';",
 "systemctl list-unit-files --type=service | grep -Ei 'vito|optolink|vcontrol|mosquitto|mqtt' || true;",
 "echo; echo '=== Active state of installed (filtered) ===';",
 "systemctl list-unit-files --type=service --no-pager | grep -Ei 'vito|optolink|vcontrol|mosquitto|mqtt' | awk '{print $1}' | while read s; do printf '%s\t' ""$s""; systemctl is-active ""$s"" || true; done;",
 "echo; echo '=== Mosquitto status ===';",
 "systemctl status mosquitto.service --no-pager2>&1 | sed -n '1,12p' || true;"
 }

    ' Hintergrundinitialisierung (bewusst nicht awaited)
    Private _dbWarmupTask As Task

    ' Device Hierarchy für TreeView/ListView
    Private _currentDeviceHierarchy As Domain.DeviceNode = Nothing
    Private _identifiedDevice As String = Nothing

    ' ============================================================
    ' INITIALIZATION METHODS
    ' ============================================================

    ''' <summary>
    ''' Lädt Geräte in die ComboBox
    ''' </summary>
    Private Sub LoadDevices()
        Services.DeviceService.LoadDevices(cmbDevices)
    End Sub

    ''' <summary>
    ''' Initialisiert die Datenbank-Umgebung im Hintergrund
    ''' </summary>
    Private Sub Init_Database()
        _dbWarmupTask = Services.DatabaseInitService.WarmupAsync(AddressOf BuildConnectionString)
    End Sub

    ''' <summary>
    ''' Schreibt eine Log-Nachricht in txtLog (Thread-safe)
    ''' </summary>
    Friend Sub AppendLog(text As String)
        Try
            If txtLog.InvokeRequired Then
                txtLog.Invoke(Sub() txtLog.AppendText(text & Environment.NewLine))
            Else
                txtLog.AppendText(text & Environment.NewLine)
            End If
        Catch ex As Exception
            Debug.WriteLine($"[ERROR] AppendLog fehlgeschlagen: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' MQTT Message Received Handler
    ''' </summary>
    Friend Sub Mqtt_MessageReceived(topic As String, payload As String)
        ' Kann für zusätzliches Logging verwendet werden
        Debug.WriteLine($"[MQTT] Empfangen auf '{topic}': {payload}")
    End Sub

    ''' <summary>
    ''' MQTT Log Handler
    ''' </summary>
    Friend Sub Mqtt_Log(message As String)
        AppendLog(message)
    End Sub

    ' ============================================================
    ' LISTVIEW PARAMETER DETAILS - SORTIERUNG & KONTEXTMENÜ
    ' ============================================================

    Private _lvParameterSortColumn As Integer = -1
    Private _lvParameterSortOrder As System.Windows.Forms.SortOrder = System.Windows.Forms.SortOrder.None

    ''' <summary>
    ''' TreeView AfterSelect - zeigt Parameter der ausgewählten Gruppe an
    ''' </summary>
    Private Sub TreeDeviceHierarchy_AfterSelect(sender As Object,
                                                e As TreeViewEventArgs) Handles treeDeviceHierarchy.AfterSelect
        Try
            Services.DeviceHierarchyBuilder.UpdateParameterListView(lvParameterDetails, e.Node)
        Catch ex As Exception
            Debug.WriteLine($"[ERROR] TreeView AfterSelect: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Sortierung bei Klick auf Spaltenüberschrift
    ''' </summary>
    Private Sub lvParameterDetails_ColumnClick(
                                              sender As Object,
                                              e As ColumnClickEventArgs) Handles lvParameterDetails.ColumnClick
        Try
            ' Wenn gleiche Spalte, toggle Sort Order
            If e.Column = _lvParameterSortColumn Then
                _lvParameterSortOrder = If(
                    _lvParameterSortOrder = System.Windows.Forms.SortOrder.Ascending,
                    System.Windows.Forms.SortOrder.Descending,
                    System.Windows.Forms.SortOrder.Ascending)
            Else
                ' Neue Spalte, starte mit Ascending
                _lvParameterSortColumn = e.Column
                _lvParameterSortOrder = System.Windows.Forms.SortOrder.Ascending
            End If

            ' Sortiere ListView
            lvParameterDetails.ListViewItemSorter = New ListViewItemComparer(e.Column, _lvParameterSortOrder)
            lvParameterDetails.Sort()
        Catch ex As Exception
            Debug.WriteLine($"[ERROR] ListView ColumnClick: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Kontextmenü: Direkt Abfragen
    ''' </summary>
    Private Async Sub mnuParamDirectQuery_Click(sender As Object, e As EventArgs) Handles mnuParamDirectQuery.Click
        Try
            If lvParameterDetails.SelectedItems.Count = 0 Then Return

            Dim selectedItem = lvParameterDetails.SelectedItems(0)
            Dim param = TryCast(selectedItem.Tag, Domain.ParameterNode)
            If param Is Nothing OrElse String.IsNullOrWhiteSpace(param.Address) Then
                Dim unused3 = MessageBox.Show("Kein gültiger Parameter ausgewählt.",
                                              "Fehler",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Warning)
                Return
            End If

            If Not _mqtt.IsConnected Then
                Dim unused2 = MessageBox.Show("MQTT nicht verbunden.",
                                              "Fehler",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Warning)
                Return
            End If

            ' Zeige "wird abgefragt..."
            Dim originalValue = param.CurrentValue
            param.CurrentValue = "⏳ wird abgefragt..."
            selectedItem.SubItems(1).Text = param.CurrentValue
            lvParameterDetails.Refresh()

            ' MQTT-Abfrage
            Dim sendTopic = If(cmbTopicSend?.SelectedItem?.ToString(), "viessmann/cmnd")
            Dim receiveTopic = If(cmbTopicReceive?.SelectedItem?.ToString(), "viessmann/status")

            ' WICHTIG: Verwende MqttCommandGenerator um korrektes Format mit scale und signed zu erhalten
            Dim command = Services.MqttCommandGenerator.GenerateReadCommand(
         param.Address,
              param.ByteLength.ToString(),
     param.Conversion,
         param.DataType)

            ' Fallback wenn Generator nichts zurückgibt
            If String.IsNullOrWhiteSpace(command) Then
                command = $"read;{param.Address};{param.ByteLength}"
            End If

            Debug.WriteLine($"[DIRECT-QUERY] MQTT Command: {command}")

            Try
                Dim response = Await _mqtt.SendCommandAndWaitForResponseAsync(command,
                                                                              sendTopic,
                                                                              receiveTopic,
                                                                              3000)

                If Not String.IsNullOrWhiteSpace(response) Then
                    Dim parts = response.Split(";"c)
                    If parts.Length >= 3 AndAlso parts(0) = "1" Then
                        Dim rawValue = parts(2).Trim()

                        ' WICHTIG: Optolink-Splitter liefert bereits konvertierte Werte!
                        ' Keine weitere Konvertierung nötig - direkt verwenden
                        Dim convertedValue = rawValue

                        Debug.WriteLine($"[DIRECT-QUERY] Response value: {convertedValue}")

                        param.CurrentValue = convertedValue
                        selectedItem.SubItems(1).Text = convertedValue
                        AppendLog($"✓ Direkt-Abfrage: {param.ParameterName} = {convertedValue}")

                    Else
                        param.CurrentValue = "❌ Fehler"
                        selectedItem.SubItems(1).Text = param.CurrentValue
                        AppendLog($"✗ Direkt-Abfrage fehlgeschlagen: {param.ParameterName}")
                    End If
                Else
                    param.CurrentValue = "⏱️ Timeout"
                    selectedItem.SubItems(1).Text = param.CurrentValue
                    AppendLog($"⏱️ Timeout bei Direkt-Abfrage: {param.ParameterName}")
                End If
            Catch mqttEx As Exception
                param.CurrentValue = originalValue
                selectedItem.SubItems(1).Text = originalValue
                Dim unused1 = MessageBox.Show($"Fehler bei Abfrage: {mqttEx.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                lvParameterDetails.Refresh()
            End Try

        Catch ex As Exception
            Debug.WriteLine($"[ERROR] Direkt-Abfrage: {ex.Message}")
            Dim unused = MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Kontextmenü: Abfragen (mit Dialog)
    ''' </summary>
    Private Sub mnuParamQuery_Click(sender As Object, e As EventArgs) Handles mnuParamQuery.Click
        Try
            If lvParameterDetails.SelectedItems.Count = 0 Then Return

            Dim selectedItem = lvParameterDetails.SelectedItems(0)
            Dim param = TryCast(selectedItem.Tag, Domain.ParameterNode)
            If param Is Nothing Then
                Dim unused1 = MessageBox.Show("Kein gültiger Parameter ausgewählt.",
                                              "Fehler",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Warning)
                Return
            End If

            ' Öffne Dialog
            Using dlg As New ParameterQueryDialog(param,
                                                  _mqtt, cmbTopicSend?.SelectedItem?.ToString(),
                                                  cmbTopicReceive?.SelectedItem?.ToString())
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    ' Aktualisiere ListView mit neuem Wert
                    selectedItem.SubItems(1).Text = param.CurrentValue
                    lvParameterDetails.Refresh()
                    AppendLog($"✓ Parameter abgefragt: {param.ParameterName} = {param.CurrentValue}")
                End If
            End Using

        Catch ex As Exception
            Debug.WriteLine($"[ERROR] Parameter-Abfrage-Dialog: {ex.Message}")
            Dim unused = MessageBox.Show($"Fehler: {ex.Message}",
                                         "Fehler",
                                         MessageBoxButtons.OK,
                                         MessageBoxIcon.Error)
        End Try
    End Sub

    ' ============================================================
    ' FORM EVENTS
    ' ============================================================

    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        btnDisconnect.Enabled = False
        btnPiDisconnect.Enabled = False
        btnAbfrage.Enabled = False

        ' Lampe initial auf Rot setzen
        lblPiStatusLamp.ForeColor = Color.Red

        ' DataDirectory für LocalDB setzen
        AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory)

        ' Nur Geräteliste laden (schnell, XML-only Standard in EcnDeviceParser)
        LoadDevices()

        ' Datenbank-Initialisierung im Hintergrund vorwärmen (Attach/Erstellung kann dauern)
        Init_Database()

        ' Autodiscovery für Raspberry Pi und MQTT Hosts starten
        Await Services.UiDiscoveryService.StartAutoDiscoveryAsync(Me)
    End Sub

    Private Async Sub btnAbfrage_Click(sender As Object, e As EventArgs) Handles btnAbfrage.Click
        Dim selectedName As String = TryCast(Me.cmbDevices.SelectedValue, String)
        If String.IsNullOrWhiteSpace(selectedName) Then
            Dim unused1 = MessageBox.Show("Bitte ein Gerät wählen.")
            Return
        End If

        Try
            prgDb.Visible = True
            lblProgress.Visible = True
            lblProgress.Text = "Lade Gerätedaten..."
            btnAbfrage.Enabled = False

            ' Daten asynchron abrufen
            Dim dt As DataTable = Await Services.SqlQueryService.GetDeviceEventsAsync(selectedName)

            ' Debug: Spalteninfo ausgeben
            If dt.Columns.Contains("Condition") Then
                Dim col = dt.Columns("Condition")
                Debug.WriteLine($"[DEBUG] Condition-Spalte VORHER: ReadOnly={col.ReadOnly}, DataType={col.DataType.Name}")

                ' Condition-Spalte als beschreibbar markieren, damit die Übersetzung funktioniert
                col.ReadOnly = False
                Debug.WriteLine($"[DEBUG] Condition-Spalte NACHHER: {col.ReadOnly}")

                ' Debug: Finde die erste Zeile mit @@-Token
                Dim firstTokenRow As Integer = -1
                For i As Integer = 0 To Math.Min(dt.Rows.Count - 1, 50)
                    Dim val = Convert.ToString(dt.Rows(i)("Condition"))
                    If Not String.IsNullOrEmpty(val) AndAlso val.Contains("@@") Then
                        firstTokenRow = i
                        Debug.WriteLine($"[DEBUG] Erste Zeile mit @@-Token: {i}")
                        Debug.WriteLine($"[DEBUG] Wert VORHER: {If(val.Length > 200,
                                        String.Concat(val.AsSpan(0, 200), "..."), val)}")
                        Exit For
                    End If
                Next

                If firstTokenRow = -1 Then
                    Debug.WriteLine("[DEBUG] Keine @@-Tokens in den ersten 50 Zeilen gefunden!")
                End If
            End If

            ' Parameter-Spalte als beschreibbar markieren
            If dt.Columns.Contains("Parameter") Then
                Dim paramCol = dt.Columns("Parameter")
                paramCol.ReadOnly = False
                Debug.WriteLine($"[DEBUG] Parameter-Spalte: ReadOnly={paramCol.ReadOnly}")
            End If

            lblProgress.Text = "Übersetze Textressourcen..."

            ' WICHTIG: Übersetzung ASYNC im Background-Thread durchführen!
            Await Task.Run(Sub()
                               ' Condition-Spalte mit Textresource_de.xml übersetzen (nur wenn @@-Tokens vorhanden)
                               Try
                                   Services.TextResourceService.TranslateColumn(dt, "Condition")
                                   Debug.WriteLine("[DEBUG] TranslateColumn erfolgreich")

                                   ' Debug: Überprüfe die GLEICHE Zeile NACH der Übersetzung
                                   If dt.Columns.Contains("Condition") Then
                                       For i As Integer = 0 To Math.Min(dt.Rows.Count - 1, 50)
                                           Dim val = Convert.ToString(dt.Rows(i)("Condition"))
                                           If Not String.IsNullOrEmpty(val) AndAlso
                                           Not val.Contains("@@") AndAlso val.Contains("HIDDEN:(") Then
                                               Debug.WriteLine($"[DEBUG] Zeile {i} NACHHER (übersetzt): {If(val.Length > 200, String.Concat(val.AsSpan(0, 200), "..."), val)}")
                                               Exit For
                                           End If
                                       Next
                                   End If

                               Catch ex As Data.ReadOnlyException
                                   Debug.WriteLine($"[DEBUG] TranslateColumn fehlgeschlagen (ReadOnly): {ex.Message}")
                               Catch ex As Exception
                                   Debug.WriteLine($"[DEBUG] TranslateColumn fehlgeschlagen: {ex.Message}")
                               End Try

                               ' WICHTIG: Beschreibung-Spalte ZUERST hinzufügen (BEVOR Parameter übersetzt wird!)
                               ' Die Beschreibungen verwenden die Original-Parameternamen als Schlüssel
                               Try
                                   Services.TextResourceService.AddDescriptionColumn(dt, "Parameter")
                                   Debug.WriteLine("[DEBUG] AddDescriptionColumn erfolgreich")
                               Catch ex As Exception
                                   Debug.WriteLine($"[DEBUG] AddDescriptionColumn fehlgeschlagen: {ex.Message}")
                               End Try

                               ' WICHTIG: Original-Parameter-Spalte hinzufügen VOR der Übersetzung!
                               ' Diese Spalte speichert den kompletten Original-Wert: "GWG_Flamme~0x55D3"
                               Try
                                   Services.TextResourceService.AddOriginalParameterColumn(dt, "Parameter")
                                   Debug.WriteLine("[DEBUG] AddOriginalParameterColumn erfolgreich")
                               Catch ex As Exception
                                   Debug.WriteLine($"[DEBUG] AddOriginalParameterColumn fehlgeschlagen: {ex.Message}")
                               End Try

                               ' Parameter-Spalte übersetzen (Wert bis zur Tilde) - NACH AddDescriptionColumn UND AddOriginalParameterColumn!
                               Try
                                   Services.TextResourceService.TranslateParameterColumn(dt, "Parameter")
                                   Debug.WriteLine("[DEBUG] TranslateParameterColumn erfolgreich")
                               Catch ex As Exception
                                   Debug.WriteLine($"[DEBUG] TranslateParameterColumn fehlgeschlagen: {ex.Message}")
                               End Try

                               ' Unit-Spalte übersetzen (ecnUnit. entfernen)
                               Try
                                   Services.TextResourceService.TranslateUnitColumn(dt, "Unit")
                                   Debug.WriteLine("[DEBUG] TranslateUnitColumn erfolgreich")
                               Catch ex As Exception
                                   Debug.WriteLine($"[DEBUG] TranslateUnitColumn fehlgeschlagen: {ex.Message}")
                               End Try

                               ' Gruppe-Spalte übersetzen
                               Try
                                   Services.TextResourceService.TranslateColumn(dt, "Gruppe")
                                   Debug.WriteLine("[DEBUG] TranslateColumn(Gruppe) erfolgreich")
                               Catch ex As Exception
                                   Debug.WriteLine($"[DEBUG] TranslateColumn(Gruppe) fehlgeschlagen: {ex.Message}")
                               End Try

                               'Kategorie-Spalte übersetzen
                               Try
                                   Services.TextResourceService.TranslateColumn(dt, "Kategorie")
                                   Debug.WriteLine("[DEBUG] TranslateColumn(Kategorie) erfolgreich")
                               Catch ex As Exception
                                   Debug.WriteLine($"[DEBUG] TranslateColumn(Kategorie) fehlgeschlagen: {ex.Message}")
                               End Try

                               ' ============================================
                               ' MQTT-Kommandos generieren
                               ' ============================================
                               Try
                                   Debug.WriteLine("[DEBUG] Starte MQTT-Command-Generierung...")
                                   Services.MqttCommandGenerator.AddMqttCommandColumns(dt)
                                   Debug.WriteLine("[DEBUG] MQTT-Command-Generierung erfolgreich")
                               Catch ex As Exception
                                   Debug.WriteLine($"[ERROR] MQTT-Command-Generierung fehlgeschlagen: {ex.Message}")
                               End Try
                           End Sub)

            ' NACH der Übersetzung: DataSource binden (im UI-Thread)
            gridResults.DataSource = dt
            Debug.WriteLine($"[DEBUG] DataSource gebunden: {dt.Rows.Count} Zeilen, {dt.Columns.Count} Spalten")

            lblProgress.Text = "Wende Filter an..."

            ' ============================================
            ' HIDDEN-Filter anwenden und gefilterte Ansicht erstellen
            ' ============================================
            Try
                Debug.WriteLine("[DEBUG] Starte HIDDEN-Filter...")

                ' Sammle alle benötigten Parameter-IDs aus den HIDDEN-Strings
                Dim hiddenStrings = dt.AsEnumerable().
                    Select(Function(r) Convert.ToString(r("Condition"))).
                    Where(Function(s) Not String.IsNullOrWhiteSpace(s) AndAlso s.Contains("HIDDEN:"))


                Dim requiredIds = Services.HiddenConditionFilter.ExtractRequiredParameterIds(hiddenStrings)
                Debug.WriteLine($"[DEBUG] Benötigte Parameter-IDs für Filter: {String.Join(", ", requiredIds)}")

                ' ============================================
                ' MQTT-Abfrage der benötigten Werte (wenn MQTT verbunden)
                ' ============================================
                Dim filteredDt As DataTable

                If _mqtt.IsConnected AndAlso requiredIds.Count > 0 Then
                    Debug.WriteLine("[DEBUG] MQTT ist verbunden -> Starte automatische Geräteabfrage...")

                    Try
                        ' Hole die MQTT-Topics aus den UI-Controls
                        Dim sendTopic = If(cmbTopicSend?.SelectedItem?.ToString(), "viessmann/cmnd")
                        Dim receiveTopic = If(cmbTopicReceive?.SelectedItem?.ToString(), "viessmann/status")

                        Debug.WriteLine($"[DEBUG] Verwende Topics: Send='{sendTopic}', Receive='{receiveTopic}'")

                        ' Asynchrone Filterung mit automatischer MQTT-Abfrage
                        filteredDt = Await Services.HiddenConditionFilter.FilterByHiddenConditionsAsync(
                              dt, _mqtt, sendTopic, receiveTopic, timeoutMs:=3000)

                        Debug.WriteLine($"[DEBUG] MQTT-basierte Filterung abgeschlossen: {filteredDt.Rows.Count} von {dt.Rows.Count} Zeilen sichtbar")

                    Catch ex As Exception
                        Debug.WriteLine($"[ERROR] MQTT-Abfrage fehlgeschlagen: {ex.Message}")
                        Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}")
                        Debug.WriteLine("[DEBUG] Fallback: Zeige alle Daten ohne Filterung")
                        filteredDt = dt.Copy()
                    End Try
                Else
                    ' MQTT nicht verbunden oder keine HIDDEN-Conditions -> Mock-Werte verwenden
                    Debug.WriteLine("[DEBUG] MQTT nicht verbunden -> Verwende Mock-Werte für Test")

                    ' Erstelle ein Mock-Dictionary mit Gerätewerten für Testzwecke
                    ' TODO: Diese Werte sollten später vom Gerät abgefragt werden (z.B. über 0x7700)
                    Dim deviceValues As New Dictionary(Of String, String) From {
                {"00", "2 A1 + WW"} ' Beispiel: Heizkreis-Warmwasserschema = "2 A1 + WW"
                   }

                    Debug.WriteLine($"[DEBUG] Mock-Gerätewerte: {String.Join(", ", deviceValues.Select(Function(kvp) $"{kvp.Key}={kvp.Value}"))}")

                    ' Filtere die DataTable basierend auf HIDDEN-Bedingungen
                    filteredDt = Services.HiddenConditionFilter.FilterByHiddenConditions(dt, deviceValues)
                    Debug.WriteLine($"[DEBUG] Mock-basierte Filterung: {filteredDt.Rows.Count} von {dt.Rows.Count} Zeilen sichtbar")
                End If

                ' Binde die gefilterte DataTable an das zweite DataGridView
                gridResultsFiltered.DataSource = filteredDt

                lblProgress.Text = $"Abfrage abgeschlossen: {filteredDt.Rows.Count} von {dt.Rows.Count} Zeilen sichtbar"

                ' ============================================
                ' HIERARCHISCHE ANSICHT ERSTELLEN (wenn MQTT verbunden)
                ' ============================================
                If _mqtt.IsConnected Then
                    Try
                        lblProgress.Text = "Erstelle hierarchische Ansicht..."

                        ' Hole Topics aus UI (wieder verwenden)
                        Dim hierarchySendTopic = If(cmbTopicSend?.SelectedItem?.ToString(), "viessmann/cmnd")
                        Dim hierarchyReceiveTopic = If(cmbTopicReceive?.SelectedItem?.ToString(), "viessmann/status")

                        ' Erstelle Device-Hierarchie aus DataTable
                        Dim deviceHierarchy = Services.DeviceHierarchyBuilder.BuildHierarchy(dt)

                        If deviceHierarchy IsNot Nothing Then
                            ' Speichere Hierarchie für Event-Handler
                            _currentDeviceHierarchy = deviceHierarchy

                            ' Fülle TreeView mit Hierarchie
                            Services.DeviceHierarchyBuilder.PopulateTreeView(treeDeviceHierarchy, deviceHierarchy, selectedName)

                            ' Fortschritts-Callback für MQTT-Abfragen
                            Dim hierarchyProgressCallback As Action(Of String) =
                                Sub(msg As String)
                                    Try
                                        Me.Invoke(Sub() lblProgress.Text = msg)
                                    Catch
                                        ' Ignoriere Invoke-Fehler
                                    End Try
                                End Sub

                            ' WICHTIG: Verwende den OPTIMIERTEN Service!
                            Debug.WriteLine("[HIERARCHY] Verwende OptimizedMqttQueryService")
                            Dim unused2 =
                                Await Services.OptimizedMqttQueryService.QueryDeviceHierarchyOptimizedAsync(
                                     deviceHierarchy,
                                     _mqtt,
                                     hierarchySendTopic,
                                     hierarchyReceiveTopic,
                                     3000,
                                     hierarchyProgressCallback)

                            lblProgress.Text = "Hierarchische Ansicht aktualisiert"
                        End If
                    Catch hierarchyEx As Exception
                        Debug.WriteLine($"[ERROR] Hierarchische Ansicht: {hierarchyEx.Message}")
                        ' Fehler nicht anzeigen, da Hauptabfrage erfolgreich war
                    End Try
                End If

            Catch filterEx As Exception
                Debug.WriteLine($"[ERROR] Filter fehlgeschlagen: {filterEx}")
                lblProgress.Text = $"Filter-Fehler: {filterEx.Message}"
                ' Zeige ungefilterte Daten im gefilterten Tab als Fallback
                gridResultsFiltered.DataSource = dt.Copy()
            End Try

        Catch ex As Exception
            Debug.WriteLine($"[ERROR] Abfrage fehlgeschlagen: {ex}")
            Dim unused = MessageBox.Show("Abfrage fehlgeschlagen: " & ex.Message)
            lblProgress.Text = $"Fehler: {ex.Message}"
        Finally
            prgDb.Visible = False
            btnAbfrage.Enabled = True
            ' lblProgress bleibt sichtbar für Status-Anzeige
        End Try
    End Sub

    Private Sub mnuExportXml_Click(sender As Object, e As EventArgs) Handles mnuExportXml.Click
        Try
            ' Ermittle welches DataGridView aktiv ist (welcher Tab ausgewählt ist)
            Dim activeGrid As DataGridView = Nothing
            Dim defaultFileName As String = "Export.xml"

            If tabDbResults.SelectedTab Is tabDbUngefiltert Then
                activeGrid = gridResults
                defaultFileName = $"{cmbDevices.Text}_Ungefiltert_{DateTime.Now:yyyyMMdd_HHmmss}.xml"
            ElseIf tabDbResults.SelectedTab Is tabDbGefiltert Then
                activeGrid = gridResultsFiltered
                defaultFileName = $"{cmbDevices.Text}_Gefiltert_{DateTime.Now:yyyyMMdd_HHmmss}.xml"
            Else
                MessageBox.Show("Bitte wählen Sie einen Tab mit Tabellendaten aus.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Exportiere die Daten
            Services.DataGridExportService.ExportToXml(activeGrid, defaultFileName)

        Catch ex As Exception
            Debug.WriteLine($"[ERROR] XML-Export: {ex.Message}")
            MessageBox.Show($"Fehler beim Export: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Exportiert die aktuelle Ansicht im Python-Format (wie PrintEventsForDatapoint.py)
    ''' </summary>
    Private Sub mnuExportPythonFormat_Click(sender As Object, e As EventArgs) Handles mnuExportPythonFormat.Click
        Try
            ' Prüfe ob ein Gerät ausgewählt ist
            Dim selectedName As String = TryCast(Me.cmbDevices.SelectedValue, String)
            If String.IsNullOrWhiteSpace(selectedName) Then
                MessageBox.Show("Bitte wählen Sie ein Gerät aus.",
                                "Export",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Return
            End If

            ' Ermittle welches DataGridView aktiv ist (welcher Tab ausgewählt ist)
            Dim activeGrid As DataGridView = Nothing
            Dim defaultFileName As String = "Export.txt"
            Dim tabName As String = ""

            If tabDbResults.SelectedTab Is tabDbUngefiltert Then
                activeGrid = gridResults
                tabName = "Ungefiltert"
                defaultFileName = $"DP_{selectedName}_{DateTime.Now:yyyyMMdd_HHmms}.txt"
            ElseIf tabDbResults.SelectedTab Is tabDbGefiltert Then
                activeGrid = gridResultsFiltered
                tabName = "Gefiltert"
                defaultFileName = $"DP_{selectedName}_Filtered_{DateTime.Now:yyyyMMdd_HHmms}.txt"
            Else
                MessageBox.Show("Bitte wählen Sie einen Tab mit Tabellendaten aus.",
                                "Export",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Return
            End If

            ' Hole DataTable vom Grid
            Dim dt As DataTable = TryCast(activeGrid.DataSource, DataTable)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                MessageBox.Show("Keine Daten zum Exportieren vorhanden.",
                                "Export",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Return
            End If

            ' Öffne SaveFileDialog
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Text-Dateien (*.txt)|*.txt|Alle Dateien (*.*)|*.*"
                sfd.FileName = defaultFileName
                sfd.Title = "Python-Format exportieren"
                sfd.DefaultExt = "txt"

                If sfd.ShowDialog(Me) = DialogResult.OK Then
                    ' Exportiere im Python-Format
                    Services.PythonFormatExporter.ExportToPythonFormat(dt, selectedName, sfd.FileName)

                    ' Erfolgsmeldung
                    MessageBox.Show($"Export erfolgreich nach:{Environment.NewLine}{sfd.FileName}",
                "Export abgeschlossen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

                    ' Optional: Öffne die Datei im Standard-Editor
                    If MessageBox.Show("Möchten Sie die exportierte Datei jetzt öffnen?",
                    "Export",
                  MessageBoxButtons.YesNo,
                     MessageBoxIcon.Question) = DialogResult.Yes Then
                        Process.Start(New ProcessStartInfo(sfd.FileName) With {.UseShellExecute = True})
                    End If
                End If
            End Using

        Catch ex As Exception
            Debug.WriteLine($"[ERROR] Python-Format-Export: {ex.Message}")
            MessageBox.Show($"Fehler beim Export: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Class