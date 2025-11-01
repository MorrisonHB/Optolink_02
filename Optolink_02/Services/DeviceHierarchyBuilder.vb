Imports Optolink_02.Domain

Namespace Services
    ''' <summary>
    ''' Baut die hierarchische Struktur der Geräte-Events aus der Datenbank auf
    ''' </summary>
    Public NotInheritable Class DeviceHierarchyBuilder
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Erstellt die Gerätehierarchie aus einer DataTable
        ''' </summary>
        Public Shared Function BuildHierarchy(dt As DataTable) As DeviceNode
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                Return Nothing
            End If

            Dim device As New DeviceNode()
            Dim categoryDict As New Dictionary(Of String, CategoryNode)()
            Dim groupDict As New Dictionary(Of String, GroupNode)()

            For Each row As DataRow In dt.Rows
                Try
                    ' Spalten auslesen (mit Fehlerbehandlung für fehlende Spalten)
                    Dim kategorieName = If(dt.Columns.Contains("Kategorie"), Convert.ToString(row("Kategorie")), "")
                    Dim gruppeName = If(dt.Columns.Contains("Gruppe"), Convert.ToString(row("Gruppe")), "")
                    Dim paramId = If(dt.Columns.Contains("EventTypeID"), Convert.ToString(row("EventTypeID")), "")
                    Dim paramName = If(dt.Columns.Contains("Parameter"), Convert.ToString(row("Parameter")), "Unbekannt")
                    Dim address = If(dt.Columns.Contains("Address"), Convert.ToString(row("Address")), "")
                    Dim dataType = If(dt.Columns.Contains("DataType"), Convert.ToString(row("DataType")), "")
                    Dim hiddenCondition = If(dt.Columns.Contains("Condition"), Convert.ToString(row("Condition")), "")
                    Dim unit = If(dt.Columns.Contains("Unit"), Convert.ToString(row("Unit")), "")
                    Dim description = If(dt.Columns.Contains("Beschreibung"), Convert.ToString(row("Beschreibung")), "")
                    Dim mqttRead = If(dt.Columns.Contains("MQTT_Read"), Convert.ToString(row("MQTT_Read")), "")
                    Dim mqttWrite = If(dt.Columns.Contains("MQTT_Write"), Convert.ToString(row("MQTT_Write")), "")

                    ' Conversion-Informationen auslesen
                    Dim conversion = If(dt.Columns.Contains("Conversion"), Convert.ToString(row("Conversion")), "")
                    Dim byteLength = If(dt.Columns.Contains("ByteLength"),
                If(IsDBNull(row("ByteLength")), 2, Convert.ToInt32(row("ByteLength"))), 2)
                    Dim conversionFactor = If(dt.Columns.Contains("ConversionFactor"),
       If(IsDBNull(row("ConversionFactor")), 1.0, Convert.ToDouble(row("ConversionFactor"))), 1.0)
                    Dim conversionOffset = If(dt.Columns.Contains("ConversionOffset"),
     If(IsDBNull(row("ConversionOffset")), 0.0, Convert.ToDouble(row("ConversionOffset"))), 0.0)
                    Dim stepping = If(dt.Columns.Contains("Stepping"), Convert.ToString(row("Stepping")), "")

                    ' WICHTIG: Übersetze Kategorie und Gruppe mit TextResourceService
                    kategorieName = TextResourceService.TranslateInline(kategorieName)
                    gruppeName = TextResourceService.TranslateInline(gruppeName)
                    paramName = TextResourceService.TranslateInline(paramName)
                    unit = TextResourceService.TranslateInline(unit)

                    ' Behandle NULL/Empty-Werte für Kategorie und Gruppe
                    If String.IsNullOrWhiteSpace(kategorieName) Then kategorieName = "Ohne Kategorie"
                    If String.IsNullOrWhiteSpace(gruppeName) Then gruppeName = "Ohne Gruppe"

                    ' Verwende den Namen als eindeutigen Key (da wir keine IDs haben)
                    Dim kategorieKey = kategorieName

                    ' Kategorie holen oder erstellen - TryGetValue vermeidet doppelte Lookup
                    Dim currentCategory As CategoryNode = Nothing
                    If Not categoryDict.TryGetValue(kategorieKey, currentCategory) Then
                        currentCategory = New CategoryNode With {
        .CategoryId = kategorieKey,
       .CategoryName = kategorieName
       }
                        categoryDict(kategorieKey) = currentCategory
                        device.Categories.Add(currentCategory)
                        Debug.WriteLine($"[HIERARCHY] Neue Kategorie: {kategorieName}")
                    End If

                    ' Erstelle eindeutigen Gruppen-Key: Kategorie|Gruppe
                    ' (da gleiche Gruppennamen in verschiedenen Kategorien vorkommen können)
                    Dim groupKey = kategorieKey & "|" & gruppeName

                    ' Gruppe holen oder erstellen - TryGetValue vermeidet doppelte Lookup
                    Dim currentGroup As GroupNode = Nothing
                    If Not groupDict.TryGetValue(groupKey, currentGroup) Then
                        currentGroup = New GroupNode With {
     .GroupId = groupKey,
   .GroupName = gruppeName,
       .HiddenCondition = hiddenCondition,
   .IsVisible = True
}
                        groupDict(groupKey) = currentGroup
                        currentCategory.Groups.Add(currentGroup)
                        Debug.WriteLine($"[HIERARCHY] Neue Gruppe in '{kategorieName}': {gruppeName}")
                    End If

                    ' Aktualisiere HIDDEN-Condition der Gruppe falls vorhanden
                    ' (nehme die erste nicht-leere Condition)
                    If String.IsNullOrWhiteSpace(currentGroup.HiddenCondition) AndAlso
           Not String.IsNullOrWhiteSpace(hiddenCondition) AndAlso
       hiddenCondition.StartsWith("HIDDEN:", StringComparison.OrdinalIgnoreCase) Then
                        currentGroup.HiddenCondition = hiddenCondition
                        Debug.WriteLine($"[HIERARCHY] HIDDEN-Condition für Gruppe '{gruppeName}': {hiddenCondition}")
                    End If

                    ' Parameter hinzufügen
                    Dim parameter As New ParameterNode With {
   .ParameterId = paramId,
    .ParameterName = paramName,
    .Address = address,
      .DataType = dataType,
     .HiddenCondition = hiddenCondition,
     .IsVisible = True,
.Unit = unit,
        .Description = description,
     .MqttReadCommand = mqttRead,
     .MqttWriteCommand = mqttWrite,
   .CurrentValue = "",
    .Conversion = conversion,
   .ByteLength = byteLength,
            .ConversionFactor = conversionFactor,
 .ConversionOffset = conversionOffset,
       .Stepping = stepping
     }

                    currentGroup.Parameters.Add(parameter)

                Catch ex As Exception
                    Debug.WriteLine($"[ERROR] Fehler beim Verarbeiten der Zeile: {ex.Message}")
                    Continue For
                End Try
            Next

            ' Debug-Ausgabe der Hierarchie
            Debug.WriteLine($"[HIERARCHY] Gerät erstellt mit {device.Categories.Count} Kategorien")
            For Each cat In device.Categories
                Debug.WriteLine($"[HIERARCHY]   Kategorie '{cat.CategoryName}' mit {cat.Groups.Count} Gruppen")
                For Each grp In cat.Groups
                    Debug.WriteLine($"[HIERARCHY]     Gruppe '{grp.GroupName}' mit {grp.Parameters.Count} Parametern")
                Next
            Next

            Return device
        End Function

        ''' <summary>
        ''' Füllt ein TreeView mit der Gerätehierarchie
        ''' </summary>
        Public Shared Sub PopulateTreeView(treeView As TreeView, device As DeviceNode, deviceName As String)
            If treeView Is Nothing OrElse device Is Nothing Then
                Return
            End If

            treeView.BeginUpdate()
            treeView.Nodes.Clear()

            ' Gerät als Root-Knoten
            Dim deviceNode As New TreeNode(deviceName) With {
  .Tag = device,
       .ImageIndex = 0,
      .SelectedImageIndex = 0
         }
            Dim unused3 = treeView.Nodes.Add(deviceNode)

            ' Kategorien hinzufügen (sortiert)
            Dim sortedCategories = device.Categories.OrderBy(Function(c) c.CategoryName).ToList()

            For Each category In sortedCategories
                Dim categoryNode As New TreeNode(category.CategoryName) With {
         .Tag = category,
  .ImageIndex = 1,
              .SelectedImageIndex = 1
         }
                Dim unused2 = deviceNode.Nodes.Add(categoryNode)

                ' Gruppen hinzufügen (nur sichtbare, sortiert)
                Dim visibleGroups = category.Groups.Where(Function(g) g.IsVisible).OrderBy(Function(g) g.GroupName).ToList()

                For Each group In visibleGroups
                    Dim groupNode As New TreeNode(group.GroupName) With {
      .Tag = group,
       .ImageIndex = 2,
      .SelectedImageIndex = 2
       }
                    Dim unused1 = categoryNode.Nodes.Add(groupNode)

                    ' Parameter hinzufügen (nur sichtbare, sortiert)
                    Dim visibleParams = group.Parameters.Where(Function(p) p.IsVisible).OrderBy(Function(p) p.ParameterName).ToList()

                    For Each param In visibleParams
                        Dim paramNode As New TreeNode(param.ParameterName) With {
       .Tag = param,
        .ImageIndex = 3,
        .SelectedImageIndex = 3
  }
                        Dim unused = groupNode.Nodes.Add(paramNode)
                    Next
                Next
            Next

            deviceNode.Expand()
            treeView.EndUpdate()

            Debug.WriteLine($"[TREEVIEW] TreeView gefüllt: {deviceNode.Nodes.Count} Kategorien")
        End Sub

        ''' <summary>
        ''' Aktualisiert die ListView mit Parameter-Details
        ''' </summary>
        Public Shared Sub UpdateParameterListView(listView As ListView, node As TreeNode)
            If listView Is Nothing Then
                Return
            End If

            listView.Items.Clear()

            If node Is Nothing OrElse node.Tag Is Nothing Then
                Return
            End If

            ' Je nach Node-Typ unterschiedlich handeln
            If TypeOf node.Tag Is ParameterNode Then
                ' Einzelner Parameter
                Dim param = TryCast(node.Tag, ParameterNode)
                AddParameterToListView(listView, param)

            ElseIf TypeOf node.Tag Is GroupNode Then
                ' Alle Parameter der Gruppe
                Dim group = TryCast(node.Tag, GroupNode)
                For Each param In group.Parameters.Where(Function(p) p.IsVisible).OrderBy(Function(p) p.ParameterName)
                    AddParameterToListView(listView, param)
                Next

            ElseIf TypeOf node.Tag Is CategoryNode Then
                ' Alle Parameter aller Gruppen der Kategorie
                Dim category = TryCast(node.Tag, CategoryNode)
                For Each group In category.Groups.Where(Function(g) g.IsVisible).OrderBy(Function(g) g.GroupName)
                    For Each param In group.Parameters.Where(Function(p) p.IsVisible).OrderBy(Function(p) p.ParameterName)
                        AddParameterToListView(listView, param)
                    Next
                Next

            ElseIf TypeOf node.Tag Is DeviceNode Then
                ' Alle Parameter des Geräts
                Dim device = TryCast(node.Tag, DeviceNode)
                For Each category In device.Categories.OrderBy(Function(c) c.CategoryName)
                    For Each group In category.Groups.Where(Function(g) g.IsVisible).OrderBy(Function(g) g.GroupName)
                        For Each param In group.Parameters.Where(Function(p) p.IsVisible).OrderBy(Function(p) p.ParameterName)
                            AddParameterToListView(listView, param)
                        Next
                    Next
                Next
            End If
        End Sub

        Private Shared Sub AddParameterToListView(listView As ListView, param As ParameterNode)
            Dim item As New ListViewItem(param.ParameterName) With {
                .Tag = param
       }
            Dim unused3 = item.SubItems.Add(param.CurrentValue)
            Dim unused2 = item.SubItems.Add(param.Unit)
            Dim unused1 = item.SubItems.Add(param.Address)
            Dim unused = listView.Items.Add(item)
        End Sub
    End Class
End Namespace
