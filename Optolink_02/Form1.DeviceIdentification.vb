' Partielle Klasse für Geräte-Identifikation via MQTT
Partial Public Class Form1

    ''' <summary>
    ''' Identifiziert das Gerät über MQTT und filtert die Geräte-ComboBox
    ''' </summary>
    Friend Async Function IdentifyDeviceAndFilterComboBoxAsync() As Task
        Try
            If Not _mqtt.IsConnected Then
                AppendLog("[IDENTIFY] MQTT nicht verbunden")
                Return
            End If

            AppendLog("[IDENTIFY] Lese Geräte-Identität von 0xF8...")

            ' Hole Topics aus UI
            Dim sendTopic = If(cmbTopicSend?.SelectedItem?.ToString(), "viessmann/cmnd")
            Dim receiveTopic = If(cmbTopicReceive?.SelectedItem?.ToString(), "viessmann/status")

            ' Erstelle MQTT Reader
            Dim reader As New MqttDeviceReader(_mqtt, sendTopic, receiveTopic)

            ' Lese F8 (8 Bytes: Identification)
            Dim f8Bytes = Await reader.ReadBytesAsync(&HF8, 8, 5000)
            AppendLog($"[IDENTIFY] F8: {BitConverter.ToString(f8Bytes).Replace("-", " ")}")

            ' Lese F0 (1 Byte: ProtocolIdentifierOffset) - Optional
            Dim f0Byte As Byte? = Nothing
            Try
                Dim f0Bytes = Await reader.ReadBytesAsync(&HF0, 1, 3000)
                If f0Bytes IsNot Nothing AndAlso f0Bytes.Length >= 1 Then
                    f0Byte = f0Bytes(0)
                    AppendLog($"[IDENTIFY] F0: {f0Byte.Value:X2}")
                End If
            Catch ex As Exception
                AppendLog($"[IDENTIFY] F0 konnte nicht gelesen werden (optional): {ex.Message}")
            End Try

            ' Erstelle DeviceIdentity
            Dim identity As New DeviceIdentity With {
               .RawF8 = f8Bytes,
            .RawF0 = f0Byte
                      }

            AppendLog($"[IDENTIFY] Identification: {identity.IdentificationHex}")
            AppendLog($"[IDENTIFY] SW-Index: {identity.SWIndexHex}")
            AppendLog($"[IDENTIFY] HW-Extension: {identity.ExtensionHex2}")

            ' Verwende DeviceControlMap für erste Identifikation
            Dim matches = DeviceControlMap.Match(identity)
            If matches.Count > 0 Then
                AppendLog($"[IDENTIFY] DeviceControlMap-Treffer: {String.Join(", ", matches)}")
                _identifiedDevice = matches(0) ' Speichere ersten Treffer
            Else
                AppendLog("[IDENTIFY] Kein Treffer in DeviceControlMap")
            End If

            ' Lade XML-Geräteliste für detaillierte Identifikation
            Dim repo = Await Task.Run(Function() Services.EcnDataPointRepository.LoadDataPointTypes())
            If repo IsNot Nothing AndAlso repo.Items IsNot Nothing AndAlso repo.Items.Count > 0 Then
                Dim xmlMatches = DeviceIdentifier.MatchDevices(identity, repo)
                If xmlMatches.Count > 0 Then
                    AppendLog($"[IDENTIFY] XML-Treffer: {xmlMatches.Count} Gerät(e)")
                    For Each match In xmlMatches.Take(5) ' Zeige max. 5 Treffer
                        AppendLog($"  - {match.ID}: {match.Description}")
                    Next

                    ' Filtere ComboBox auf identifizierte Geräte
                    Invoke(Sub()
                               Dim currentSelection = cmbDevices.SelectedValue

                               ' Hole Original-Liste aus Tag
                               Dim allDevices = If(TryCast(cmbDevices.Tag,
                                   List(Of Parsers.DeviceInfo)),
                                   TryCast(cmbDevices.DataSource,
                                   List(Of Parsers.DeviceInfo)))

                               If allDevices IsNot Nothing Then
                                   ' WICHTIG: Nur EXAKTE Treffer verwenden!
                                   ' Erstelle Set von XML-IDs für exakten Match
                                   Dim matchingIds = xmlMatches.Select(Function(m) m.ID).ToHashSet()

                                   ' Filtere auf exakte Übereinstimmung (nicht Contains!)
                                   Dim filteredDevices = allDevices.Where(Function(d As Parsers.DeviceInfo)
                                                                              ' Exakter Match:
                                                                              ' DeviceName muss genau der ID entsprechen
                                                                              Return matchingIds.Contains(d.DeviceName)
                                                                          End Function).ToList()

                                   If filteredDevices.Count > 0 Then
                                       ' Setze gefilterte Liste als DataSource
                                       cmbDevices.DataSource = filteredDevices

                                       ' Wähle ersten Treffer oder behalte Auswahl
                                       If filteredDevices.Any(Function(d) d.DeviceName = currentSelection?.ToString()) Then
                                           cmbDevices.SelectedValue = currentSelection
                                       Else
                                           cmbDevices.SelectedIndex = 0
                                       End If

                                       AppendLog($"[IDENTIFY] ComboBox auf {filteredDevices.Count} Gerät(e) gefiltert")

                                       ' Warne bei mehreren Treffern
                                       If filteredDevices.Count > 1 Then
                                           AppendLog($"[IDENTIFY] ? WARNUNG: {filteredDevices.Count} Geräte gefunden - bitte spezifischstes auswählen!")
                                       End If

                                       btnAbfrage.Enabled = True
                                   Else
                                       ' Kein exakter Gerät gefunden - versuche DeviceControlMap-Treffer
                                       If matches.Count > 0 Then
                                           Dim matchDevices = allDevices.Where(Function(d As Parsers.DeviceInfo)
                                                                                   Return matches.Any(Function(m) d.DeviceName = m)
                                                                               End Function).ToList()
                                           If matchDevices.Count > 0 Then
                                               cmbDevices.DataSource = matchDevices
                                               cmbDevices.SelectedIndex = 0
                                               AppendLog($"[IDENTIFY] ComboBox auf {matchDevices.Count} Gerät(e) gefiltert (via DeviceControlMap)")

                                               If matchDevices.Count > 1 Then
                                                   AppendLog($"[IDENTIFY] ? WARNUNG: {matchDevices.Count} Geräte gefunden!")
                                               End If

                                               btnAbfrage.Enabled = True
                                           Else
                                               AppendLog("[IDENTIFY] ? Keine passenden Geräte in ComboBox gefunden")
                                           End If
                                       Else
                                           AppendLog("[IDENTIFY] ? Keine passenden Geräte in ComboBox gefunden")
                                       End If
                                   End If
                               End If
                           End Sub)
                Else
                    AppendLog("[IDENTIFY] Kein Treffer in XML-Datenbank")
                End If
            Else
                AppendLog("[IDENTIFY] ? XML-Datenbank konnte nicht geladen werden")
                AppendLog("[IDENTIFY] Tipp: Lege ecnDataPointType.xml im XML-Ordner ab")
            End If

            ' Zeige Zusammenfassung
            AppendLog($"[IDENTIFY] ? Identifikation abgeschlossen")

        Catch ex As Exception
            Debug.WriteLine($"[ERROR] IdentifyDeviceAndFilterComboBoxAsync: {ex.Message}")
            Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}")
            AppendLog($"[IDENTIFY] ? Fehler: {ex.Message}")
            Dim unused = MessageBox.Show($"Geräte-Identifikation fehlgeschlagen:{Environment.NewLine}{ex.Message}",
                     "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    ''' <summary>
    ''' Stellt die Original-Geräteliste wieder her
    ''' </summary>
    Friend Sub RestoreAllDevices()
        Try
            ' Lade komplette Geräteliste erneut
            Services.DeviceService.LoadDevices(cmbDevices)
            AppendLog("[RESTORE] Geräteliste wiederhergestellt")
            _identifiedDevice = Nothing
            btnAbfrage.Enabled = False
            Debug.WriteLine("[RESTORE] Geräteliste komplett wiederhergestellt")
        Catch ex As Exception
            Debug.WriteLine($"[ERROR] RestoreAllDevices: {ex.Message}")
            AppendLog($"[RESTORE] Fehler: {ex.Message}")
        End Try
    End Sub

End Class