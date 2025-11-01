Namespace Services
    Public NotInheritable Class DeviceService
        Private Sub New()
        End Sub

        ' Lädt die Geräte und weist sie der ComboBox zu
        Public Shared Sub LoadDevices(cmb As ComboBox)
            ArgumentNullException.ThrowIfNull(cmb)
            Try
                Dim devices = Parsers.EcnDeviceParser.GetDevices()
                cmb.DisplayMember = "DeviceName"
                cmb.ValueMember = "DeviceName"
                cmb.DataSource = devices
                cmb.Tag = devices ' Originale Liste merken
            Catch ex As Exception
                Debug.WriteLine("Geräteliste konnte nicht geladen werden: " & ex.Message)
            End Try
        End Sub

        ' Stellt die Original-Liste wieder her (alle Geräte)
        Public Shared Sub RestoreAll(cmb As ComboBox)
            If cmb Is Nothing Then Return
            If cmb.Tag IsNot Nothing Then
                cmb.DataSource = cmb.Tag
            End If
        End Sub
    End Class
End Namespace