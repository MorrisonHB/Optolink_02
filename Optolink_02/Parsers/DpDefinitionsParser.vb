Namespace Parsers
    Public NotInheritable Class DpDefinitionsParser
        Private Sub New()
        End Sub

        Private Shared _loaded As Boolean = False
        Private Shared ReadOnly _sync As New Object()

        ' dictionaries from DPDefinitions
        Private Shared _displayConditionGroups As List(Of Dictionary(Of String, String))
        Private Shared _displayConditionsByGroup As Dictionary(Of Integer, List(Of Dictionary(Of String, String)))
        Private Shared _eventTypes As Dictionary(Of Integer, Dictionary(Of String, String))
        Private Shared _eventValueTypes As Dictionary(Of Integer, Dictionary(Of String, String))
        Private Shared _eventGroupsByEventType As Dictionary(Of Integer, List(Of Integer))


        Private Shared Function GetXmlPath(fileName As String) As String
            Dim baseDir As String = TryCast(AppDomain.CurrentDomain.GetData("DataDirectory"), String)
            If String.IsNullOrEmpty(baseDir) Then
                baseDir = AppDomain.CurrentDomain.BaseDirectory
            End If
            Return IO.Path.Combine(baseDir, "XML", fileName)
        End Function

        Private Shared Sub EnsureLoaded()
            If _loaded Then Return
            SyncLock _sync
                If _loaded Then Return

                _displayConditionGroups = New List(Of Dictionary(Of String, String))()
                _displayConditionsByGroup = New Dictionary(Of Integer, List(Of Dictionary(Of String, String)))()
                _eventTypes = New Dictionary(Of Integer, Dictionary(Of String, String))()
                _eventValueTypes = New Dictionary(Of Integer, Dictionary(Of String, String))()
                _eventGroupsByEventType = New Dictionary(Of Integer, List(Of Integer))()

                ' Load DPDefinitions
                Dim dpPath = GetXmlPath("DPDefinitions.xml")
                If IO.File.Exists(dpPath) Then
                    Dim doc As XDocument = XDocument.Load(dpPath, LoadOptions.PreserveWhitespace)

                    ' helper to get groupType_value of child element by local name
                    Dim getVal = Function(el As XElement, name As String) As String
                                     Dim c = el.Elements().FirstOrDefault(Function(x)
                                                                              Return x.Name.LocalName = name
                                                                          End Function)
                                     Return c?.Value
                                 End Function

                    ' ecnDisplayConditionGroup
                    For Each el In doc.Descendants().Where(Function(x)
                                                               Return x.Name.LocalName = "ecnDisplayConditionGroup"
                                                           End Function)
                        Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                        For Each c In el.Elements()
                            d(c.Name.LocalName) = c.Value
                        Next
                        _displayConditionGroups.Add(d)
                    Next

                    ' ecnDisplayCondition
                    For Each el In doc.Descendants().Where(Function(x)
                                                               Return x.Name.LocalName = "ecnDisplayCondition"
                                                           End Function)
                        Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                        For Each c In el.Elements()
                            d(c.Name.LocalName) = c.Value
                        Next
                        Dim groupIdStr As String = Nothing
                        If d.TryGetValue("ConditionGroupId", groupIdStr) Then
                            Dim gid As Integer
                            If Integer.TryParse(groupIdStr, gid) Then
                                Dim list As List(Of Dictionary(Of String, String)) = Nothing
                                If Not _displayConditionsByGroup.TryGetValue(gid, list) Then
                                    list = New List(Of Dictionary(Of String, String))()
                                    _displayConditionsByGroup(gid) = list
                                End If
                                list.Add(d)
                            End If
                        End If
                    Next

                    ' ecnEventType (only Id and Name used)
                    For Each el In doc.Descendants().Where(Function(x)
                                                               Return x.Name.LocalName = "ecnEventType"
                                                           End Function)
                        Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                        For Each c In el.Elements()
                            d(c.Name.LocalName) = c.Value
                        Next
                        Dim idStr As String = Nothing
                        If d.TryGetValue("Id", idStr) Then
                            Dim id As Integer
                            If Integer.TryParse(idStr, id) Then
                                _eventTypes(id) = d
                            End If
                        End If
                    Next

                    ' ecnEventValueType (Id, EnumReplaceValue, EnumAddressValue)
                    For Each el In doc.Descendants().Where(Function(x)
                                                               Return x.Name.LocalName = "ecnEventValueType"
                                                           End Function)
                        Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                        For Each c In el.Elements()
                            d(c.Name.LocalName) = c.Value
                        Next
                        Dim idStr As String = Nothing
                        If d.TryGetValue("Id", idStr) Then
                            Dim id As Integer
                            If Integer.TryParse(idStr, id) Then
                                _eventValueTypes(id) = d
                            End If
                        End If
                    Next

                    ' ecnEventTypeEventTypeGroupLink (map EventTypeId -> list of EventTypeGroupId)
                    For Each el In doc.Descendants().Where(Function(x)
                                                               Return x.Name.LocalName = "ecnEventTypeEventTypeGroupLink"
                                                           End Function)
                        Dim evIdStr As String =
                            el.Elements().FirstOrDefault(Function(x)
                                                             Return x.Name.LocalName = "EventTypeId"
                                                         End Function)?.Value
                        Dim grpIdStr As String =
                            el.Elements().FirstOrDefault(Function(x)
                                                             Return x.Name.LocalName = "EventTypeGroupId"
                                                         End Function)?.Value
                        Dim evId As Integer, grpId As Integer
                        If Integer.TryParse(evIdStr, evId) AndAlso
                            Integer.TryParse(grpIdStr, grpId) Then
                            Dim list As List(Of Integer) = Nothing
                            If Not _eventGroupsByEventType.TryGetValue(evId, list) Then
                                list = New List(Of Integer)()
                                _eventGroupsByEventType(evId) = list
                            End If
                            If Not list.Contains(grpId) Then
                                list.Add(grpId)
                            End If
                        End If
                    Next
                End If

                _loaded = True
            End SyncLock
        End Sub

        Public Shared Function GetConditionByEventTypeId(eventTypeId As Integer) As String
            Try
                EnsureLoaded()
                Dim condDict As New Dictionary(Of Integer, String) From {
                    {0, "="}, {1, "≠"}, {2, ">"}, {3, "≥"}, {4, "<"}, {5, "≤"}}
                Dim operDict As New Dictionary(Of Integer, String) From {{1, "AND"}, {2, "OR"}}

                ' Wie im Python-Skript: groupCond wird als String aufgebaut (Zeile 93)
                Dim groupCond As String = ""

                ' Nur Gruppen berücksichtigen, die explizit dieses Event als Ziel haben (Zeile 91-92)
                For Each group In _displayConditionGroups
                    Dim destStr As String = Nothing
                    If Not group.TryGetValue("EventTypeIdDest", destStr) Then Continue For
                    Dim destId As Integer
                    If Not Integer.TryParse(destStr, destId) Then Continue For
                    If destId <> eventTypeId Then Continue For

                    Dim groupIdStr As String = Nothing
                    If Not group.TryGetValue("Id", groupIdStr) Then Continue For
                    Dim groupId As Integer
                    If Not Integer.TryParse(groupIdStr, groupId) Then Continue For

                    Dim typeStr As String = Nothing
                    Dim groupType As Integer = 1
                    If group.TryGetValue("Type", typeStr) Then
                        Dim tmp As Integer
                        If Integer.TryParse(typeStr, tmp) Then groupType = tmp
                    End If

                    Dim ll As New List(Of String)()
                    Dim conds As List(Of Dictionary(Of String, String)) = Nothing
                    If _displayConditionsByGroup.TryGetValue(groupId, conds) Then
                        For Each dispCond In conds
                            Dim eventTypeCondIdStr As String = Nothing
                            If Not dispCond.TryGetValue("EventTypeIdCondition", eventTypeCondIdStr) Then Continue For
                            Dim eventTypeCondId As Integer
                            If Not Integer.TryParse(eventTypeCondIdStr, eventTypeCondId) Then Continue For

                            Dim evType As Dictionary(Of String, String) = Nothing
                            If Not _eventTypes.TryGetValue(eventTypeCondId, evType) Then Continue For
                            Dim name As String = Nothing
                            evType.TryGetValue("Name", name)
                            name = If(name, String.Empty).Trim()
                            name = $"""{name}"""

                            Dim evValIdStr As String = Nothing
                            If Not dispCond.TryGetValue("EventTypeValueCondition", evValIdStr) Then Continue For
                            Dim evValId As Integer
                            If Not Integer.TryParse(evValIdStr, evValId) Then Continue For

                            Dim evVal As Dictionary(Of String, String) = Nothing
                            Dim val As String = String.Empty
                            If _eventValueTypes.TryGetValue(evValId, evVal) Then
                                Dim rep As String = Nothing
                                If evVal.TryGetValue("EnumReplaceValue", rep) AndAlso Not String.IsNullOrEmpty(rep) Then
                                    val = $"""{rep}"""
                                Else
                                    Dim addr As String = Nothing
                                    If evVal.TryGetValue("EnumAddressValue", addr) AndAlso Not String.IsNullOrEmpty(addr) Then
                                        val = $"""{addr}"""
                                    End If
                                End If
                            End If

                            Dim op As String = "="
                            ' Zeile 100 im Python: if 'EqualCondition' in dispCond
                            If dispCond.ContainsKey("EqualCondition") Then
                                ll.Add(name & "=" & val)
                            Else
                                ' Zeile 102: ll.append(name + condDict[int(dispCond['Condition'])] + val)
                                Dim condCodeStr As String = Nothing
                                If dispCond.TryGetValue("Condition", condCodeStr) Then
                                    Dim code As Integer
                                    If Integer.TryParse(condCodeStr, code) Then
                                        Dim code_value As String = Nothing
                                        If condDict.TryGetValue(code, code_value) Then
                                            op = code_value
                                        End If
                                    End If
                                End If
                                ll.Add(name & op & val)
                            End If
                        Next
                    End If

                    ' Zeile 103 im Python: groupCond += (' ' + operDict[displayConditionGroup['Type']] + ' ').join(ll)
                    If ll.Count > 0 Then
                        Dim connector As String = "AND"
                        Dim groupType_value As String = Nothing
                        If operDict.TryGetValue(groupType, groupType_value) Then
                            connector = groupType_value
                        End If
                        groupCond &= String.Join(" " & connector & " ", ll)
                    End If
                Next

                If groupCond.Length > 0 Then
                    Return " HIDDEN:(" & groupCond & ")"
                End If
            Catch ex As Exception
                Debug.WriteLine("parse-error: " & ex.Message)
            End Try
            Return String.Empty
        End Function

    End Class
End Namespace