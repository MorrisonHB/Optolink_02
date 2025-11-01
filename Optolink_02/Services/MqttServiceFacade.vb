Namespace Services
    ' Fassade: Verbindet MQTT, identifiziert Gerät und filtert Combobox
    Public NotInheritable Class MqttServiceFacade
        Private Sub New()
        End Sub

        Public Shared Async Function ConnectAndIdentifyAsync(form As Form1) As Task
            ArgumentNullException.ThrowIfNull(form)

            form.Invoke(Sub()
                            form.btnConnect.Enabled = False
                            form.btnDisconnect.Enabled = False
                        End Sub)

            ' Handler neu binden im UI-Thread
            form.Invoke(Sub()
                            RemoveHandler form._mqtt.MessageReceived, AddressOf form.Mqtt_MessageReceived
                            RemoveHandler form._mqtt.Log, AddressOf form.Mqtt_Log
                            AddHandler form._mqtt.MessageReceived, AddressOf form.Mqtt_MessageReceived
                            AddHandler form._mqtt.Log, AddressOf form.Mqtt_Log
                        End Sub)

            Await form._mqtt.ConnectAsync(form.cboIPAddress.Text,
                                          Convert.ToInt32(form.txtPort.Text),
                                          form.cmbTopicReceive.Text)
            form.Invoke(Sub() form.btnDisconnect.Enabled = True)

            ' Automatische Geräteidentifikation und Filterung
            form.AppendLog("Identifiziere Gerät...")
            Await form.IdentifyDeviceAndFilterComboBoxAsync()
        End Function

        Public Shared Async Function DisconnectAsync(form As Form1) As Task
            ArgumentNullException.ThrowIfNull(form)
            form.Invoke(Sub() form.btnDisconnect.Enabled = False)
            If (form._mqtt IsNot Nothing) AndAlso form._mqtt.IsConnected Then
                Await form._mqtt.DisconnectAsync()
            End If
            form.Invoke(Sub()
                            form.btnConnect.Enabled = True
                            form.btnAbfrage.Enabled = False
                        End Sub)
            form.RestoreAllDevices()
        End Function
    End Class
End Namespace