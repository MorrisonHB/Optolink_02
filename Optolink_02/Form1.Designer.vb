<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer. 
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        tabMain = New TabControl()
        tabPi = New TabPage()
        lblPiHost = New Label()
        cboPiHost = New ComboBox()
        lblPiUser = New Label()
        txtPiUser = New TextBox()
        lblPiPassword = New Label()
        txtPiPassword = New TextBox()
        lblPiStatusLamp = New Label()
        btnPiConnect = New Button()
        btnPiDisconnect = New Button()
        btnPiQuery = New Button()
        lblPiOutput = New Label()
        lvPiServices = New ListView()
        colService = New ColumnHeader()
        colStatus = New ColumnHeader()
        colSection = New ColumnHeader()
        cmsPiServices = New ContextMenuStrip(components)
        mnuPiStart = New ToolStripMenuItem()
        mnuPiStop = New ToolStripMenuItem()
        mnuPiAdopt = New ToolStripMenuItem()
        lblPiDebug = New Label()
        txtPiDebug = New TextBox()
        tabMqtt = New TabPage()
        btnIdentify = New Button()
        btnDisconnect = New Button()
        txtReceive = New TextBox()
        txtSend = New TextBox()
        btnSend = New Button()
        txtLog = New TextBox()
        cmbTopicReceive = New ComboBox()
        txtPort = New TextBox()
        cboIPAddress = New ComboBox()
        cmbTopicSend = New ComboBox()
        btnConnect = New Button()
        tabDatabase = New TabPage()
        btnFilterAnwenden = New Button()
        prgDb = New ProgressBar()
        tabDbResults = New TabControl()
        tabDbUngefiltert = New TabPage()
        gridResults = New DataGridView()
        cmsDatabase = New ContextMenuStrip(components)
        mnuExportXml = New ToolStripMenuItem()
        mnuExportPythonFormat = New ToolStripMenuItem()
        tabDbGefiltert = New TabPage()
        gridResultsFiltered = New DataGridView()
        tabDbHierarchie = New TabPage()
        splitContainer1 = New SplitContainer()
        treeDeviceHierarchy = New TreeView()
        lvParameterDetails = New ListView()
        colParamName = New ColumnHeader()
        colParamValue = New ColumnHeader()
        colParamUnit = New ColumnHeader()
        colParamAddress = New ColumnHeader()
        cmsParameterDetails = New ContextMenuStrip(components)
        mnuParamQuery = New ToolStripMenuItem()
        mnuParamDirectQuery = New ToolStripMenuItem()
        btnAbfrage = New Button()
        cmbDevices = New ComboBox()
        lblProgress = New Label()
        tabMain.SuspendLayout()
        tabPi.SuspendLayout()
        cmsPiServices.SuspendLayout()
        tabMqtt.SuspendLayout()
        tabDatabase.SuspendLayout()
        tabDbResults.SuspendLayout()
        tabDbUngefiltert.SuspendLayout()
        CType(gridResults, ComponentModel.ISupportInitialize).BeginInit()
        cmsDatabase.SuspendLayout()
        tabDbGefiltert.SuspendLayout()
        CType(gridResultsFiltered, ComponentModel.ISupportInitialize).BeginInit()
        tabDbHierarchie.SuspendLayout()
        CType(splitContainer1, ComponentModel.ISupportInitialize).BeginInit()
        splitContainer1.Panel1.SuspendLayout()
        splitContainer1.Panel2.SuspendLayout()
        splitContainer1.SuspendLayout()
        cmsParameterDetails.SuspendLayout()
        SuspendLayout()
        ' 
        ' tabMain
        ' 
        tabMain.Controls.Add(tabPi)
        tabMain.Controls.Add(tabMqtt)
        tabMain.Controls.Add(tabDatabase)
        tabMain.Dock = DockStyle.Fill
        tabMain.Location = New Point(0, 0)
        tabMain.Name = "tabMain"
        tabMain.SelectedIndex = 0
        tabMain.Size = New Size(677, 496)
        tabMain.TabIndex = 100
        ' 
        ' tabPi
        ' 
        tabPi.Controls.Add(lblPiHost)
        tabPi.Controls.Add(cboPiHost)
        tabPi.Controls.Add(lblPiUser)
        tabPi.Controls.Add(txtPiUser)
        tabPi.Controls.Add(lblPiPassword)
        tabPi.Controls.Add(txtPiPassword)
        tabPi.Controls.Add(lblPiStatusLamp)
        tabPi.Controls.Add(btnPiConnect)
        tabPi.Controls.Add(btnPiDisconnect)
        tabPi.Controls.Add(btnPiQuery)
        tabPi.Controls.Add(lblPiOutput)
        tabPi.Controls.Add(lvPiServices)
        tabPi.Controls.Add(lblPiDebug)
        tabPi.Controls.Add(txtPiDebug)
        tabPi.Location = New Point(4, 24)
        tabPi.Name = "tabPi"
        tabPi.Size = New Size(669, 468)
        tabPi.TabIndex = 0
        tabPi.Text = "Raspberry Pi"
        tabPi.UseVisualStyleBackColor = True
        ' 
        ' lblPiHost
        ' 
        lblPiHost.AutoSize = True
        lblPiHost.Location = New Point(10, 15)
        lblPiHost.Name = "lblPiHost"
        lblPiHost.Size = New Size(53, 15)
        lblPiHost.TabIndex = 200
        lblPiHost.Text = "Host / IP"
        ' 
        ' cboPiHost
        ' 
        cboPiHost.Location = New Point(80, 12)
        cboPiHost.Name = "cboPiHost"
        cboPiHost.Size = New Size(150, 23)
        cboPiHost.TabIndex = 201
        cboPiHost.Text = "192.168.178.31"
        ' 
        ' lblPiUser
        ' 
        lblPiUser.AutoSize = True
        lblPiUser.Location = New Point(240, 15)
        lblPiUser.Name = "lblPiUser"
        lblPiUser.Size = New Size(30, 15)
        lblPiUser.TabIndex = 202
        lblPiUser.Text = "User"
        ' 
        ' txtPiUser
        ' 
        txtPiUser.Location = New Point(278, 12)
        txtPiUser.Name = "txtPiUser"
        txtPiUser.Size = New Size(100, 23)
        txtPiUser.TabIndex = 203
        txtPiUser.Text = "morrison"
        ' 
        ' lblPiPassword
        ' 
        lblPiPassword.AutoSize = True
        lblPiPassword.Location = New Point(385, 15)
        lblPiPassword.Name = "lblPiPassword"
        lblPiPassword.Size = New Size(54, 15)
        lblPiPassword.TabIndex = 204
        lblPiPassword.Text = "Passwort"
        ' 
        ' txtPiPassword
        ' 
        txtPiPassword.Location = New Point(448, 12)
        txtPiPassword.Name = "txtPiPassword"
        txtPiPassword.Size = New Size(120, 23)
        txtPiPassword.TabIndex = 205
        txtPiPassword.Text = "Matrix"
        txtPiPassword.UseSystemPasswordChar = True
        ' 
        ' lblPiStatusLamp
        ' 
        lblPiStatusLamp.AccessibleName = "PiConnectionLamp"
        lblPiStatusLamp.AutoSize = True
        lblPiStatusLamp.Font = New Font("Segoe UI Symbol", 12F, FontStyle.Bold)
        lblPiStatusLamp.ForeColor = Color.Red
        lblPiStatusLamp.Location = New Point(574, 12)
        lblPiStatusLamp.Name = "lblPiStatusLamp"
        lblPiStatusLamp.Size = New Size(25, 21)
        lblPiStatusLamp.TabIndex = 213
        lblPiStatusLamp.Text = "●"
        ' 
        ' btnPiConnect
        ' 
        btnPiConnect.Location = New Point(80, 41)
        btnPiConnect.Name = "btnPiConnect"
        btnPiConnect.Size = New Size(90, 23)
        btnPiConnect.TabIndex = 206
        btnPiConnect.Text = "Verbinden"
        btnPiConnect.UseVisualStyleBackColor = True
        ' 
        ' btnPiDisconnect
        ' 
        btnPiDisconnect.Location = New Point(176, 41)
        btnPiDisconnect.Name = "btnPiDisconnect"
        btnPiDisconnect.Size = New Size(90, 23)
        btnPiDisconnect.TabIndex = 207
        btnPiDisconnect.Text = "Trennen"
        btnPiDisconnect.UseVisualStyleBackColor = True
        ' 
        ' btnPiQuery
        ' 
        btnPiQuery.Enabled = False
        btnPiQuery.Location = New Point(272, 41)
        btnPiQuery.Name = "btnPiQuery"
        btnPiQuery.Size = New Size(150, 23)
        btnPiQuery.TabIndex = 208
        btnPiQuery.Text = "Dienste abfragen"
        btnPiQuery.UseVisualStyleBackColor = True
        ' 
        ' lblPiOutput
        ' 
        lblPiOutput.AutoSize = True
        lblPiOutput.Location = New Point(10, 75)
        lblPiOutput.Name = "lblPiOutput"
        lblPiOutput.Size = New Size(97, 15)
        lblPiOutput.TabIndex = 209
        lblPiOutput.Text = "Optolink-Dienste"
        ' 
        ' lvPiServices
        ' 
        lvPiServices.Columns.AddRange(New ColumnHeader() {colService, colStatus, colSection})
        lvPiServices.ContextMenuStrip = cmsPiServices
        lvPiServices.FullRowSelect = True
        lvPiServices.GridLines = True
        lvPiServices.Location = New Point(10, 93)
        lvPiServices.MultiSelect = False
        lvPiServices.Name = "lvPiServices"
        lvPiServices.Size = New Size(651, 244)
        lvPiServices.TabIndex = 210
        lvPiServices.UseCompatibleStateImageBehavior = False
        lvPiServices.View = View.Details
        ' 
        ' colService
        ' 
        colService.Text = "Service"
        colService.Width = 260
        ' 
        ' colStatus
        ' 
        colStatus.Text = "Status"
        colStatus.Width = 120
        ' 
        ' colSection
        ' 
        colSection.Text = "Quelle"
        colSection.Width = 200
        ' 
        ' cmsPiServices
        ' 
        cmsPiServices.Items.AddRange(New ToolStripItem() {mnuPiStart, mnuPiStop, mnuPiAdopt})
        cmsPiServices.Name = "cmsPiServices"
        cmsPiServices.Size = New Size(179, 70)
        ' 
        ' mnuPiStart
        ' 
        mnuPiStart.Name = "mnuPiStart"
        mnuPiStart.Size = New Size(178, 22)
        mnuPiStart.Text = "Dienst starten"
        ' 
        ' mnuPiStop
        ' 
        mnuPiStop.Name = "mnuPiStop"
        mnuPiStop.Size = New Size(178, 22)
        mnuPiStop.Text = "Dienst stoppen"
        ' 
        ' mnuPiAdopt
        ' 
        mnuPiAdopt.Name = "mnuPiAdopt"
        mnuPiAdopt.Size = New Size(178, 22)
        mnuPiAdopt.Text = "Dienst übernehmen"
        ' 
        ' lblPiDebug
        ' 
        lblPiDebug.AutoSize = True
        lblPiDebug.Location = New Point(10, 345)
        lblPiDebug.Name = "lblPiDebug"
        lblPiDebug.Size = New Size(42, 15)
        lblPiDebug.TabIndex = 211
        lblPiDebug.Text = "Debug"
        ' 
        ' txtPiDebug
        ' 
        txtPiDebug.Location = New Point(10, 363)
        txtPiDebug.Multiline = True
        txtPiDebug.Name = "txtPiDebug"
        txtPiDebug.ReadOnly = True
        txtPiDebug.ScrollBars = ScrollBars.Both
        txtPiDebug.Size = New Size(651, 97)
        txtPiDebug.TabIndex = 212
        ' 
        ' tabMqtt
        ' 
        tabMqtt.Controls.Add(btnIdentify)
        tabMqtt.Controls.Add(btnDisconnect)
        tabMqtt.Controls.Add(txtReceive)
        tabMqtt.Controls.Add(txtSend)
        tabMqtt.Controls.Add(btnSend)
        tabMqtt.Controls.Add(txtLog)
        tabMqtt.Controls.Add(cmbTopicReceive)
        tabMqtt.Controls.Add(txtPort)
        tabMqtt.Controls.Add(cboIPAddress)
        tabMqtt.Controls.Add(cmbTopicSend)
        tabMqtt.Controls.Add(btnConnect)
        tabMqtt.Location = New Point(4, 24)
        tabMqtt.Name = "tabMqtt"
        tabMqtt.Size = New Size(669, 468)
        tabMqtt.TabIndex = 1
        tabMqtt.Text = "MQTT"
        tabMqtt.UseVisualStyleBackColor = True
        ' 
        ' btnIdentify
        ' 
        btnIdentify.Location = New Point(387, 12)
        btnIdentify.Name = "btnIdentify"
        btnIdentify.Size = New Size(150, 23)
        btnIdentify.TabIndex = 10
        btnIdentify.Text = "Gerät identifizieren"
        btnIdentify.UseVisualStyleBackColor = True
        ' 
        ' btnDisconnect
        ' 
        btnDisconnect.Location = New Point(306, 12)
        btnDisconnect.Name = "btnDisconnect"
        btnDisconnect.Size = New Size(75, 23)
        btnDisconnect.TabIndex = 9
        btnDisconnect.Text = "Trennen"
        btnDisconnect.UseVisualStyleBackColor = True
        ' 
        ' txtReceive
        ' 
        txtReceive.Location = New Point(338, 71)
        txtReceive.Multiline = True
        txtReceive.Name = "txtReceive"
        txtReceive.Size = New Size(320, 200)
        txtReceive.TabIndex = 8
        ' 
        ' txtSend
        ' 
        txtSend.Location = New Point(12, 71)
        txtSend.Multiline = True
        txtSend.Name = "txtSend"
        txtSend.Size = New Size(320, 200)
        txtSend.TabIndex = 7
        txtSend.Text = "read;0xF8;8"
        ' 
        ' btnSend
        ' 
        btnSend.Font = New Font("Segoe UI", 22F)
        btnSend.Location = New Point(12, 277)
        btnSend.Name = "btnSend"
        btnSend.Size = New Size(646, 67)
        btnSend.TabIndex = 6
        btnSend.Text = "Übermitteln"
        btnSend.UseVisualStyleBackColor = True
        ' 
        ' txtLog
        ' 
        txtLog.Location = New Point(12, 350)
        txtLog.Multiline = True
        txtLog.Name = "txtLog"
        txtLog.ScrollBars = ScrollBars.Vertical
        txtLog.Size = New Size(646, 107)
        txtLog.TabIndex = 5
        ' 
        ' cmbTopicReceive
        ' 
        cmbTopicReceive.Location = New Point(338, 42)
        cmbTopicReceive.Name = "cmbTopicReceive"
        cmbTopicReceive.Size = New Size(320, 23)
        cmbTopicReceive.TabIndex = 4
        ' 
        ' txtPort
        ' 
        txtPort.Location = New Point(118, 12)
        txtPort.Name = "txtPort"
        txtPort.Size = New Size(100, 23)
        txtPort.TabIndex = 3
        txtPort.Text = "1883"
        ' 
        ' cboIPAddress
        ' 
        cboIPAddress.Location = New Point(12, 12)
        cboIPAddress.Name = "cboIPAddress"
        cboIPAddress.Size = New Size(100, 23)
        cboIPAddress.TabIndex = 2
        cboIPAddress.Text = "192.168.178.31"
        ' 
        ' cmbTopicSend
        ' 
        cmbTopicSend.Location = New Point(12, 41)
        cmbTopicSend.Name = "cmbTopicSend"
        cmbTopicSend.Size = New Size(320, 23)
        cmbTopicSend.TabIndex = 1
        ' 
        ' btnConnect
        ' 
        btnConnect.Location = New Point(225, 12)
        btnConnect.Name = "btnConnect"
        btnConnect.Size = New Size(75, 23)
        btnConnect.TabIndex = 0
        btnConnect.Text = "Verbinden"
        btnConnect.UseVisualStyleBackColor = True
        ' 
        ' tabDatabase
        ' 
        tabDatabase.Controls.Add(btnFilterAnwenden)
        tabDatabase.Controls.Add(prgDb)
        tabDatabase.Controls.Add(tabDbResults)
        tabDatabase.Controls.Add(btnAbfrage)
        tabDatabase.Controls.Add(cmbDevices)
        tabDatabase.Location = New Point(4, 24)
        tabDatabase.Name = "tabDatabase"
        tabDatabase.Size = New Size(669, 468)
        tabDatabase.TabIndex = 2
        tabDatabase.Text = "Database"
        tabDatabase.UseVisualStyleBackColor = True
        ' 
        ' btnFilterAnwenden
        ' 
        btnFilterAnwenden.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnFilterAnwenden.Enabled = False
        btnFilterAnwenden.Location = New Point(385, 12)
        btnFilterAnwenden.Name = "btnFilterAnwenden"
        btnFilterAnwenden.Size = New Size(134, 23)
        btnFilterAnwenden.TabIndex = 208
        btnFilterAnwenden.Text = "Filter anwenden"
        btnFilterAnwenden.UseVisualStyleBackColor = True
        ' 
        ' prgDb
        ' 
        prgDb.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        prgDb.Location = New Point(10, 439)
        prgDb.MarqueeAnimationSpeed = 40
        prgDb.Name = "prgDb"
        prgDb.Size = New Size(649, 15)
        prgDb.Style = ProgressBarStyle.Marquee
        prgDb.TabIndex = 206
        prgDb.Visible = False
        ' 
        ' tabDbResults
        ' 
        tabDbResults.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        tabDbResults.Controls.Add(tabDbUngefiltert)
        tabDbResults.Controls.Add(tabDbGefiltert)
        tabDbResults.Controls.Add(tabDbHierarchie)
        tabDbResults.Location = New Point(10, 50)
        tabDbResults.Name = "tabDbResults"
        tabDbResults.SelectedIndex = 0
        tabDbResults.Size = New Size(649, 385)
        tabDbResults.TabIndex = 207
        ' 
        ' tabDbUngefiltert
        ' 
        tabDbUngefiltert.Controls.Add(gridResults)
        tabDbUngefiltert.Location = New Point(4, 24)
        tabDbUngefiltert.Name = "tabDbUngefiltert"
        tabDbUngefiltert.Padding = New Padding(3)
        tabDbUngefiltert.Size = New Size(641, 357)
        tabDbUngefiltert.TabIndex = 0
        tabDbUngefiltert.Text = "Ungefiltert"
        tabDbUngefiltert.UseVisualStyleBackColor = True
        ' 
        ' gridResults
        ' 
        gridResults.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        gridResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
        gridResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        gridResults.ContextMenuStrip = cmsDatabase
        gridResults.Location = New Point(3, 3)
        gridResults.Name = "gridResults"
        gridResults.ReadOnly = True
        gridResults.Size = New Size(635, 351)
        gridResults.TabIndex = 205
        ' 
        ' cmsDatabase
        ' 
        cmsDatabase.Items.AddRange(New ToolStripItem() {mnuExportXml, mnuExportPythonFormat})
        cmsDatabase.Name = "cmsDatabase"
        cmsDatabase.Size = New Size(220, 48)
        ' 
        ' mnuExportXml
        ' 
        mnuExportXml.Name = "mnuExportXml"
        mnuExportXml.Size = New Size(219, 22)
        mnuExportXml.Text = "Export nach XML..."
        ' 
        ' mnuExportPythonFormat
        ' 
        mnuExportPythonFormat.Name = "mnuExportPythonFormat"
        mnuExportPythonFormat.Size = New Size(219, 22)
        mnuExportPythonFormat.Text = "Export Python-Format..."
        ' 
        ' tabDbGefiltert
        ' 
        tabDbGefiltert.Controls.Add(gridResultsFiltered)
        tabDbGefiltert.Location = New Point(4, 24)
        tabDbGefiltert.Name = "tabDbGefiltert"
        tabDbGefiltert.Padding = New Padding(3)
        tabDbGefiltert.Size = New Size(641, 357)
        tabDbGefiltert.TabIndex = 1
        tabDbGefiltert.Text = "Gefiltert"
        tabDbGefiltert.UseVisualStyleBackColor = True
        ' 
        ' gridResultsFiltered
        ' 
        gridResultsFiltered.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        gridResultsFiltered.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
        gridResultsFiltered.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        gridResultsFiltered.ContextMenuStrip = cmsDatabase
        gridResultsFiltered.Location = New Point(3, 3)
        gridResultsFiltered.Name = "gridResultsFiltered"
        gridResultsFiltered.ReadOnly = True
        gridResultsFiltered.Size = New Size(635, 351)
        gridResultsFiltered.TabIndex = 206
        ' 
        ' tabDbHierarchie
        ' 
        tabDbHierarchie.Controls.Add(splitContainer1)
        tabDbHierarchie.Location = New Point(4, 24)
        tabDbHierarchie.Name = "tabDbHierarchie"
        tabDbHierarchie.Padding = New Padding(3)
        tabDbHierarchie.Size = New Size(641, 357)
        tabDbHierarchie.TabIndex = 2
        tabDbHierarchie.Text = "Hierarchie"
        tabDbHierarchie.UseVisualStyleBackColor = True
        ' 
        ' splitContainer1
        ' 
        splitContainer1.Dock = DockStyle.Fill
        splitContainer1.Location = New Point(3, 3)
        splitContainer1.Name = "splitContainer1"
        ' 
        ' splitContainer1.Panel1
        ' 
        splitContainer1.Panel1.Controls.Add(treeDeviceHierarchy)
        ' 
        ' splitContainer1.Panel2
        ' 
        splitContainer1.Panel2.Controls.Add(lvParameterDetails)
        splitContainer1.Size = New Size(635, 351)
        splitContainer1.SplitterDistance = 300
        splitContainer1.TabIndex = 0
        ' 
        ' treeDeviceHierarchy
        ' 
        treeDeviceHierarchy.Dock = DockStyle.Fill
        treeDeviceHierarchy.Location = New Point(0, 0)
        treeDeviceHierarchy.Name = "treeDeviceHierarchy"
        treeDeviceHierarchy.Size = New Size(300, 351)
        treeDeviceHierarchy.TabIndex = 0
        ' 
        ' lvParameterDetails
        ' 
        lvParameterDetails.Columns.AddRange(New ColumnHeader() {colParamName, colParamValue, colParamUnit, colParamAddress})
        lvParameterDetails.ContextMenuStrip = cmsParameterDetails
        lvParameterDetails.Dock = DockStyle.Fill
        lvParameterDetails.FullRowSelect = True
        lvParameterDetails.GridLines = True
        lvParameterDetails.Location = New Point(0, 0)
        lvParameterDetails.Name = "lvParameterDetails"
        lvParameterDetails.Size = New Size(331, 351)
        lvParameterDetails.TabIndex = 0
        lvParameterDetails.UseCompatibleStateImageBehavior = False
        lvParameterDetails.View = View.Details
        ' 
        ' colParamName
        ' 
        colParamName.Text = "Parameter"
        colParamName.Width = 180
        ' 
        ' colParamValue
        ' 
        colParamValue.Text = "Wert"
        colParamValue.Width = 80
        ' 
        ' colParamUnit
        ' 
        colParamUnit.Text = "Einheit"
        ' 
        ' colParamAddress
        ' 
        colParamAddress.Text = "Adresse"
        colParamAddress.Width = 80
        ' 
        ' cmsParameterDetails
        ' 
        cmsParameterDetails.Items.AddRange(New ToolStripItem() {mnuParamQuery, mnuParamDirectQuery})
        cmsParameterDetails.Name = "cmsParameterDetails"
        cmsParameterDetails.Size = New Size(158, 48)
        ' 
        ' mnuParamQuery
        ' 
        mnuParamQuery.Name = "mnuParamQuery"
        mnuParamQuery.Size = New Size(157, 22)
        mnuParamQuery.Text = "Abfragen..."
        ' 
        ' mnuParamDirectQuery
        ' 
        mnuParamDirectQuery.Name = "mnuParamDirectQuery"
        mnuParamDirectQuery.Size = New Size(157, 22)
        mnuParamDirectQuery.Text = "Direkt Abfragen"
        ' 
        ' btnAbfrage
        ' 
        btnAbfrage.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnAbfrage.Location = New Point(525, 12)
        btnAbfrage.Name = "btnAbfrage"
        btnAbfrage.Size = New Size(134, 23)
        btnAbfrage.TabIndex = 204
        btnAbfrage.Text = "Abfrage"
        btnAbfrage.UseVisualStyleBackColor = True
        ' 
        ' cmbDevices
        ' 
        cmbDevices.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        cmbDevices.DropDownStyle = ComboBoxStyle.DropDownList
        cmbDevices.FormattingEnabled = True
        cmbDevices.Location = New Point(10, 12)
        cmbDevices.Name = "cmbDevices"
        cmbDevices.Size = New Size(369, 23)
        cmbDevices.TabIndex = 203
        ' 
        ' lblProgress
        ' 
        lblProgress.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        lblProgress.Location = New Point(1532, 465)
        lblProgress.Name = "lblProgress"
        lblProgress.Size = New Size(80, 20)
        lblProgress.TabIndex = 14
        lblProgress.Text = "0 %"
        lblProgress.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(677, 496)
        Controls.Add(tabMain)
        Controls.Add(lblProgress)
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Form1"
        tabMain.ResumeLayout(False)
        tabPi.ResumeLayout(False)
        tabPi.PerformLayout()
        cmsPiServices.ResumeLayout(False)
        tabMqtt.ResumeLayout(False)
        tabMqtt.PerformLayout()
        tabDatabase.ResumeLayout(False)
        tabDbResults.ResumeLayout(False)
        tabDbUngefiltert.ResumeLayout(False)
        CType(gridResults, ComponentModel.ISupportInitialize).EndInit()
        cmsDatabase.ResumeLayout(False)
        tabDbGefiltert.ResumeLayout(False)
        CType(gridResultsFiltered, ComponentModel.ISupportInitialize).EndInit()
        tabDbHierarchie.ResumeLayout(False)
        splitContainer1.Panel1.ResumeLayout(False)
        splitContainer1.Panel2.ResumeLayout(False)
        CType(splitContainer1, ComponentModel.ISupportInitialize).EndInit()
        splitContainer1.ResumeLayout(False)
        cmsParameterDetails.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents tabMain As TabControl
    Friend WithEvents tabPi As TabPage
    Friend WithEvents tabMqtt As TabPage
    Friend WithEvents btnConnect As Button
    Friend WithEvents cmbTopicSend As ComboBox
    Friend WithEvents cboIPAddress As ComboBox
    Friend WithEvents txtPort As TextBox
    Friend WithEvents cmbTopicReceive As ComboBox
    Friend WithEvents txtLog As TextBox
    Friend WithEvents btnSend As Button
    Friend WithEvents txtReceive As TextBox
    Friend WithEvents txtSend As TextBox
    Friend WithEvents btnDisconnect As Button
    Friend WithEvents lblProgress As Label
    Friend WithEvents btnIdentify As Button
    Friend WithEvents lblPiHost As Label
    Friend WithEvents cboPiHost As ComboBox
    Friend WithEvents lblPiUser As Label
    Friend WithEvents txtPiUser As TextBox
    Friend WithEvents lblPiPassword As Label
    Friend WithEvents txtPiPassword As TextBox
    Friend WithEvents lblPiStatusLamp As Label
    Friend WithEvents btnPiConnect As Button
    Friend WithEvents btnPiDisconnect As Button
    Friend WithEvents btnPiQuery As Button
    Friend WithEvents lblPiOutput As Label
    Friend WithEvents lvPiServices As ListView
    Friend WithEvents colService As ColumnHeader
    Friend WithEvents colStatus As ColumnHeader
    Friend WithEvents colSection As ColumnHeader
    Friend WithEvents cmsPiServices As ContextMenuStrip
    Friend WithEvents mnuPiStart As ToolStripMenuItem
    Friend WithEvents mnuPiStop As ToolStripMenuItem
    Friend WithEvents mnuPiAdopt As ToolStripMenuItem
    Friend WithEvents lblPiDebug As Label
    Friend WithEvents txtPiDebug As TextBox
    Friend WithEvents tabDatabase As TabPage
    Friend WithEvents tabDbResults As TabControl
    Friend WithEvents tabDbUngefiltert As TabPage
    Friend WithEvents gridResults As DataGridView
    Friend WithEvents tabDbGefiltert As TabPage
    Friend WithEvents gridResultsFiltered As DataGridView
    Friend WithEvents tabDbHierarchie As TabPage
    Friend WithEvents splitContainer1 As SplitContainer
    Friend WithEvents treeDeviceHierarchy As TreeView
    Friend WithEvents lvParameterDetails As ListView
    Friend WithEvents colParamName As ColumnHeader
    Friend WithEvents colParamValue As ColumnHeader
    Friend WithEvents colParamUnit As ColumnHeader
    Friend WithEvents colParamAddress As ColumnHeader
    Friend WithEvents cmsParameterDetails As ContextMenuStrip
    Friend WithEvents mnuParamQuery As ToolStripMenuItem
    Friend WithEvents mnuParamDirectQuery As ToolStripMenuItem
    Friend WithEvents btnAbfrage As Button
    Friend WithEvents cmbDevices As ComboBox
    Friend WithEvents prgDb As ProgressBar
    Friend WithEvents cmsDatabase As ContextMenuStrip
    Friend WithEvents mnuExportXml As ToolStripMenuItem
    Friend WithEvents mnuExportPythonFormat As ToolStripMenuItem
    Friend WithEvents btnFilterAnwenden As Button

End Class
