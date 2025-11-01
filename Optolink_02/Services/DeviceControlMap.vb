Public Class ControlRule
    Public Property IdentHex As String            ' e.g. 20CB
    Public Property ExtFrom As Integer?           ' SW index from (hex)
    Public Property ExtTo As Integer?             ' SW index to (hex)
    Public Property F0From As Integer?            ' optional F0 from (dec)
    Public Property F0To As Integer?              ' optional F0 to (dec)
    Public Property ResultId As String            ' ID string like VScotHO1_20
End Class

Public Class DeviceControlMap
    Public Shared ReadOnly Rules As New List(Of ControlRule)()

    Shared Sub New()
        ' Data provided by user: ecnsysDeviceIdent mappings
        Add("2032", Nothing, Nothing, Nothing, Nothing, "VBC550S")
        Add("2033", Nothing, Nothing, Nothing, Nothing, "VBC550P")
        Add("2034", Nothing, Nothing, Nothing, Nothing, "Ecotronic")
        Add("2035", Nothing, Nothing, Nothing, Nothing, "Ecotronic_100")
        Add("2043", Nothing, Nothing, Nothing, Nothing, "VBC701")
        Add("2046", Nothing, Nothing, Nothing, Nothing, "VBC700_BW_WW")
        Add("2047", Nothing, Nothing, Nothing, Nothing, "VBC700_AW")
        Add("2048", Nothing, Nothing, Nothing, Nothing, "V200WO1A")
        Add("2049", Nothing, Nothing, Nothing, Nothing, "VBC702_AW")
        Add("204A", Nothing, Nothing, Nothing, Nothing, "VBC702_S")
        Add("204B", Nothing, Nothing, Nothing, Nothing, "CU401B_G")
        Add("204C", Nothing, Nothing, Nothing, Nothing, "CU401B_A")
        Add("204D", Nothing, Nothing, Nothing, Nothing, "CU401B_S")

        ' 2053 ranges
        Add("2053", &H100, &H100, Nothing, Nothing, "GWG_VBEM_00")
        Add("2053", &H103, &H103, Nothing, Nothing, "GWG_VBEM_03")
        Add("2053", &H121, &H121, Nothing, Nothing, "GWG_VBEM_21")
        Add("2053", &H135, &H135, Nothing, Nothing, "GWG_VBEM_35")
        Add("2053", &H136, &H136, Nothing, Nothing, "GWG_VBEM_36")
        Add("2053", &H200, &H200, Nothing, Nothing, "GWG_VBES_00")
        Add("2053", &H203, &H203, Nothing, Nothing, "GWG_VBES_03")
        Add("2053", &H221, &H221, Nothing, Nothing, "GWG_VBES_21")
        Add("2053", &H235, &H235, Nothing, Nothing, "GWG_VBES_35")
        Add("2053", &H236, &H236, Nothing, Nothing, "GWG_VBES_36")
        Add("2053", &H800, &H800, Nothing, Nothing, "GWG_VWMS_00")
        Add("2053", &H803, &H803, Nothing, Nothing, "GWG_VWMS_03")
        Add("2053", &H821, &H821, Nothing, Nothing, "GWG_VWMS_21")
        Add("2053", &H835, &H835, Nothing, Nothing, "GWG_VWMS_35")
        Add("2053", &H836, &H836, Nothing, Nothing, "GWG_VWMS_36")
        Add("2053", &H1000, &H1000, Nothing, Nothing, "GWG_VBT2_00")
        Add("2053", &H1003, &H1003, Nothing, Nothing, "GWG_VBT2_03")
        Add("2053", &H1021, &H1021, Nothing, Nothing, "GWG_VBT2_21")
        Add("2053", &H1035, &H1035, Nothing, Nothing, "GWG_VBT2_35")
        Add("2053", &H1036, &H1036, Nothing, Nothing, "GWG_VBT2_36")

        Add("2054", &HFF, &HFF, Nothing, Nothing, "GWG_BT2")

        ' 208A variants
        Add("208A", Nothing, Nothing, Nothing, Nothing, "Vitocom300")
        Add("208A", &H10A, &H10F, Nothing, Nothing, "Vitocom300_10")
        Add("208A", &H110, &H12F, Nothing, Nothing, "Vitocom300_LAN")

        Add("208C", Nothing, Nothing, Nothing, Nothing, "Vitocom200_LAN")
        Add("208D", Nothing, Nothing, Nothing, Nothing, "Vitocom100_LAN")
        Add("208E", Nothing, Nothing, Nothing, Nothing, "Vitocom200_LAN_GP")
        Add("208F", Nothing, Nothing, Nothing, Nothing, "Vitocom300_LAN_GP_Analog")
        Add("208X", Nothing, Nothing, Nothing, Nothing, "Vitogate200EIB")

        ' 2091
        Add("2091", &H100, &H105, Nothing, Nothing, "V100KC2")
        Add("2091", &H106, &H10F, Nothing, Nothing, "V100KC2_6")

        ' 2092
        Add("2092", &H100, &H103, Nothing, Nothing, "V150KB1")
        Add("2092", &H104, &H104, Nothing, Nothing, "V150KB1_4")
        Add("2092", &H105, &H105, Nothing, Nothing, "V150KB1_5")
        Add("2092", &H106, &H10F, Nothing, Nothing, "V150KB1_6")

        ' 2094
        Add("2094", &H100, &H103, Nothing, Nothing, "V200KW1")
        Add("2094", &H104, &H104, Nothing, Nothing, "V200KW1_4")
        Add("2094", &H105, &H105, Nothing, Nothing, "V200KW1_5")
        Add("2094", &H106, &H10F, Nothing, Nothing, "V200KW1_6")

        ' 2098
        Add("2098", &H100, &H103, Nothing, Nothing, "V200KW2")
        Add("2098", &H104, &H104, Nothing, Nothing, "V200KW2_4")
        Add("2098", &H105, &H105, Nothing, Nothing, "V200KW2_5")
        Add("2098", &H106, &H10F, Nothing, Nothing, "V200KW2_6")

        ' 209C
        Add("209C", &H100, &H103, Nothing, Nothing, "V300KW3")
        Add("209C", &H104, &H104, Nothing, Nothing, "V300KW3_4")
        Add("209C", &H105, &H105, Nothing, Nothing, "V300KW3_5")
        Add("209C", &H106, &H10F, Nothing, Nothing, "V300KW3_6")

        Add("209D", Nothing, Nothing, Nothing, Nothing, "V100KC4B")
        Add("209F", Nothing, Nothing, Nothing, Nothing, "V200KO2B")

        ' 20A0
        Add("20A0", &H100, &H106, Nothing, Nothing, "V100GC1")
        Add("20A0", &H107, &H107, Nothing, Nothing, "V100GC1_7")
        Add("20A0", &H108, &H10F, Nothing, Nothing, "V100GC1_8")

        Add("20A1", Nothing, Nothing, Nothing, Nothing, "V100GC1A")
        Add("20A2", Nothing, Nothing, Nothing, Nothing, "V100GC1C")
        Add("20A3", Nothing, Nothing, Nothing, Nothing, "EA2")

        ' 20A4
        Add("20A4", &H100, &H106, Nothing, Nothing, "V200GW1")
        Add("20A4", &H107, &H107, Nothing, Nothing, "V200GW1_7")
        Add("20A4", &H108, &H108, Nothing, Nothing, "V200GW1_8")
        Add("20A4", &H109, &H10F, Nothing, Nothing, "V200GW1_9")

        ' 20A5
        Add("20A5", &H100, &H106, Nothing, Nothing, "V300GW2")
        Add("20A5", &H107, &H107, Nothing, Nothing, "V300GW2_7")
        Add("20A5", &H108, &H108, Nothing, Nothing, "V300GW2_8")
        Add("20A5", &H109, &H10F, Nothing, Nothing, "V300GW2_9")

        Add("20A6", Nothing, Nothing, Nothing, Nothing, "V300GW2A")
        Add("20A7", Nothing, Nothing, Nothing, Nothing, "V100CC1x")
        Add("20A9", Nothing, Nothing, Nothing, Nothing, "V200CO1x")

        ' 20AA/AB/AC/AD
        Add("20AA", &H100, &H106, Nothing, Nothing, "V050HK1W")
        Add("20AA", &H107, &H107, Nothing, Nothing, "V050HK1W_7")
        Add("20AA", &H108, &H108, Nothing, Nothing, "V050HK1W_8")
        Add("20AA", &H109, &H10F, Nothing, Nothing, "V050HK1W_9")

        Add("20AB", &H100, &H106, Nothing, Nothing, "V050HK3W")
        Add("20AB", &H107, &H107, Nothing, Nothing, "V050HK3W_7")
        Add("20AB", &H108, &H108, Nothing, Nothing, "V050HK3W_8")
        Add("20AB", &H109, &H10F, Nothing, Nothing, "V050HK3W_9")

        Add("20AC", &H100, &H106, Nothing, Nothing, "V050HK1S")
        Add("20AC", &H107, &H107, Nothing, Nothing, "V050HK1S_7")
        Add("20AC", &H108, &H108, Nothing, Nothing, "V050HK1S_8")
        Add("20AC", &H109, &H10F, Nothing, Nothing, "V050HK1S_9")

        Add("20AD", &H100, &H106, Nothing, Nothing, "V050HK3S")
        Add("20AD", &H107, &H107, Nothing, Nothing, "V050HK3S_7")
        Add("20AD", &H108, &H108, Nothing, Nothing, "V050HK3S_8")
        Add("20AD", &H109, &H10F, Nothing, Nothing, "V050HK3S_9")

        Add("20AE", Nothing, Nothing, Nothing, Nothing, "V200HK3A")

        ' 20B4
        Add("20B4", &H100, &H106, Nothing, Nothing, "V050HK1M")
        Add("20B4", &H107, &H107, Nothing, Nothing, "V050HK1M_7")
        Add("20B4", &H108, &H108, Nothing, Nothing, "V050HK1M_8")
        Add("20B4", &H109, &H10F, Nothing, Nothing, "V050HK1M_9")

        ' 20B8/B9
        Add("20B8", &H100, &H106, Nothing, Nothing, "V333MW1")
        Add("20B8", &H107, &H107, Nothing, Nothing, "V333MW1_7")
        Add("20B8", &H108, &H108, Nothing, Nothing, "V333MW1_8")
        Add("20B8", &H109, &H10F, Nothing, Nothing, "V333MW1_9")

        Add("20B9", &H100, &H106, Nothing, Nothing, "V333MW1S")
        Add("20B9", &H107, &H107, Nothing, Nothing, "V333MW1S_7")
        Add("20B9", &H108, &H108, Nothing, Nothing, "V333MW1S_8")
        Add("20B9", &H109, &H10F, Nothing, Nothing, "V333MW1S_9")

        Add("20BA", Nothing, Nothing, Nothing, Nothing, "V333MW2")
        Add("20BA", &H102, &H10F, Nothing, Nothing, "V333MW2_2")

        Add("20BB", Nothing, Nothing, Nothing, Nothing, "V300KMW1A")
        Add("20BC", Nothing, Nothing, Nothing, Nothing, "V300KMW2A")
        Add("20BD", Nothing, Nothing, Nothing, Nothing, "V300MW3B")
        Add("20BE", Nothing, Nothing, Nothing, Nothing, "V300GW6B")
        Add("20BF", Nothing, Nothing, Nothing, Nothing, "V300CM1x")

        ' 20C0..20C9
        Add("20C0", &H100, &H103, Nothing, Nothing, "VDensHC1")
        Add("20C0", &H104, &H19F, Nothing, Nothing, "VDensHC1_4")

        Add("20C1", &H100, &H103, Nothing, Nothing, "VDensHC2")
        Add("20C1", &H104, &H19F, Nothing, Nothing, "VDensHC2_4")

        Add("20C2", &H100, &H103, Nothing, Nothing, "VDensHO1")
        Add("20C2", &H104, &H19F, Nothing, Nothing, "VDensHO1_4")

        Add("20C3", &H100, &H103, Nothing, Nothing, "VPendHC1")
        Add("20C3", &H104, &H113, Nothing, Nothing, "VPendHC1_4")
        Add("20C3", &H128, &H19F, Nothing, Nothing, "VPendHC1_40")

        Add("20C4", &H100, &H103, Nothing, Nothing, "VPendHC2")
        Add("20C4", &H104, &H113, Nothing, Nothing, "VPendHC2_4")
        Add("20C4", &H128, &H19F, Nothing, Nothing, "VPendHC2_40")

        Add("20C5", &H100, &H103, Nothing, Nothing, "VPendHO1")
        Add("20C5", &H104, &H127, Nothing, Nothing, "VPendHO1_4")
        Add("20C5", &H128, &H19F, Nothing, Nothing, "VPendHO1_40")

        Add("20C6", &H100, &H103, Nothing, Nothing, "VPlusHC1")
        Add("20C6", &H104, &H113, Nothing, Nothing, "VPlusHC1_4")
        Add("20C6", &H114, &H127, Nothing, Nothing, "VPlusHC1_20")
        Add("20C6", &H128, &H181, Nothing, Nothing, "VPlusHC1_40")
        Add("20C6", &H182, &H195, Nothing, Nothing, "VPlusHC1_130")

        Add("20C7", &H100, &H103, Nothing, Nothing, "VPlusHC2")
        Add("20C7", &H104, &H113, Nothing, Nothing, "VPlusHC2_4")
        Add("20C7", &H128, &H19F, Nothing, Nothing, "VPlusHC2_40")

        Add("20C8", &H100, &H103, Nothing, Nothing, "VPlusHO1")
        Add("20C8", &H104, &H113, Nothing, Nothing, "VPlusHO1_4")
        Add("20C8", &H114, &H127, Nothing, Nothing, "VPlusHO1_20")
        Add("20C8", &H128, &H19F, Nothing, Nothing, "VPlusHO1_40")

        Add("20C9", &H100, &H103, Nothing, Nothing, "VScotHC1")
        Add("20C9", &H104, &H113, Nothing, Nothing, "VScotHC1_4")
        Add("20C9", &H114, &H127, Nothing, Nothing, "VScotHC1_20")
        Add("20C9", &H128, &H159, Nothing, Nothing, "VScotHC1_40")
        Add("20C9", &H15A, &H181, Nothing, Nothing, "VScotHC1_90")
        Add("20C9", &H182, &H195, Nothing, Nothing, "VCaldens")
        Add("20C9", &H1C8, &H1FF, 0, 9, "VScotHC1_200")
        Add("20C9", &H1C8, &H1FF, 10, 19, "VScotHC1_200_10")
        Add("20C9", &H1C8, &H1FF, 30, 39, "VScotHC1_200_30")

        Add("20CA", &H100, &H103, Nothing, Nothing, "VScotHC2")
        Add("20CA", &H104, &H113, Nothing, Nothing, "VScotHC2_4")
        Add("20CA", &H128, &H19F, Nothing, Nothing, "VScotHC2_40")
        Add("20CA", &H1C8, &H1FF, 11, 19, "VScotHC2_200_11")

        ' 20CB HO1
        Add("20CB", &H100, &H103, Nothing, Nothing, "VScotHO1")
        Add("20CB", &H104, &H113, Nothing, Nothing, "VScotHO1_4")
        Add("20CB", &H114, &H127, Nothing, Nothing, "VScotHO1_20")
        Add("20CB", &H128, &H145, Nothing, Nothing, "VScotHO1_40")
        Add("20CB", &H146, &H147, Nothing, Nothing, "VScotHO1_70")
        Add("20CB", &H148, &H159, Nothing, Nothing, "VScotHO1_72")
        Add("20CB", &H15A, &H181, Nothing, Nothing, "VScotHO1_90")
        Add("20CB", &H182, &H195, Nothing, Nothing, "VCaldens_HO1")
        Add("20CB", &H196, &H1C7, Nothing, Nothing, "VSorp")
        Add("20CB", &H1C8, &H1FF, 0, 0, "VScotHO1_200")
        Add("20CB", &H1C8, &H1FF, 1, 9, "VScotHO1_200_01")
        Add("20CB", &H1C8, &H1FF, 10, 10, "VScotHO1_200_10")
        Add("20CB", &H1C8, &H1FF, 11, 19, "VScotHO1_200_11")
        Add("20CB", &H1C8, &H1FF, 20, 29, "VScotHO1_200_20")
        Add("20CB", &H1C8, &H1FF, 30, 39, "VScotHO1_200_30")

        Add("20E3", Nothing, Nothing, Nothing, Nothing, "Vitovalor")
    End Sub

    Private Shared Sub Add(ident As String,
                           extFrom As Integer?,
                           extTo As Integer?,
                           f0From As Integer?,
                           f0To As Integer?, resultId As String)
        Rules.Add(New ControlRule With {
                  .IdentHex = ident.ToUpperInvariant(),
                  .ExtFrom = extFrom,
                  .ExtTo = extTo,
                  .F0From = f0From,
                  .F0To = f0To,
                  .ResultId = resultId})
    End Sub

    Public Shared Function Match(identity As DeviceIdentity) As List(Of String)
        Dim id = identity.IdentificationHex.ToUpperInvariant()
        Dim sw = Convert.ToInt32(identity.SWIndexHex, 16)
        Dim f0 As Integer? = If(identity.RawF0.HasValue, CInt(identity.RawF0.Value), CType(Nothing, Integer?))
        Dim res As New List(Of String)()
        For Each r In Rules
            If r.IdentHex = id Then
                Dim extOk = True
                If r.ExtFrom.HasValue AndAlso r.ExtTo.HasValue Then
                    extOk = sw >= r.ExtFrom.Value AndAlso sw <= r.ExtTo.Value
                End If
                Dim f0Ok = True
                If r.F0From.HasValue AndAlso r.F0To.HasValue Then
                    f0Ok = f0.HasValue AndAlso f0.Value >= r.F0From.Value AndAlso f0.Value <= r.F0To.Value
                End If
                If extOk AndAlso f0Ok Then res.Add(r.ResultId)
            End If
        Next
        Return res
    End Function

End Class