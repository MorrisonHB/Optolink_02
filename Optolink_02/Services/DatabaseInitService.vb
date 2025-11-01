Namespace Services
    Public NotInheritable Class DatabaseInitService
        Private Sub New()
        End Sub

        ' Startet die DB-Initialisierung im Hintergrund (Attach/Erstellung)
        Public Shared Function WarmupAsync(buildConnectionString As Func(Of String)) As Task
            ArgumentNullException.ThrowIfNull(buildConnectionString)
            Return Task.Run(Sub()
                                Try
                                    Dim tmp = buildConnectionString()
                                Catch
                                    ' Ignorieren - Fehler werden später bei der Abfrage angezeigt
                                End Try
                            End Sub)
        End Function
    End Class
End Namespace