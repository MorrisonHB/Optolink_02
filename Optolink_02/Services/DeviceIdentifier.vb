Imports System.Text.RegularExpressions

Public Class DeviceIdentifier
    Public Shared Function MatchDevices(identity As DeviceIdentity,
                                 repo As EcnDataPointTypes) As List(Of EcnDataPointType)
        Dim all As List(Of EcnDataPointType) = If(repo IsNot Nothing AndAlso
            repo.Items IsNot Nothing, repo.Items, New List(Of EcnDataPointType)())
        Dim idHex = identity.IdentificationHex

        ' 1) Identification match with optional X wildcards in XML
        Dim candidates = all.Where(Function(d As EcnDataPointType)
                                       Dim ident = If(d.Identification, String.Empty).Trim().ToUpperInvariant()
                                       If String.IsNullOrWhiteSpace(ident) OrElse ident = "-" Then Return False
                                       Return WildcardHexEquals(ident, idHex)
                                   End Function).ToList()
        If candidates.Count = 0 Then
            ' Fallback: some XML entries may only require the first 2 bytes at F8 (i.e., Identification only)
            candidates = all.Where(Function(d As EcnDataPointType)
                                       Dim ident = If(d.Identification, String.Empty).Trim().ToUpperInvariant()
                                       If String.IsNullOrWhiteSpace(ident) OrElse ident = "-" Then Return False
                                       ' Allow prefix match of first two bytes
                                       Return ident.StartsWith(idHex)
                                   End Function).ToList()
        End If
        If candidates.Count <= 1 Then Return candidates

        ' 2) Filter by IdentificationExtension if present
        ' Prefer SW index (bytes 6+7) as IdentificationExtension unless device family uses HW index
        Dim extHex = identity.SWIndexHex
        Dim withExt = candidates.Where(Function(d As EcnDataPointType)
                                           Return Not String.IsNullOrWhiteSpace(d.IdentificationExtension)
                                       End Function).ToList()
        Dim withoutExt = candidates.Where(Function(d As EcnDataPointType)
                                              Return String.IsNullOrWhiteSpace(d.IdentificationExtension)
                                          End Function).ToList()

        Dim narrowed As New List(Of EcnDataPointType)()
        If withExt.Count > 0 Then
            For Each d As EcnDataPointType In withExt
                Dim fromHex = d.IdentificationExtension?.Trim()?.ToUpperInvariant()
                Dim tillHex = d.IdentificationExtensionTill?.Trim()?.ToUpperInvariant()
                If String.IsNullOrEmpty(fromHex) Then Continue For
                If String.IsNullOrEmpty(tillHex) Then
                    If String.Equals(fromHex,
                                     extHex,
                                     StringComparison.OrdinalIgnoreCase) OrElse
                                     ExtPlaceholderMatch(fromHex, extHex) Then
                        narrowed.Add(d)
                    End If
                Else
                    If InExtRange(fromHex, tillHex, extHex) Then
                        narrowed.Add(d)
                    End If
                End If
            Next
        End If

        If narrowed.Count > 0 Then
            candidates = narrowed
        ElseIf withoutExt.Count > 0 Then
            ' Try again using HW index (bytes 2+3) if SW index didn't narrow
            Dim hwExt = identity.ExtensionHex2
            Dim narrowedHw As New List(Of EcnDataPointType)()
            For Each d As EcnDataPointType In withExt
                Dim fromHex = d.IdentificationExtension?.Trim()?.ToUpperInvariant()
                Dim tillHex = d.IdentificationExtensionTill?.Trim()?.ToUpperInvariant()
                If String.IsNullOrEmpty(fromHex) Then Continue For
                If String.IsNullOrEmpty(tillHex) Then
                    If String.Equals(fromHex,
                                     hwExt,
                                     StringComparison.OrdinalIgnoreCase) OrElse
                                     ExtPlaceholderMatch(fromHex, hwExt) Then
                        narrowedHw.Add(d)
                    End If
                Else
                    If InExtRange(fromHex, tillHex, hwExt) Then
                        narrowedHw.Add(d)
                    End If
                End If
            Next
            candidates = If(narrowedHw.Count > 0, narrowedHw, withoutExt)
        End If

        If candidates.Count <= 1 Then Return candidates

        ' 3) Filter by F0 if present in XML and we have read F0
        If identity.RawF0.HasValue Then
            Dim f0 = identity.RawF0.Value
            Dim usingF0 = candidates.Where(Function(d As EcnDataPointType)
                                               Return Not String.IsNullOrWhiteSpace(d.F0)
                                           End Function).ToList()
            If usingF0.Count > 0 Then
                Dim narrowedF0 As New List(Of EcnDataPointType)()
                For Each d As EcnDataPointType In usingF0
                    Dim f0From = d.F0?.Trim()?.ToUpperInvariant()
                    Dim f0Till = d.F0Till?.Trim()?.ToUpperInvariant()
                    If String.IsNullOrEmpty(f0From) Then Continue For
                    Dim f0Val = CInt(f0)
                    If String.IsNullOrEmpty(f0Till) Then
                        Dim fromVal = Convert.ToInt32(f0From, 16)
                        If f0Val = fromVal Then narrowedF0.Add(d)
                    Else
                        Dim fromVal = Convert.ToInt32(f0From, 16)
                        Dim tillVal = Convert.ToInt32(f0Till, 16)
                        If f0Val >= fromVal AndAlso f0Val <= tillVal Then narrowedF0.Add(d)
                    End If
                Next
                If narrowedF0.Count > 0 Then candidates = narrowedF0
            End If
        End If

        Return candidates
    End Function

    Private Shared Function WildcardHexEquals(pattern As String, value As String) As Boolean
        ' pattern may contain X characters as wildcard for a single hex
        If pattern.Length <> value.Length Then Return False
        For i = 0 To pattern.Length - 1
            Dim pc = pattern(i)
            Dim vc = value(i)
            If pc = "X"c Then Continue For
            If pc <> vc Then Return False
        Next
        Return True
    End Function

    Private Shared Function InExtRange(fromHex As String, tillHex As String, valueHex As String) As Boolean
        fromHex = fromHex.PadLeft(4, "0"c)
        tillHex = tillHex.PadLeft(4, "0"c)
        valueHex = valueHex.PadLeft(4, "0"c)
        ' If range uses 01xx, sometimes 01 is placeholder => compare only low byte
        If fromHex.StartsWith("01") AndAlso tillHex.StartsWith("01") Then
            Dim v = Convert.ToInt32(valueHex.Substring(2, 2), 16)
            Dim f = Convert.ToInt32(fromHex.Substring(2, 2), 16)
            Dim t = Convert.ToInt32(tillHex.Substring(2, 2), 16)
            Return v >= f AndAlso v <= t
        Else
            Dim v = Convert.ToInt32(valueHex, 16)
            Dim f = Convert.ToInt32(fromHex, 16)
            Dim t = Convert.ToInt32(tillHex, 16)
            Return v >= f AndAlso v <= t
        End If
    End Function

    Private Shared Function ExtPlaceholderMatch(extPattern As String, extValue As String) As Boolean
        ' Accept equal low byte if extPattern starts with 01 (placeholder)
        If String.IsNullOrWhiteSpace(extPattern) OrElse
            extPattern.Length < 4 OrElse
            extValue Is Nothing OrElse
            extValue.Length < 4 Then Return False
        Return extPattern.StartsWith("01", StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(extPattern.Substring(2, 2),
                                 extValue.Substring(2, 2),
                                 StringComparison.OrdinalIgnoreCase)
    End Function
End Class

Public Class MqttDeviceReader
    Private ReadOnly _mqtt As MqttService
    Private ReadOnly _sendTopic As String
    Private ReadOnly _receiveTopic As String

    Public Sub New(mqtt As MqttService, sendTopic As String, receiveTopic As String)
        _mqtt = mqtt
        _sendTopic = sendTopic
        _receiveTopic = receiveTopic
    End Sub

    Public Async Function ReadBytesAsync(address As Integer,
                                         length As Integer,
                                         Optional timeoutMs As Integer = 3000) As Task(Of Byte())
        Dim cmd = $"read;0x{address:X2};{length}"
        Dim resp = Await _mqtt.RequestResponseAsync(_sendTopic, cmd, _receiveTopic, timeoutMs)
        Dim bytes = ParseBytes(resp)
        If bytes Is Nothing OrElse bytes.Length < length Then
            Throw New InvalidOperationException($"Antwort unvollständig ({bytes?.Length} Bytes): {resp}")
        End If
        Return bytes.Take(length).ToArray()
    End Function

    Private Shared Function ParseBytes(s As String) As Byte()
        If String.IsNullOrWhiteSpace(s) Then Return Array.Empty(Of Byte)()

        ' Preferred: parse last semicolon segment if it is a continuous hex string (even length)
        Dim parts = s.Split(";"c)
        For i = parts.Length - 1 To 0 Step -1
            Dim seg = parts(i).Trim()
            If seg.Length >= 2 AndAlso seg.Length Mod 2 = 0 AndAlso Regex.IsMatch(seg, "\A(?i)[0-9a-f]+\z") Then
                Dim buf As New List(Of Byte)()
                For p = 0 To seg.Length - 2 Step 2
                    buf.Add(Convert.ToByte(seg.Substring(p, 2), 16))
                Next
                Return buf.ToArray()
            End If
        Next

        ' Fallback: collect any 0xNN tokens
        Dim list As New List(Of Byte)()
        Dim m = Regex.Matches(s, "(?i)0x([0-9a-f]{2})")
        For Each mm As Match In m
            list.Add(Convert.ToByte(mm.Groups(1).Value, 16))
        Next
        Return list.ToArray()
    End Function

End Class