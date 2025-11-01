Imports System.Text
Imports System.Threading
Imports MQTTnet.Client
Imports MQTTnet.Formatter
Imports MQTTnet.Protocol

Public Class MqttService
    Implements IDisposable

    Private _client As IMqttClient
    Private _options As MqttClientOptions
    Private ReadOnly _waiters As New Dictionary(Of String,
        Queue(Of TaskCompletionSource(Of String)))(StringComparer.OrdinalIgnoreCase)
    Private _disposed As Boolean = False

    Public Event MessageReceived(topic As String, payload As String)
    Public Event Log(message As String)

    Public ReadOnly Property IsConnected As Boolean
        Get
            Return _client IsNot Nothing AndAlso _client.IsConnected
        End Get
    End Property

    Public Async Function ConnectAsync(host As String, port As Integer, subscribeTopic As String) As Task
        Dim factory = New MqttFactory()
        _client = factory.CreateMqttClient()

        _options = New MqttClientOptionsBuilder() _
            .WithTcpServer(host, port) _
            .WithProtocolVersion(MqttProtocolVersion.V311) _
            .Build()

        AddHandler _client.ApplicationMessageReceivedAsync,
            Function(e)
                Dim payload As String = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
                Dim topic As String = e.ApplicationMessage.Topic
                RaiseEvent MessageReceived(topic, payload)
                ' Complete waiters for this topic
                Try
                    Dim tcs As TaskCompletionSource(Of String) = Nothing
                    SyncLock _waiters
                        If _waiters.TryGetValue(topic, Nothing) Then
                            Dim q As Queue(Of TaskCompletionSource(Of String)) = Nothing
                            If _waiters.TryGetValue(topic, q) AndAlso q IsNot Nothing AndAlso q.Count > 0 Then
                                tcs = q.Dequeue()
                                If q.Count = 0 Then Dim unused3 = _waiters.Remove(topic)
                            End If
                        End If
                    End SyncLock
                    If tcs IsNot Nothing AndAlso Not tcs.Task.IsCompleted Then
                        Dim unused2 = tcs.TrySetResult(payload)
                    End If
                Catch ex As Exception
                    ' Silent error handling
                End Try
                Return Task.CompletedTask
            End Function
        Try
            Dim unused1 = Await _client.ConnectAsync(_options, CancellationToken.None)
            If Not String.IsNullOrWhiteSpace(subscribeTopic) Then
                Dim unused = Await _client.SubscribeAsync(subscribeTopic, MqttQualityOfServiceLevel.AtLeastOnce)
                RaiseEvent Log($"Verbunden und abonniert: {subscribeTopic}")
            Else
                RaiseEvent Log("Verbunden.")
            End If
        Catch ex As Exception
            RaiseEvent Log($"MQTT-Verbindung fehlgeschlagen: {ex.Message}")
            Throw
        End Try
    End Function

    Public Async Function PublishAsync(topic As String, payload As String) As Task
        If _client Is Nothing OrElse Not _client.IsConnected Then
            Throw New InvalidOperationException("MQTT ist nicht verbunden.")
        End If

        Dim message = New MqttApplicationMessageBuilder() _
            .WithTopic(topic) _
            .WithPayload(Encoding.UTF8.GetBytes(payload)) _
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce) _
            .WithRetainFlag(False) _
            .Build()
        Try
            Dim unused = Await _client.PublishAsync(message, CancellationToken.None)
            RaiseEvent Log($"Gesendet: {payload}")
        Catch ex As Exception
            RaiseEvent Log($"MQTT-Senden fehlgeschlagen: {ex.Message}")
            Throw
        End Try
    End Function

    Public Async Function DisconnectAsync() As Task
        Try
            If _client IsNot Nothing AndAlso _client.IsConnected Then
                Await _client.DisconnectAsync()
                RaiseEvent Log("Verbindung getrennt.")
            End If
        Catch ex As Exception
            RaiseEvent Log($"MQTT-Trennen fehlgeschlagen: {ex.Message}")
        Finally
            ' Cleanup waiters
            Try
                SyncLock _waiters
                    For Each kvp In _waiters
                        While kvp.Value.Count > 0
                            Dim tcs = kvp.Value.Dequeue()
                            If Not tcs.Task.IsCompleted Then
                                Dim unused = tcs.TrySetCanceled()
                            End If
                        End While
                    Next
                    _waiters.Clear()
                End SyncLock
            Catch ex As Exception
                ' Silent error handling
            End Try
        End Try
    End Function

    Public Async Function WaitForNextMessageAsync(topic As String,
                                                  Optional timeoutMs As Integer = 3000) As Task(Of String)
        If String.IsNullOrWhiteSpace(topic) Then Throw New ArgumentException(Nothing, NameOf(topic))
        Dim tcs As New TaskCompletionSource(Of String)(TaskCreationOptions.RunContinuationsAsynchronously)
        SyncLock _waiters
            Dim q As Queue(Of TaskCompletionSource(Of String)) = Nothing
            If Not _waiters.TryGetValue(topic, q) OrElse q Is Nothing Then
                q = New Queue(Of TaskCompletionSource(Of String))()
                _waiters(topic) = q
            End If
            q.Enqueue(tcs)
        End SyncLock

        Dim cts = New CancellationTokenSource(timeoutMs)
        Using cts
            Try
                Dim completed = Await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token))
                If completed Is tcs.Task Then
                    Return Await tcs.Task
                Else
                    ' timeout - remove this tcs from queue if still present
                    SyncLock _waiters
                        Dim q As Queue(Of TaskCompletionSource(Of String)) = Nothing
                        If _waiters.TryGetValue(topic, q) AndAlso q IsNot Nothing Then
                            Dim remaining As New Queue(Of TaskCompletionSource(Of String))()
                            While q.Count > 0
                                Dim cur = q.Dequeue()
                                If Not ReferenceEquals(cur, tcs) Then remaining.Enqueue(cur)
                            End While
                            If remaining.Count > 0 Then
                                _waiters(topic) = remaining
                            Else
                                Dim unused = _waiters.Remove(topic)
                            End If
                        End If
                    End SyncLock
                    Throw New TimeoutException($"Timeout beim Warten auf Topic '{topic}'.")
                End If
            Catch ex As Exception
                Throw
            End Try
        End Using
    End Function

    Public Async Function RequestResponseAsync(sendTopic As String,
                                               payload As String,
                                               receiveTopic As String,
                                               Optional timeoutMs As Integer = 3000) As Task(Of String)
        Await PublishAsync(sendTopic, payload)
        Return Await WaitForNextMessageAsync(receiveTopic, timeoutMs)
    End Function

    ''' <summary>
    ''' Sendet ein MQTT-Command und wartet auf die Antwort
    ''' Alias für RequestResponseAsync zur besseren Lesbarkeit
    ''' </summary>
    Public Async Function SendCommandAndWaitForResponseAsync(command As String,
                                                             sendTopic As String,
                                                             receiveTopic As String,
                                                             Optional timeoutMs As Integer = 3000) As Task(Of String)
        Return Await RequestResponseAsync(sendTopic, command, receiveTopic, timeoutMs)
    End Function

    ' IDisposable Implementation
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                Try
                    ' Disconnect synchronously if still connected
                    If _client IsNot Nothing AndAlso _client.IsConnected Then
                        DisconnectAsync().GetAwaiter().GetResult()
                    End If

                    ' Dispose client
                    If _client IsNot Nothing Then
                        _client.Dispose()
                        _client = Nothing
                    End If
                Catch ex As Exception
                    ' Silent error handling
                End Try
            End If
            _disposed = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overrides Sub Finalize()
        Dispose(False)
    End Sub

End Class