Imports System.Text

Public Class RaspberryPiSshService
    Private _client As SshClient

    Public ReadOnly Property IsConnected As Boolean
        Get
            Return _client IsNot Nothing AndAlso _client.IsConnected
        End Get
    End Property

    Public Sub Connect(host As String, username As String, password As String)
        Disconnect()
        Try
            _client = New SshClient(host, username, password)
            _client.Connect()
        Catch ex As Exception
            Debug.WriteLine($"[SSH] Connect fehlgeschlagen ({host}): {ex.GetType().Name}: {ex.Message}")
            Throw
        End Try
    End Sub

    ' Neue asynchrone Variante, um die UI nicht zu blockieren
    Public Async Function ConnectAsync(host As String, username As String, password As String) As Task
        Await Task.Run(Sub() Connect(host, username, password))
    End Function

    Public Function RunCommand(command As String, Optional timeoutMs As Integer = 15000) As String
        If _client Is Nothing OrElse Not _client.IsConnected Then
            Throw New InvalidOperationException("SSH ist nicht verbunden.")
        End If
        Try
            ' Ensure shell features like pipes/grep by invoking bash
            Dim wrapped = $"bash -lc ""{command}"""
            Dim cmd = _client.CreateCommand(wrapped)
            cmd.CommandTimeout = TimeSpan.FromMilliseconds(timeoutMs)
            Dim result = cmd.Execute()
            Dim sb As New StringBuilder()
            If Not String.IsNullOrEmpty(result) Then Dim unused1 = sb.AppendLine(result.TrimEnd())
            If Not String.IsNullOrEmpty(cmd.Error) Then Dim unused = sb.AppendLine("[stderr] " & cmd.Error.TrimEnd())
            Dim s = sb.ToString()
            ' Normalize to Windows CRLF for WinForms TextBox rendering
            s = s.Replace(vbCrLf, vbLf).Replace(vbLf, Environment.NewLine)
            Return s.TrimEnd()
        Catch ex As Exception
            Debug.WriteLine($"[SSH] RunCommand Fehler: {ex.GetType().Name}: {ex.Message}")
            Throw
        End Try
    End Function

    Public Sub Disconnect()
        If _client IsNot Nothing Then
            Try
                If _client.IsConnected Then _client.Disconnect()
            Finally
                Try
                    _client.Dispose()
                Catch ex As Exception
                    Debug.WriteLine($"[SSH] Dispose Fehler: {ex.GetType().Name}: {ex.Message}")
                End Try
                _client = Nothing
            End Try
        End If
    End Sub
End Class