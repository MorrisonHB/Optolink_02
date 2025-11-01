Imports Optolink_02.Domain

''' <summary>
''' Dialog zur manuellen Parameter-Abfrage mit editierbaren Werten
''' </summary>
Public Class ParameterQueryDialog
    Inherits Form

    Private ReadOnly _parameter As ParameterNode
    Private ReadOnly _mqttService As MqttService
    Private ReadOnly _sendTopic As String
    Private ReadOnly _receiveTopic As String

    ' Controls
    Private WithEvents txtParameterName As TextBox
    Private WithEvents txtAddress As TextBox
    Private WithEvents numByteLength As NumericUpDown
    Private WithEvents cmbConversion As ComboBox
    Private WithEvents cmbDataType As ComboBox
    Private WithEvents txtStepping As TextBox
    Private WithEvents txtResult As TextBox
    Private WithEvents btnQuery As Button
    Private WithEvents btnCancel As Button
    Private lblParameterName As Label
    Private lblAddress As Label
    Private lblByteLength As Label
    Private lblConversion As Label
    Private lblDataType As Label
    Private lblStepping As Label
    Private lblResult As Label
    Private prgQuery As ProgressBar
    Private Shared ReadOnly items As String() = New String() {
            "NoConversion", "Div2", "Div10", "Div100", "Div1000",
            "Mult2", "Mult5", "Mult10", "Mult100", "MultOffset",
            "HexByte2DecimalByte", "IPAddress", "DateBCD", "DateTimeBCD",
            "Time53", "Sec2Minute", "Sec2Hour", "Sec2Day", "Sec2Week",
            "RotateBytes", "Phone2BCD"
        }
    Private Shared ReadOnly itemsArray As String() = New String() {
      "Int", "Float", "Double", "Byte", "UInt", "Int16", "Int32", "Long"
 }

    Public Sub New(parameter As ParameterNode, mqttService As MqttService, sendTopic As String, receiveTopic As String)
        _parameter = parameter
        _mqttService = mqttService
        _sendTopic = If(sendTopic, "viessmann/cmnd")
        _receiveTopic = If(receiveTopic, "viessmann/status")

        InitializeComponent()
        LoadParameterData()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Parameter Abfragen"
        Me.Size = New Size(500, 450)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Labels
        lblParameterName = New Label With {
            .Text = "Parameter:",
            .Location = New Point(20, 20),
            .AutoSize = True}

        lblAddress = New Label With {
            .Text = "Adresse:",
            .Location = New Point(20, 55),
            .AutoSize = True}

        lblByteLength = New Label With {
            .Text = "Byte-Länge:",
            .Location = New Point(20, 90),
            .AutoSize = True}

        lblConversion = New Label With {
            .Text = "Conversion:",
            .Location = New Point(20, 125),
            .AutoSize = True}

        lblDataType = New Label With {
            .Text = "DataType:",
            .Location = New Point(20, 160),
            .AutoSize = True}

        lblStepping = New Label With {
            .Text = "Stepping:",
            .Location = New Point(20, 195),
            .AutoSize = True}

        lblResult = New Label With {
            .Text = "Ergebnis:",
            .Location = New Point(20, 230),
            .AutoSize = True}

        ' TextBoxes
        txtParameterName = New TextBox With {
            .Location = New Point(120, 17),
            .Size = New Size(340, 23),
            .ReadOnly = True,
            .BackColor = SystemColors.Control}

        txtAddress = New TextBox With {
            .Location = New Point(120, 52),
            .Size = New Size(150, 23)}

        numByteLength = New NumericUpDown With {
            .Location = New Point(120, 87),
            .Size = New Size(80, 23),
            .Minimum = 1,
            .Maximum = 32,
            .Value = 2}

        cmbConversion = New ComboBox With {
            .Location = New Point(120, 122),
            .Size = New Size(200, 23),
            .DropDownStyle = ComboBoxStyle.DropDown}

        cmbDataType = New ComboBox With {
            .Location = New Point(120, 157),
            .Size = New Size(150, 23),
            .DropDownStyle = ComboBoxStyle.DropDown}

        ' Conversion-Typen hinzufügen
        cmbConversion.Items.AddRange(items)

        ' DataType-Typen hinzufügen
        cmbDataType.Items.AddRange(itemsArray)

        txtStepping = New TextBox With {
            .Location = New Point(120, 192),
            .Size = New Size(150, 23)}

        txtResult = New TextBox With {
            .Location = New Point(120, 227),
            .Size = New Size(340, 80),
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical}

        prgQuery = New ProgressBar With {
            .Location = New Point(20, 320),
            .Size = New Size(440, 10),
            .Style = ProgressBarStyle.Marquee,
            .Visible = False}

        ' Buttons
        btnQuery = New Button With {
            .Text = "Abfragen",
            .Location = New Point(280, 340),
            .Size = New Size(90, 30),
            .DialogResult = DialogResult.None}

        btnCancel = New Button With {
            .Text = "Abbrechen",
            .Location = New Point(380, 340),
            .Size = New Size(90, 30),
            .DialogResult = DialogResult.Cancel}

        ' Controls hinzufügen
        Me.Controls.AddRange(New Control() {lblParameterName, txtParameterName, lblAddress,
                             txtAddress, lblByteLength, numByteLength, lblConversion, cmbConversion,
                             lblDataType, cmbDataType, lblStepping, txtStepping,
                             lblResult, txtResult, prgQuery, btnQuery, btnCancel})

        Me.AcceptButton = btnQuery
        Me.CancelButton = btnCancel
    End Sub

    Private Sub LoadParameterData()
        If _parameter Is Nothing Then Return

        txtParameterName.Text = _parameter.ParameterName
        txtAddress.Text = _parameter.Address
        numByteLength.Value = If(_parameter.ByteLength > 0, _parameter.ByteLength, 2)

        ' Conversion: Setze den Wert oder lasse die ComboBox leer für manuelle Eingabe
        If Not String.IsNullOrWhiteSpace(_parameter.Conversion) Then
            cmbConversion.Text = _parameter.Conversion
        Else
            cmbConversion.Text = ""
        End If

        ' DataType: Setze den Wert wenn vorhanden
        If Not String.IsNullOrWhiteSpace(_parameter.DataType) Then
            cmbDataType.Text = _parameter.DataType
        Else
            cmbDataType.Text = ""
        End If

        ' Stepping: Zeige den Wert an, aber lasse Bearbeitung zu
        txtStepping.Text = If(String.IsNullOrWhiteSpace(_parameter.Stepping), "", _parameter.Stepping)

        txtResult.Text = $"Aktueller Wert: {_parameter.CurrentValue}"
    End Sub

    Private Async Sub btnQuery_Click(sender As Object, e As EventArgs) Handles btnQuery.Click
        Try
            If Not _mqttService.IsConnected Then
                Dim unused3 = MessageBox.Show("MQTT nicht verbunden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim address = txtAddress.Text.Trim()
            If String.IsNullOrWhiteSpace(address) Then
                Dim unused2 = MessageBox.Show("Bitte eine gültige Adresse eingeben.",
                                              "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Dim unused1 = txtAddress.Focus()
                Return
            End If

            ' UI während Abfrage sperren
            btnQuery.Enabled = False
            btnCancel.Enabled = False
            prgQuery.Visible = True
            txtResult.Text = "⏳ Abfrage läuft..."

            ' MQTT-Command - WICHTIG: Verwende MqttCommandGenerator
            Dim byteLen = CInt(numByteLength.Value)
            Dim conversion = cmbConversion.Text.Trim()
            Dim dataType = cmbDataType.Text.Trim()

            ' Generiere Kommando mit MqttCommandGenerator (inkl. scale und signed)
            Dim command = Services.MqttCommandGenerator.GenerateReadCommand(
                address,
                byteLen.ToString(),
                conversion,
                dataType)

            ' Fallback wenn Generator nichts zurückgibt
            If String.IsNullOrWhiteSpace(command) Then
                command = $"read;{address};{byteLen}"
            End If

            Debug.WriteLine($"[PARAM-QUERY] MQTT Command: {command}")

            Try
                Dim response =
                    Await _mqttService.SendCommandAndWaitForResponseAsync(command,
                                                                          _sendTopic,
                                                                          _receiveTopic,
                                                                          5000)

                If Not String.IsNullOrWhiteSpace(response) Then
                    Dim parts = response.Split(";"c)

                    If parts.Length >= 3 AndAlso parts(0) = "1" Then
                        Dim rawValue = parts(2).Trim()

                        ' WICHTIG: Optolink-Splitter liefert bereits konvertierte Werte!
                        ' Keine weitere Konvertierung nötig - direkt verwenden
                        Dim convertedValue = rawValue

                        Debug.WriteLine($"[PARAM-QUERY] Response value: {convertedValue}")

                        ' Ergebnis anzeigen
                        txtResult.Text = $"✓ Abfrage erfolgreich{Environment.NewLine}" &
                            $"Command: {command}{Environment.NewLine}" &
                            $"Response: {response}{Environment.NewLine}" &
                            $"Value: {convertedValue}"

                        ' Parameter aktualisieren
                        _parameter.CurrentValue = convertedValue
                        _parameter.Address = address
                        _parameter.ByteLength = byteLen
                        _parameter.Conversion = conversion
                        _parameter.DataType = dataType
                        _parameter.Stepping = txtStepping.Text.Trim()

                        ' Dialog kann geschlossen werden
                        Me.DialogResult = DialogResult.OK
                    Else
                        ' Fehler-Response
                        txtResult.Text = $"? Fehler-Response{Environment.NewLine}" &
                  $"Command: {command}{Environment.NewLine}" &
                             $"Response: {response}"
                    End If
                Else
                    txtResult.Text = $"?? Timeout{Environment.NewLine}" &
                 $"Command: {command}{Environment.NewLine}" &
                    $"Keine Antwort nach 5 Sekunden"
                End If

            Catch ex As TimeoutException
                txtResult.Text = $"?? Timeout{Environment.NewLine}" &
             $"Command: {command}{Environment.NewLine}" &
                 $"Fehler: {ex.Message}"
            Catch ex As Exception
                txtResult.Text = $"? Fehler{Environment.NewLine}" &
         $"Command: {command}{Environment.NewLine}" &
          $"Fehler: {ex.Message}"
            End Try

        Catch ex As Exception
            Dim unused = MessageBox.Show($"Fehler bei Abfrage: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            ' UI wieder freigeben
            btnQuery.Enabled = True
            btnCancel.Enabled = True
            prgQuery.Visible = False
        End Try
    End Sub
End Class