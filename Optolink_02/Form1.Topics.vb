Imports System.Text.RegularExpressions

' Partielle Klasse für Topic-Management
Partial Public Class Form1

    Private Shared ReadOnly collection As String() = {"viessmann/cmnd", "viessmann/resp"}

    ''' <summary>
    ''' Erkennt MQTT-Topics automatisch vom Raspberry Pi
    ''' </summary>
    Private Async Function DiscoverMqttTopicsAsync() As Task
        If Not _ssh.IsConnected Then Return

        Try
            txtPiDebug.AppendText($"[INFO] Suche MQTT-Topics...{Environment.NewLine}")

            Dim topics As New List(Of String)

            ' STRATEGIE 2: Lese settings_ini.py direkt (PRIORITÄT!)
            Try
                txtPiDebug.AppendText($"[DEBUG] Suche in Config-Dateien...{Environment.NewLine}")

                ' Finde settings_ini.py Dateien gezielt
                Dim configCmd = "find /home -path '*/optolink*/settings_ini.py' 2>/dev/null | head -5 | xargs cat 2>/dev/null | grep -A1 -E 'mqtt_listen|mqtt_respond'"
                Dim configOutput = Await Task.Run(Function() _ssh.RunCommand(configCmd, 5000))

                txtPiDebug.AppendText($"[DEBUG] Config-Ausgabe:{Environment.NewLine}{configOutput}{Environment.NewLine}")

                ' Extrahiere Topics aus Config
                ' Pattern: mqtt_listen = "vitodens/cmnd" oder 'vitocal/resp'
                Dim configMatches = System.Text.RegularExpressions.Regex.Matches(configOutput, "mqtt_(?:listen|respond)\s*=\s*['""]([A-Za-z0-9_-]+/[A-Za-z0-9_-]+)['""]")
                For Each match As System.Text.RegularExpressions.Match In configMatches
                    Dim topic = match.Groups(1).Value.ToLower() ' Normalisiere zu lowercase

                    ' Nur Topics mit /cmnd, /resp oder /status
                    If (topic.EndsWith("/cmnd") OrElse topic.EndsWith("/resp") OrElse topic.EndsWith("/status")) AndAlso
   Not topics.Contains(topic) Then
                        topics.Add(topic)
                        txtPiDebug.AppendText($"[DEBUG] Gefunden in Config: {topic}{Environment.NewLine}")
                    End If
                Next
            Catch ex As Exception
                txtPiDebug.AppendText($"[DEBUG] Config-Suche Fehler: {ex.Message}{Environment.NewLine}")
            End Try

            ' STRATEGIE 1: Suche in laufenden Python-Prozessen (BACKUP - nur wenn Config leer)
            If topics.Count = 0 Then
                Try
                    txtPiDebug.AppendText($"[DEBUG] Suche in laufenden Prozessen (Backup)...{Environment.NewLine}")

                    ' Zeige komplette Kommandozeile (nicht gekürzt)
                    Dim psCmd = "ps auxww | grep -E 'python.*optolink|python.*vcontrol' | grep -v grep"
                    Dim psOutput = Await Task.Run(Function() _ssh.RunCommand(psCmd, 5000))

                    txtPiDebug.AppendText($"[DEBUG] Prozess-Ausgabe:{Environment.NewLine}{psOutput}{Environment.NewLine}")

                    ' Zusätzlich: Zeige /proc/PID/cmdline für Details
                    Dim pidCmd = "ps aux | grep -E 'python.*optolink' | grep -v grep | awk '{print $2}' | head -2 | while read pid; do echo ""[PID $pid]""; cat /proc/$pid/cmdline 2>/dev/null | tr '\0' ' '; echo; done"
                    Dim pidOutput = Await Task.Run(Function() _ssh.RunCommand(pidCmd, 5000))

                    If Not String.IsNullOrWhiteSpace(pidOutput) Then
                        txtPiDebug.AppendText($"[DEBUG] Prozess-Kommandozeilen:{Environment.NewLine}{pidOutput}{Environment.NewLine}")
                    End If

                    ' Extrahiere alle Wörter die wie Topics aussehen
                    Dim allMatches = System.Text.RegularExpressions.Regex.Matches(psOutput & " " & pidOutput, "([A-Za-z0-9_-]+/[A-Za-z0-9_-]+)")
                    For Each match As System.Text.RegularExpressions.Match In allMatches
                        Dim topic = match.Value.ToLower() ' Normalisiere zu lowercase
                        ' Prüfe ob es ein MQTT-Topic sein könnte
                        If (topic.Contains("cmnd") OrElse topic.Contains("resp") OrElse
          topic.Contains("status")) AndAlso Not topics.Contains(topic) Then
                            topics.Add(topic)
                            txtPiDebug.AppendText($"[DEBUG] Gefunden in Prozess: {topic}{Environment.NewLine}")
                        End If
                    Next
                Catch ex As Exception
                    txtPiDebug.AppendText($"[DEBUG] Prozess-Suche Fehler: {ex.Message}{Environment.NewLine}")
                End Try
            End If

            ' STRATEGIE 3: Standardisiere gefundene Topics
            Dim validTopics As New List(Of String)
            For Each topic In topics
                ' Normalisiere: /status → /resp, Großbuchstaben → Klein
                Dim normalized = topic.ToLower().Replace("/status", "/resp")

                ' Nur Topics die auf /cmnd oder /resp enden
                If normalized.EndsWith("/cmnd") OrElse normalized.EndsWith("/resp") Then
                    ' WICHTIG: Ignoriere "vito/cmnd" und "vito/resp" (zu generisch, kein spezifisches Gerät)
                    ' Akzeptiere nur: vitodens, vitocal, vitocrossal, etc.
                    Dim baseName = normalized.Split("/"c)(0)
                    If baseName = "vito" Then
                        txtPiDebug.AppendText($"[DEBUG] Ignoriere generisches Topic: {normalized}{Environment.NewLine}")
                        Continue For
                    End If

                    If Not validTopics.Contains(normalized) Then
                        validTopics.Add(normalized)
                    End If
                End If
            Next



            ' Fallback: Standard-Topics
            If validTopics.Count = 0 Then
                validTopics.AddRange(Collection)
                txtPiDebug.AppendText($"[INFO] Keine Topics gefunden, verwende Standard-Topics{Environment.NewLine}")
            Else
                txtPiDebug.AppendText($"[OK] {validTopics.Count} MQTT-Topic(s) gefunden: {String.Join(", ", validTopics)}{Environment.NewLine}")
            End If

            ' Fülle ComboBoxen
            UpdateTopicComboBoxes(validTopics)

        Catch ex As Exception
            txtPiDebug.AppendText($"[FEHLER] Topic-Discovery: {ex.Message}{Environment.NewLine}")
            Debug.WriteLine($"[ERROR] DiscoverMqttTopicsAsync: {ex.Message}")

            ' Fallback bei totalem Fehler
            UpdateTopicComboBoxes(New List(Of String) From {"viessmann/cmnd", "viessmann/resp"})
        End Try
    End Function

    ''' <summary>
    ''' Aktualisiert Topic-ComboBoxen (Thread-safe)
    ''' </summary>
    Private Sub UpdateTopicComboBoxes(topics As List(Of String))
        Me.Invoke(Sub()
                      updatingTopics = True

                      cmbTopicSend.Items.Clear()
                      cmbTopicReceive.Items.Clear()

                      ' Nur gültige Topics hinzufügen (müssen /cmnd oder /resp enthalten)
                      For Each topic In topics
                          If topic.EndsWith("/cmnd") AndAlso Not cmbTopicSend.Items.Contains(topic) Then
                              Dim unused3 = cmbTopicSend.Items.Add(topic)
                          End If
                          If topic.EndsWith("/resp") AndAlso Not cmbTopicReceive.Items.Contains(topic) Then
                              Dim unused2 = cmbTopicReceive.Items.Add(topic)
                          End If
                      Next

                      ' Wenn nur /cmnd vorhanden, leite /resp ab
                      If cmbTopicSend.Items.Count > 0 AndAlso cmbTopicReceive.Items.Count = 0 Then
                          Dim baseTopic = cmbTopicSend.Items(0).ToString().Replace("/cmnd", "")
                          Dim derivedResp = baseTopic & "/resp"
                          Dim unused1 = cmbTopicReceive.Items.Add(derivedResp)
                      End If

                      ' Wenn nur /resp vorhanden, leite /cmnd ab
                      If cmbTopicReceive.Items.Count > 0 AndAlso cmbTopicSend.Items.Count = 0 Then
                          Dim baseTopic = cmbTopicReceive.Items(0).ToString().Replace("/resp", "")
                          Dim derivedCmnd = baseTopic & "/cmnd"
                          Dim unused = cmbTopicSend.Items.Add(derivedCmnd)
                      End If

                      ' Wähle erste Topics
                      If cmbTopicSend.Items.Count > 0 Then cmbTopicSend.SelectedIndex = 0
                      If cmbTopicReceive.Items.Count > 0 Then cmbTopicReceive.SelectedIndex = 0

                      updatingTopics = False

                      txtPiDebug.AppendText($"[OK] Topics: Send={cmbTopicSend.Text}, Receive={cmbTopicReceive.Text}{Environment.NewLine}")
                  End Sub)
    End Sub

    ''' <summary>
    ''' Doppelklick auf lvPiServices: Topics übernehmen
    ''' </summary>
    Private Sub lvPiServices_DoubleClick(sender As Object, e As EventArgs) Handles lvPiServices.DoubleClick
        If lvPiServices.SelectedItems.Count = 0 Then Return

        Dim selectedText = lvPiServices.SelectedItems(0).Text

        ' Extrahiere nur gültige Topics (enden mit /cmnd oder /resp)
        Dim topicPattern = New Regex("([a-z0-9_-]+/(?:cmnd|resp))")
        Dim matches = topicPattern.Matches(selectedText)

        If matches.Count = 0 Then
            txtPiDebug.AppendText($"[INFO] Keine gültigen Topics in Service gefunden{Environment.NewLine}")
            Return
        End If

        Dim foundTopics As New List(Of String)
        For Each match As Match In matches
            Dim topic = match.Value
            ' Nur Topics die GENAU auf /cmnd oder /resp enden
            If topic.EndsWith("/cmnd") OrElse topic.EndsWith("/resp") Then
                If Not foundTopics.Contains(topic) Then foundTopics.Add(topic)
            End If
        Next

        If foundTopics.Count = 0 Then
            txtPiDebug.AppendText($"[INFO] Keine gültigen Topics gefunden (müssen /cmnd oder /resp enden){Environment.NewLine}")
            Return
        End If

        UpdateTopicComboBoxes(foundTopics)
        txtPiDebug.AppendText($"[OK] {foundTopics.Count} Topic(s) übernommen{Environment.NewLine}")
    End Sub

    ''' <summary>
    ''' Synchronisiert Send ↔ Receive (/cmnd ↔ /resp)
    ''' </summary>
    Private Sub cmbTopicSend_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbTopicSend.SelectedIndexChanged
        If updatingTopics Then Return

        Dim sendTopic = cmbTopicSend.Text
        If String.IsNullOrWhiteSpace(sendTopic) OrElse Not sendTopic.EndsWith("/cmnd") Then Return

        Dim baseTopic = sendTopic.Replace("/cmnd", "")
        Dim receiveTopic = baseTopic & "/resp"

        updatingTopics = True
        If cmbTopicReceive.Items.Contains(receiveTopic) Then
            cmbTopicReceive.SelectedItem = receiveTopic
        Else
            Dim unused = cmbTopicReceive.Items.Add(receiveTopic)
            cmbTopicReceive.SelectedItem = receiveTopic
        End If
        updatingTopics = False

        Debug.WriteLine($"[SYNC] Send='{sendTopic}' → Receive='{receiveTopic}'")
    End Sub

    ''' <summary>
    ''' Synchronisiert Receive ↔ Send (/resp ↔ /cmnd)
    ''' </summary>
    Private Sub cmbTopicReceive_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbTopicReceive.SelectedIndexChanged
        If updatingTopics Then Return

        Dim receiveTopic = cmbTopicReceive.Text
        If String.IsNullOrWhiteSpace(receiveTopic) OrElse Not receiveTopic.EndsWith("/resp") Then Return

        Dim baseTopic = receiveTopic.Replace("/resp", "")
        Dim sendTopic = baseTopic & "/cmnd"

        updatingTopics = True
        If cmbTopicSend.Items.Contains(sendTopic) Then
            cmbTopicSend.SelectedItem = sendTopic
        Else
            Dim unused = cmbTopicSend.Items.Add(sendTopic)
            cmbTopicSend.SelectedItem = sendTopic
        End If
        updatingTopics = False

        Debug.WriteLine($"[SYNC] Receive='{receiveTopic}' → Send='{sendTopic}'")
    End Sub
End Class
