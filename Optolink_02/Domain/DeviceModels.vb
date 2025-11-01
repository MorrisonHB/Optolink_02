Imports System.Xml.Serialization

<XmlRoot("DataPointTypes")>
Public Class EcnDataPointTypes
    <XmlElement("DataPointType")>
    Public Property Items As List(Of EcnDataPointType)
End Class

Public Class EcnDataPointType
    <XmlElement("ID")>
    Public Property ID As String

    <XmlElement("Description")>
    Public Property Description As String

    <XmlElement("Identification")>
    Public Property Identification As String ' e.g. 20CB or 208X

    <XmlElement("IdentificationExtension")>
    Public Property IdentificationExtension As String ' e.g. 0100

    <XmlElement("IdentificationExtensionTill")>
    Public Property IdentificationExtensionTill As String

    <XmlElement("F0")>
    Public Property F0 As String

    <XmlElement("F0Till")>
    Public Property F0Till As String
End Class

Public Class DeviceIdentity
    Public Property RawF8 As Byte()
    Public Property RawF0 As Byte?

    Public ReadOnly Property Byte0 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 1, RawF8(0), CByte(0))
        End Get
    End Property

    Public ReadOnly Property Byte1 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 2, RawF8(1), CByte(0))
        End Get
    End Property

    Public ReadOnly Property Byte2 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 3, RawF8(2), CByte(0))
        End Get
    End Property

    Public ReadOnly Property Byte3 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 4, RawF8(3), CByte(0))
        End Get
    End Property

    Public ReadOnly Property Byte4 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 5, RawF8(4), CByte(0))
        End Get
    End Property

    Public ReadOnly Property Byte5 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 6, RawF8(5), CByte(0))
        End Get
    End Property

    Public ReadOnly Property Byte6 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 7, RawF8(6), CByte(0))
        End Get
    End Property

    Public ReadOnly Property Byte7 As Byte
        Get
            Return If(RawF8 IsNot Nothing AndAlso RawF8.Length >= 8, RawF8(7), CByte(0))
        End Get
    End Property

    Public ReadOnly Property IdentificationHex As String
        Get
            ' XML uses 4-hex codes like 20CB. F8 gives [20, CB, ...] -> 20CB
            Return Byte0.ToString("X2") & Byte1.ToString("X2")
        End Get
    End Property

    Public ReadOnly Property ExtensionHex2 As String
        Get
            Return Byte2.ToString("X2") & Byte3.ToString("X2")
        End Get
    End Property

    Public ReadOnly Property SWIndexHex As String
        Get
            ' Many XML IdentificationExtension values map to SW index at F8 bytes 6+7
            Return Byte6.ToString("X2") & Byte7.ToString("X2")
        End Get
    End Property

    Public Overrides Function ToString() As String
        Dim f8 = If(RawF8 Is Nothing, "", BitConverter.ToString(RawF8).Replace("-", " "))
        Dim f0s = If(RawF0.HasValue, RawF0.Value.ToString("X2"), "--")
        Return $"F8: {f8}  |  F0: {f0s}"
    End Function

End Class