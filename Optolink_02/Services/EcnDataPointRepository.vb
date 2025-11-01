Imports System.IO
Imports System.Xml.Serialization

Namespace Services
    Public Class EcnDataPointRepository
        Private ReadOnly _filePath As String
        Private _cache As EcnDataPointTypes

        Public Sub New(xmlFilePath As String)
            _filePath = xmlFilePath
        End Sub

        Public Function GetAll() As EcnDataPointTypes
            If _cache IsNot Nothing Then Return _cache
            If Not File.Exists(_filePath) Then Throw New FileNotFoundException("XML nicht gefunden.", _filePath)
            Dim ser As New XmlSerializer(GetType(EcnDataPointTypes))
            Using fs = File.OpenRead(_filePath)
                _cache = DirectCast(ser.Deserialize(fs), EcnDataPointTypes)
            End Using
            If _cache Is Nothing OrElse _cache.Items Is Nothing Then
                _cache = New EcnDataPointTypes With {.Items = New List(Of EcnDataPointType)()}
            End If
            Return _cache
        End Function

        ''' <summary>
        ''' Lädt die ecnDataPointType.xml Datei aus dem Standard-Pfad
        ''' </summary>
        Public Shared Function LoadDataPointTypes() As EcnDataPointTypes
            Try
                ' Suche ecnDataPointType.xml im Projekt-Verzeichnis
                Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
                Dim possiblePaths As String() = {
                    IO.Path.Combine(baseDir, "XML", "ecnDataPointType.xml"),
                    IO.Path.Combine(baseDir, "Data", "XML", "ecnDataPointType.xml"),
                    IO.Path.Combine(baseDir, "ecnDataPointType.xml"),
                    IO.Path.Combine(baseDir, "..", "..", "XML", "ecnDataPointType.xml")
                }

                For Each path In possiblePaths
                    Dim fullPath = IO.Path.GetFullPath(path)
                    If File.Exists(fullPath) Then
                        Debug.WriteLine($"[REPO] Lade ecnDataPointType.xml von: {fullPath}")
                        Dim repo As New EcnDataPointRepository(fullPath)
                        Return repo.GetAll()
                    End If
                Next

                Debug.WriteLine("[REPO] ecnDataPointType.xml nicht gefunden in:")
                For Each path In possiblePaths
                    Debug.WriteLine($"  - {IO.Path.GetFullPath(path)}")
                Next

                ' Rückgabe leerer Liste wenn Datei nicht gefunden
                Return New EcnDataPointTypes With {.Items = New List(Of EcnDataPointType)()}

            Catch ex As Exception
                Debug.WriteLine($"[ERROR] LoadDataPointTypes: {ex.Message}")
                ' Rückgabe leerer Liste bei Fehler
                Return New EcnDataPointTypes With {.Items = New List(Of EcnDataPointType)()}
            End Try
        End Function

    End Class
End Namespace