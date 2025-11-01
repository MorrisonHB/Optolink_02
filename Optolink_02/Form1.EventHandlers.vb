' Partielle Klasse für MQTT und Raspberry Pi Event-Handler
Partial Public Class Form1

    ' ============================================================
    ' MQTT TAB - EVENT HANDLERS
    ' ============================================================

    ''' <summary>
    ''' MQTT Connect Button
    ''' </summary>
    Private Async Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        Try
            Await Services.MqttServiceFacade.ConnectAndIdentifyAsync(Me)
            btnAbfrage.Enabled = True
        Catch ex As Exception
            Dim unused = MessageBox.Show($"MQTT-Verbindung fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            AppendLog($"[FEHLER] MQTT-Verbindung: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' MQTT Disconnect Button
    ''' </summary>
    Private Async Sub btnDisconnect_Click(sender As Object, e As EventArgs) Handles btnDisconnect.Click
        Try
            Await Services.MqttServiceFacade.DisconnectAsync(Me)
        Catch ex As Exception
            Dim unused = MessageBox.Show($"MQTT-Trennung fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            AppendLog($"[FEHLER] MQTT-Trennung: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' MQTT Send Button - Sendet Nachricht und wartet auf Antwort
    ''' </summary>
    Private Async Sub btnSend_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        Try
            If Not _mqtt.IsConnected Then
                Dim unused2 = MessageBox.Show("Nicht verbunden. Bitte zuerst MQTT-Verbindung herstellen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim command = txtSend.Text.Trim()
            If String.IsNullOrWhiteSpace(command) Then
                Dim unused1 = MessageBox.Show("Bitte einen Befehl eingeben.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim sendTopic = If(cmbTopicSend?.SelectedItem?.ToString(), "viessmann/cmnd")
            Dim receiveTopic = If(cmbTopicReceive?.SelectedItem?.ToString(), "viessmann/status")

            AppendLog($"? Sende: {command}")
            txtReceive.Text = "? Warte auf Antwort..."

            Dim response = Await _mqtt.SendCommandAndWaitForResponseAsync(command, sendTopic, receiveTopic, 5000)

            If Not String.IsNullOrWhiteSpace(response) Then
                txtReceive.Text = response
                AppendLog($"? Empfangen: {response}")
            Else
                txtReceive.Text = "?? Timeout - Keine Antwort erhalten"
                AppendLog("?? Timeout")
            End If

        Catch ex As Exception
            txtReceive.Text = $"? Fehler: {ex.Message}"
            Dim unused = MessageBox.Show($"Fehler beim Senden: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            AppendLog($"[FEHLER] Senden: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' MQTT Identify Button - Identifiziert das angeschlossene Gerät
    ''' </summary>
    Private Async Sub btnIdentify_Click(sender As Object, e As EventArgs) Handles btnIdentify.Click
        Try
            If Not _mqtt.IsConnected Then
                Dim unused1 = MessageBox.Show("Nicht verbunden. Bitte zuerst MQTT-Verbindung herstellen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            AppendLog("Starte Geräte-Identifikation...")
            Await IdentifyDeviceAndFilterComboBoxAsync()

        Catch ex As Exception
            Dim unused = MessageBox.Show($"Geräte-Identifikation fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            AppendLog($"[FEHLER] Identifikation: {ex.Message}")
        End Try
    End Sub

    ' ============================================================
    ' RASPBERRY PI TAB - EVENT HANDLERS
    ' ============================================================

    ''' <summary>
    ''' Pi Connect Button - Verbindet mit Raspberry Pi via SSH
    ''' </summary>
    Private Async Sub btnPiConnect_Click(sender As Object, e As EventArgs) Handles btnPiConnect.Click
        Try
            btnPiConnect.Enabled = False
            lblPiStatusLamp.ForeColor = Color.Orange
            txtPiDebug.AppendText($"[INFO] Verbinde mit {cboPiHost.Text}...{Environment.NewLine}")

            Dim host = cboPiHost.Text.Trim()
            Dim user = txtPiUser.Text.Trim()
            Dim password = txtPiPassword.Text

            If String.IsNullOrWhiteSpace(host) OrElse String.IsNullOrWhiteSpace(user) Then
                Dim unused2 = MessageBox.Show("Bitte Host und Benutzername eingeben.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                btnPiConnect.Enabled = True
                Return
            End If

            Await Task.Run(Sub() _ssh.Connect(host, user, password))

            If _ssh.IsConnected Then
                lblPiStatusLamp.ForeColor = Color.Green
                btnPiDisconnect.Enabled = True
                btnPiQuery.Enabled = True
                txtPiDebug.AppendText($"[OK] Verbunden mit {host}{Environment.NewLine}")

                ' Automatisch MQTT-Topics entdecken
                Await DiscoverMqttTopicsAsync()
            Else
                lblPiStatusLamp.ForeColor = Color.Red
                btnPiConnect.Enabled = True
                Dim unused1 = MessageBox.Show("Verbindung fehlgeschlagen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            lblPiStatusLamp.ForeColor = Color.Red
            btnPiConnect.Enabled = True
            Dim unused = MessageBox.Show($"SSH-Verbindung fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPiDebug.AppendText($"[FEHLER] {ex.Message}{Environment.NewLine}")
        End Try
    End Sub

    ''' <summary>
    ''' Pi Disconnect Button - Trennt SSH-Verbindung
    ''' </summary>
    Private Sub btnPiDisconnect_Click(sender As Object, e As EventArgs) Handles btnPiDisconnect.Click
        Try
            If _ssh.IsConnected Then
                _ssh.Disconnect()
                txtPiDebug.AppendText($"[INFO] SSH-Verbindung getrennt{Environment.NewLine}")
            End If

            lblPiStatusLamp.ForeColor = Color.Red
            btnPiConnect.Enabled = True
            btnPiDisconnect.Enabled = False
            btnPiQuery.Enabled = False
            lvPiServices.Items.Clear()

        Catch ex As Exception
            Dim unused = MessageBox.Show($"Fehler beim Trennen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPiDebug.AppendText($"[FEHLER] {ex.Message}{Environment.NewLine}")
        End Try
    End Sub

    ''' <summary>
    ''' Pi Query Button - Fragt Dienste-Status ab
    ''' </summary>
    Private Async Sub btnPiQuery_Click(sender As Object, e As EventArgs) Handles btnPiQuery.Click
        Try
            If Not _ssh.IsConnected Then
                Dim unused1 = MessageBox.Show("Nicht verbunden. Bitte zuerst SSH-Verbindung herstellen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            btnPiQuery.Enabled = False
            lvPiServices.Items.Clear()
            txtPiDebug.AppendText($"[INFO] Frage Dienste ab...{Environment.NewLine}")

            ' Führe Abfrage-Skript aus
            Dim command = String.Join(" ", RPi_CommandValue)
            Dim output = Await Task.Run(Function() _ssh.RunCommand(command, 10000))

            txtPiDebug.AppendText($"[DEBUG] Ausgabe:{Environment.NewLine}{output}{Environment.NewLine}")

            ' Parse Ausgabe und fülle ListView
            ParseServicesOutput(output)

        Catch ex As Exception
            Dim unused = MessageBox.Show($"Dienste-Abfrage fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPiDebug.AppendText($"[FEHLER] {ex.Message}{Environment.NewLine}")
        Finally
            btnPiQuery.Enabled = True
        End Try
    End Sub

    ''' <summary>
    ''' Parst die Service-Ausgabe und füllt die ListView
    ''' </summary>
    Private Sub ParseServicesOutput(output As String)
        Try
            Dim lines = output.Split(New String() {Environment.NewLine, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim currentSection = "Unknown"

            For Each line In lines
                line = line.Trim()

                ' Erkenne Abschnitts-Header
                If line.StartsWith("===") Then
                    If line.Contains("Running/failed") Then
                        currentSection = "Running Services"
                    ElseIf line.Contains("Installed service") Then
                        currentSection = "Installed Services"
                    ElseIf line.Contains("Active state") Then
                        currentSection = "Active State"
                    ElseIf line.Contains("Mosquitto") Then
                        currentSection = "Mosquitto"
                    End If
                    Continue For
                End If

                ' Überspringe Leerzeilen und Header
                If String.IsNullOrWhiteSpace(line) OrElse line.StartsWith("UNIT") Then Continue For

                ' Parse Service-Zeilen
                Dim parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                If parts.Length >= 2 Then
                    Dim serviceName = parts(0).Trim()
                    Dim status = parts(1).Trim()

                    ' Nur relevante Services anzeigen
                    If serviceName.Contains("vito") OrElse serviceName.Contains("optolink") OrElse
              serviceName.Contains("vcontrol") OrElse serviceName.Contains("mosquitto") OrElse
                serviceName.Contains("mqtt") Then

                        Dim item As New ListViewItem(serviceName)
                        Dim unused2 = item.SubItems.Add(status)
                        Dim unused1 = item.SubItems.Add(currentSection)

                        ' Färbe Status
                        If status.Contains("active") OrElse status.Contains("running") Then
                            item.ForeColor = Color.Green
                        ElseIf status.Contains("failed") OrElse status.Contains("inactive") Then
                            item.ForeColor = Color.Red
                        End If

                        Dim unused = lvPiServices.Items.Add(item)
                    End If
                End If
            Next

            txtPiDebug.AppendText($"[OK] {lvPiServices.Items.Count} Dienste gefunden{Environment.NewLine}")

        Catch ex As Exception
            txtPiDebug.AppendText($"[FEHLER] Parse: {ex.Message}{Environment.NewLine}")
        End Try
    End Sub

    ''' <summary>
    ''' Kontextmenü: Dienst starten
    ''' </summary>
    Private Async Sub mnuPiStart_Click(sender As Object, e As EventArgs) Handles mnuPiStart.Click
        Await ExecuteServiceCommand("start")
    End Sub

    ''' <summary>
    ''' Kontextmenü: Dienst stoppen
    ''' </summary>
    Private Async Sub mnuPiStop_Click(sender As Object, e As EventArgs) Handles mnuPiStop.Click
        Await ExecuteServiceCommand("stop")
    End Sub

    ''' <summary>
    ''' Kontextmenü: Dienst übernehmen (adoptieren)
    ''' </summary>
    Private Async Sub mnuPiAdopt_Click(sender As Object, e As EventArgs) Handles mnuPiAdopt.Click
        ' TODO: Implementiere Service-Übernahme-Logik
        Dim unused = MessageBox.Show("Dienst-Übernahme noch nicht implementiert.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Await Task.CompletedTask
    End Sub

    ''' <summary>
    ''' Führt einen Service-Befehl aus (start/stop/restart)
    ''' </summary>
    Private Async Function ExecuteServiceCommand(action As String) As Task
        Try
            If lvPiServices.SelectedItems.Count = 0 Then Return
            If Not _ssh.IsConnected Then
                Dim unused1 = MessageBox.Show("SSH nicht verbunden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim serviceName = lvPiServices.SelectedItems(0).Text
            Dim command = $"sudo systemctl {action} {serviceName}"

            txtPiDebug.AppendText($"[INFO] Führe aus: {command}{Environment.NewLine}")

            Dim output = Await Task.Run(Function() _ssh.RunCommand(command, 5000))

            txtPiDebug.AppendText($"[OK] {action} für {serviceName} ausgeführt{Environment.NewLine}")
            If Not String.IsNullOrWhiteSpace(output) Then
                txtPiDebug.AppendText($"[OUTPUT] {output}{Environment.NewLine}")
            End If

            ' Aktualisiere Liste
            Await Task.Delay(500) ' Kurze Pause für Service-Statusänderung
            btnPiQuery.PerformClick()

        Catch ex As Exception
            Dim unused = MessageBox.Show($"Fehler bei Service-Befehl: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPiDebug.AppendText($"[FEHLER] {ex.Message}{Environment.NewLine}")
        End Try
    End Function

End Class
