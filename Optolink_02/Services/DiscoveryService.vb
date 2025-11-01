Namespace Services
    Public NotInheritable Class DiscoveryService
        Private Sub New()
        End Sub

        Public Shared Async Function ResolveMdnsPiAsync() As Task(Of List(Of String))
            Dim result As New List(Of String)()
            Try
                Dim mdnsPi = Await PiDiscovery.ResolveMdnsAsync("raspberrypi.local")
                If mdnsPi IsNot Nothing AndAlso mdnsPi.Length > 0 Then
                    result.AddRange(mdnsPi.Select(Function(a) a.ToString()))
                End If
            Catch
            End Try
            Return result
        End Function

        Public Shared Async Function FindPiHostsAsync() As Task(Of List(Of String))
            Try
                Dim piHosts = Await PiDiscovery.FindPisOnLocalNetworkAsync(timeoutMs:=250,
                                                                           maxConcurrency:=64,
                                                                           maxHostsPerSubnet:=1024)
                If piHosts Is Nothing OrElse piHosts.Count = 0 Then Return New List(Of String)()
                Dim ordered = piHosts.OrderBy(Function(h) Not h.IsRaspberryPi).ThenBy(Function(h) h.IP.ToString()).ToList()
                Return ordered.Select(Function(h) h.IP.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            Catch
                Return New List(Of String)()
            End Try
        End Function

        Public Shared Async Function FindMqttHostsAsync() As Task(Of List(Of String))
            Try
                Dim mqttHosts = Await PiDiscovery.FindMqttHostsOnLocalNetworkAsync(timeoutMs:=200,
                                                                                   maxConcurrency:=64,
                                                                                   maxHostsPerSubnet:=1024)
                If mqttHosts Is Nothing OrElse mqttHosts.Count = 0 Then Return New List(Of String)()
                Return mqttHosts.Select(Function(ip) ip.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            Catch
                Return New List(Of String)()
            End Try
        End Function
    End Class
End Namespace
