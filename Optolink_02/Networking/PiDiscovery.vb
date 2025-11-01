Imports System.Collections.Concurrent
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Text

Public NotInheritable Class PiDiscovery
    Private Sub New()
    End Sub

    ' Bekannte Raspberry Pi OUIs
    Private Shared ReadOnly PiOui As String() = {
        "B8:27:EB",
        "DC:A6:32",
        "E4:5F:01"
    }

    Public Class DiscoveredHost
        Public Property IP As IPAddress
        Public Property Mac As String
        Public Property Hostname As String
        Public Property IsRaspberryPi As Boolean
        Public Overrides Function ToString() As String
            Return $"{IP} {Mac} {Hostname} Pi={IsRaspberryPi}"
        End Function
    End Class

    '1) mDNS
    Public Shared Async Function ResolveMdnsAsync(Optional hostname As String = "raspberrypi.local") As Task(Of IPAddress())
        Try
            Return Await Dns.GetHostAddressesAsync(hostname)
        Catch ex As Exception
            Debug.WriteLine($"[PiDiscovery] ResolveMdnsAsync Fehler: {ex.GetType().Name}: {ex.Message}")
            Return Array.Empty(Of IPAddress)()
        End Try
    End Function

    '2) Netzwerkscan: Ping-Sweep + ARP (MAC-OUI)
    Public Shared Async Function FindPisOnLocalNetworkAsync(Optional timeoutMs As Integer = 250,
    Optional maxConcurrency As Integer = 64,
    Optional maxHostsPerSubnet As Integer = 1024) As Task(Of List(Of DiscoveredHost))
        Dim results As New ConcurrentBag(Of DiscoveredHost)()
        Try
            For Each ni In NetworkInterface.GetAllNetworkInterfaces()
                If ni.OperationalStatus <> OperationalStatus.Up Then Continue For
                If ni.NetworkInterfaceType = NetworkInterfaceType.Loopback OrElse
                    ni.NetworkInterfaceType = NetworkInterfaceType.Tunnel Then Continue For

                For Each ua In ni.GetIPProperties().UnicastAddresses
                    If ua.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                    If IPAddress.IsLoopback(ua.Address) Then Continue For
                    If ua.IPv4Mask Is Nothing Then Continue For

                    Dim network As UInteger, broadcast As UInteger
                    ComputeNetworkBounds(ua.Address, ua.IPv4Mask, network, broadcast)

                    Dim hostCount As UInteger = Math.Max(0UI, broadcast - network - 1UI)
                    If hostCount = 0UI Then Continue For

                    ' Begrenzen, um sehr große Netze zu vermeiden
                    Dim scanStart As UInteger = network + 1UI
                    Dim scanEnd As UInteger = broadcast - 1UI
                    If hostCount > CUInt(maxHostsPerSubnet) Then
                        ' auf /24 umschalten (IP mit255.255.255.0)
                        Dim ip As UInteger = ToUInt32(ua.Address)
                        Dim cMask As UInteger = ToUInt32(IPAddress.Parse("255.255.255.0"))
                        Dim cNet As UInteger = ip And cMask
                        scanStart = cNet + 1UI
                        scanEnd = cNet + 254UI
                    End If

                    Dim throttler = New Threading.SemaphoreSlim(maxConcurrency)
                    Dim tasks As New List(Of Task)()

                    For host As UInteger = scanStart To scanEnd
                        Dim ip = FromUInt32(host)
                        tasks.Add(Task.Run(Async Function()
                                               Await throttler.WaitAsync().ConfigureAwait(False)
                                               Try
                                                   Dim reply = Await PingOnceAsync(ip, timeoutMs).ConfigureAwait(False)
                                                   If reply IsNot Nothing AndAlso
                                                   reply.Status = IPStatus.Success Then
                                                       Dim mac = GetMacAddress(ip)
                                                       Dim hostname = Await TryGetHostnameAsync(ip).ConfigureAwait(False)
                                                       Dim isPi = IsRaspberryPiMac(mac)
                                                       results.Add(New DiscoveredHost With {
                                                               .IP = ip,
                                                               .Mac = mac,
                                                               .Hostname = hostname,
                                                               .IsRaspberryPi = isPi
                                                           })
                                                   End If
                                               Catch ex As Exception
                                                   Debug.WriteLine($"[PiDiscovery] ScanHost {ip}: {ex.GetType().Name}: {ex.Message}")
                                               Finally
                                                   Dim unused = throttler.Release()
                                               End Try
                                           End Function))
                    Next

                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                Next
            Next
        Catch ex As Exception
            Debug.WriteLine($"[PiDiscovery] FindPisOnLocalNetworkAsync Fehler: {ex.GetType().Name}: {ex.Message}")
        End Try

        ' Deduplizieren nach IP
        Dim unique = results.GroupBy(Function(r)
                                         Return r.IP.ToString()
                                     End Function) _
        .Select(Function(g) g.First()) _
        .OrderBy(Function(r) Not r.IsRaspberryPi) _
        .ThenBy(Function(r) r.IP.ToString()) _
        .ToList()
        Return unique
    End Function

    ' MQTT Host Scan: Hosts mit offenen1883 TCP
    Public Shared Async Function FindMqttHostsOnLocalNetworkAsync(Optional timeoutMs As Integer = 200,
    Optional maxConcurrency As Integer = 64,
    Optional maxHostsPerSubnet As Integer = 1024) As Task(Of List(Of IPAddress))
        Dim results As New ConcurrentBag(Of IPAddress)()
        Try
            For Each ni In NetworkInterface.GetAllNetworkInterfaces()
                If ni.OperationalStatus <> OperationalStatus.Up Then Continue For
                If ni.NetworkInterfaceType = NetworkInterfaceType.Loopback OrElse
                    ni.NetworkInterfaceType = NetworkInterfaceType.Tunnel Then Continue For

                For Each ua In ni.GetIPProperties().UnicastAddresses
                    If ua.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                    If IPAddress.IsLoopback(ua.Address) Then Continue For
                    If ua.IPv4Mask Is Nothing Then Continue For

                    Dim network As UInteger, broadcast As UInteger
                    ComputeNetworkBounds(ua.Address, ua.IPv4Mask, network, broadcast)

                    Dim hostCount As UInteger = Math.Max(0UI, broadcast - network - 1UI)
                    If hostCount = 0UI Then Continue For

                    Dim scanStart As UInteger = network + 1UI
                    Dim scanEnd As UInteger = broadcast - 1UI
                    If hostCount > CUInt(maxHostsPerSubnet) Then
                        Dim ip As UInteger = ToUInt32(ua.Address)
                        Dim cMask As UInteger = ToUInt32(IPAddress.Parse("255.255.255.0"))
                        Dim cNet As UInteger = ip And cMask
                        scanStart = cNet + 1UI
                        scanEnd = cNet + 254UI
                    End If

                    Dim throttler = New Threading.SemaphoreSlim(maxConcurrency)
                    Dim tasks As New List(Of Task)()

                    For host As UInteger = scanStart To scanEnd
                        Dim ip = FromUInt32(host)
                        tasks.Add(Task.Run(Async Function()
                                               Await throttler.WaitAsync().ConfigureAwait(False)
                                               Try
                                                   If Await IsTcpPortOpenAsync(ip, 1883, timeoutMs).ConfigureAwait(False) Then
                                                       results.Add(ip)
                                                   End If
                                               Catch ex As Exception
                                                   Debug.WriteLine($"[PiDiscovery] ScanPort {ip}: {ex.GetType().Name}: {ex.Message}")
                                               Finally
                                                   Dim unused = throttler.Release()
                                               End Try
                                           End Function))
                    Next

                    Await Task.WhenAll(tasks).ConfigureAwait(False)
                Next
            Next
        Catch ex As Exception
            Debug.WriteLine($"[PiDiscovery] FindMqttHostsOnLocalNetworkAsync Fehler: {ex.GetType().Name}: {ex.Message}")
        End Try

        Dim unique = results.Distinct().OrderBy(Function(a) a.ToString()).ToList()
        Return unique
    End Function

    Private Shared Async Function IsTcpPortOpenAsync(ip As IPAddress, port As Integer, timeoutMs As Integer) As Task(Of Boolean)
        Using client As New TcpClient()
            Dim cts As New Threading.CancellationTokenSource(timeoutMs)
            Try
                Dim t = client.ConnectAsync(ip, port)
                Dim completed = Await Task.WhenAny(t, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(False)
                Return t.IsCompleted AndAlso client.Connected
            Catch ex As Exception
                Debug.WriteLine($"[PiDiscovery] IsTcpPortOpenAsync {ip}:{port} Fehler: {ex.GetType().Name}: {ex.Message}")
                Return False
            Finally
                cts.Cancel()
            End Try
        End Using
    End Function

    Private Shared Async Function PingOnceAsync(ip As IPAddress, timeoutMs As Integer) As Task(Of PingReply)
        Using p As New Ping()
            Try
                Return Await p.SendPingAsync(ip, timeoutMs).ConfigureAwait(False)
            Catch ex As Exception
                Debug.WriteLine($"[PiDiscovery] PingOnceAsync {ip} Fehler: {ex.GetType().Name}: {ex.Message}")
                Return Nothing
            End Try
        End Using
    End Function

    Private Shared Async Function TryGetHostnameAsync(ip As IPAddress) As Task(Of String)
        Try
            Dim he = Await Dns.GetHostEntryAsync(ip).ConfigureAwait(False)
            Return he.HostName
        Catch ex As Exception
            Debug.WriteLine($"[PiDiscovery] TryGetHostnameAsync {ip} Fehler: {ex.GetType().Name}: {ex.Message}")
            Return ""
        End Try
    End Function

    Private Shared Function IsRaspberryPiMac(mac As String) As Boolean
        If String.IsNullOrWhiteSpace(mac) Then Return False
        Dim upper = mac.ToUpperInvariant()
        For Each p In PiOui
            If upper.StartsWith(p, StringComparison.Ordinal) Then Return True
        Next
        Return False
    End Function

    ' ARP: MAC-Adresse zu einer IPv4 ermitteln
    Public Shared Function GetMacAddress(ip As IPAddress) As String
        Try
            If ip.AddressFamily <> AddressFamily.InterNetwork Then Return ""
            Dim dest As UInteger = ToUInt32(ip)
            Dim mac(5) As Byte
            Dim len As Integer = mac.Length
            Dim ret = SendARP(dest, 0UI, mac, len)
            If ret <> 0 Then Return ""
            Dim sb As New StringBuilder()
            For i = 0 To len - 1
                If i > 0 Then Dim unused1 = sb.Append(":"c)
                Dim unused = sb.Append(mac(i).ToString("X2"))
            Next
            Return sb.ToString()
        Catch ex As Exception
            Debug.WriteLine($"[PiDiscovery] GetMacAddress {ip} Fehler: {ex.GetType().Name}: {ex.Message}")
            Return ""
        End Try
    End Function

    <DllImport("iphlpapi.dll", ExactSpelling:=True)>
    Private Shared Function SendARP(destIP As UInteger,
                                    srcIP As UInteger,
                                    pMacAddr As Byte(),
                                    ByRef phyAddrLen As Integer) As Integer
    End Function

    ' Hilfsfunktionen IP <-> UInt32
    Private Shared Function ToUInt32(ip As IPAddress) As UInteger
        Dim b = ip.GetAddressBytes()
        Array.Reverse(b) ' little endian
        Return BitConverter.ToUInt32(b, 0)
    End Function

    Private Shared Function FromUInt32(val As UInteger) As IPAddress
        Dim b = BitConverter.GetBytes(val)
        Array.Reverse(b)
        Return New IPAddress(b)
    End Function

    Private Shared Sub ComputeNetworkBounds(ip As IPAddress,
                                            mask As IPAddress,
                                            ByRef network As UInteger,
                                            ByRef broadcast As UInteger)
        Dim ip32 = ToUInt32(ip)
        Dim mask32 = ToUInt32(mask)
        network = ip32 And mask32
        broadcast = network Or Not mask32
    End Sub
End Class