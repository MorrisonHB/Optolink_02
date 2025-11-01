Namespace Services
    Public NotInheritable Class UiDiscoveryService
        Private Sub New()
        End Sub

        Public Shared Async Function StartAutoDiscoveryAsync(form As Form1) As Task
            ArgumentNullException.ThrowIfNull(form)
            Try
                Dim mdnsPi = Await PiDiscovery.ResolveMdnsAsync("raspberrypi.local")
                If mdnsPi IsNot Nothing AndAlso mdnsPi.Length > 0 Then
                    form.Invoke(Sub()
                                    SafeSetComboItems(form.cboPiHost,
                                                      mdnsPi.Select(Function(a)
                                                                        Return a.ToString()
                                                                    End Function).ToList(),
                                                      selectFirst:=True)
                                End Sub)
                End If

                Dim piScanTask = PiDiscovery.FindPisOnLocalNetworkAsync(timeoutMs:=250,
                                                                        maxConcurrency:=64,
                                                                        maxHostsPerSubnet:=1024)
                Dim mqttScanTask = PiDiscovery.FindMqttHostsOnLocalNetworkAsync(timeoutMs:=200,
                                                                                maxConcurrency:=64,
                                                                                maxHostsPerSubnet:=1024)

                Dim piHosts = Await piScanTask
                If (piHosts IsNot Nothing) AndAlso piHosts.Count > 0 Then
                    Dim ordered = piHosts.OrderBy(Function(h)
                                                      Return Not h.IsRaspberryPi
                                                  End Function).ThenBy(Function(h)
                                                                           Return h.IP.ToString()
                                                                       End Function).ToList()
                    Dim piList = ordered.Select(Function(h)
                                                    Return h.IP.ToString()
                                                End Function).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    form.Invoke(Sub()
                                    SafeSetComboItems(form.cboPiHost,
                                                      piList,
                                                      selectFirst:=form.cboPiHost.Items.Count = 0)
                                End Sub)
                End If

                Dim mqttHosts = Await mqttScanTask
                If (mqttHosts IsNot Nothing) AndAlso mqttHosts.Count > 0 Then
                    Dim mqttList = mqttHosts.Select(Function(ip)
                                                        Return ip.ToString()
                                                    End Function).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    form.Invoke(Sub()
                                    SafeSetComboItems(form.cboIPAddress,
                                                      mqttList,
                                                      selectFirst:=True)
                                End Sub)
                End If
            Catch ex As Exception
                Debug.WriteLine("Discovery fehlgeschlagen: " & ex.Message)
            End Try
        End Function

        Private Shared Sub SafeSetComboItems(cmb As ComboBox, items As List(Of String), selectFirst As Boolean)
            If cmb Is Nothing OrElse items Is Nothing OrElse items.Count = 0 Then Return

            ' Elemente ergänzen ohne Duplikate
            For Each s In items
                Dim exists As Boolean = False
                For Each it In cmb.Items
                    If String.Equals(Convert.ToString(it), s, StringComparison.OrdinalIgnoreCase) Then
                        exists = True
                        Exit For
                    End If
                Next
                If Not exists Then
                    Dim unused = cmb.Items.Add(s)
                End If
            Next

            If selectFirst AndAlso cmb.Items.Count > 0 Then
                cmb.Text = Convert.ToString(cmb.Items(0))
            End If
        End Sub
    End Class
End Namespace