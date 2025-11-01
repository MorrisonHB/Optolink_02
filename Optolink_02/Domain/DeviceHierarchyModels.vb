''' <summary>
''' Datenmodelle für die hierarchische Darstellung der Geräte-Events
''' Basierend auf der Struktur aus den DP_Listen Dateien
''' </summary>
Namespace Domain
    ''' <summary>
    ''' Repräsentiert ein Gerät (z.B. V050HK1M)
    ''' </summary>
    Public Class DeviceNode
        Public Property DeviceName As String
        Public Property Categories As New List(Of CategoryNode)
    End Class

    ''' <summary>
    ''' Repräsentiert eine Kategorie (z.B. "# Überblick (7008)")
    ''' </summary>
    Public Class CategoryNode
        Public Property CategoryId As String
        Public Property CategoryName As String
        Public Property Groups As New List(Of GroupNode)
    End Class

    ''' <summary>
    ''' Repräsentiert eine Gruppe (z.B. "- Allgemein (1971)")
    ''' </summary>
    Public Class GroupNode
        Public Property GroupId As String
        Public Property GroupName As String
        Public Property HiddenCondition As String
        Public Property IsVisible As Boolean = True
        Public Property Parameters As New List(Of ParameterNode)
    End Class

    ''' <summary>
    ''' Repräsentiert einen Parameter/Wert (z.B. "Aussentemperatur (5373)")
    ''' </summary>
    Public Class ParameterNode
        Public Property ParameterId As String
        Public Property ParameterName As String
        Public Property Address As String
        Public Property DataType As String
        Public Property ReadWrite As String ' NEU: R, R/W oder W
        Public Property HiddenCondition As String
        Public Property IsVisible As Boolean = True
        Public Property Unit As String
        Public Property Description As String
        Public Property MqttReadCommand As String
        Public Property MqttWriteCommand As String
        Public Property CurrentValue As String

        ' NEU: Conversion-Informationen
        Public Property Conversion As String
        Public Property ByteLength As Integer
        Public Property ConversionFactor As Double
        Public Property ConversionOffset As Double

        ' NEU: Stepping (Schrittweite für Wertänderungen)
        Public Property Stepping As String
    End Class

    ''' <summary>
    ''' Hilfsklasse für MQTT-Abfrageergebnisse
    ''' </summary>
    Public Class MqttQueryResult
        Public Property Address As String
        Public Property Value As String
        Public Property Success As Boolean
        Public Property ErrorMessage As String
    End Class
End Namespace
