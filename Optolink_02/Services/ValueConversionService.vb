Imports System.Globalization
Imports System.Text

Namespace Services
    ''' <summary>
    ''' Service für die Konvertierung von MQTT-Rohwerten basierend auf Vitosoft-Conversion-Regeln
    ''' Siehe Documentation/VitosoftXML.md - Conversion Table
    ''' </summary>
    Public NotInheritable Class ValueConversionService
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Konvertiert einen Hex-String basierend auf dem Conversion-Typ
        ''' </summary>
        Public Shared Function ConvertValue(hexValue As String, conversionType As String, Optional factor As Double = 1.0, Optional offset As Double = 0.0) As String
            If String.IsNullOrWhiteSpace(hexValue) Then
                Return ""
            End If

            Try
                Dim conversion = If(conversionType, "NoConversion").Trim()

                Select Case conversion
                    Case "NoConversion"
                        Return hexValue

                    Case "Div2"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue / 2.0).ToString("F1", CultureInfo.InvariantCulture)

                    Case "Div10"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue / 10.0).ToString("F1", CultureInfo.InvariantCulture)

                    Case "Div100"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue / 100.0).ToString("F2", CultureInfo.InvariantCulture)

                    Case "Div1000"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue / 1000.0).ToString("F3", CultureInfo.InvariantCulture)

                    Case "Mult2"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue * 2.0).ToString("F0", CultureInfo.InvariantCulture)

                    Case "Mult5"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue * 5.0).ToString("F0", CultureInfo.InvariantCulture)

                    Case "Mult10"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue * 10.0).ToString("F0", CultureInfo.InvariantCulture)

                    Case "Mult100"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return (numValue * 100.0).ToString("F0", CultureInfo.InvariantCulture)

                    Case "MultOffset"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return ((numValue * factor) + offset).ToString("F2", CultureInfo.InvariantCulture)

                    Case "HexByte2DecimalByte", "HexByte2AsciiByte"
                        Return ParseHexToDecimal(hexValue).ToString(CultureInfo.InvariantCulture)

                    Case "IPAddress"
                        Return ConvertToIPAddress(hexValue)

                    Case "DateBCD", "DateTimeBCD"
                        Return ConvertBCDToDateTime(hexValue)

                    Case "Time53"
                        Return ConvertTime53(hexValue)

                    Case "Sec2Minute"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return Math.Round(numValue / 60.0, 2).ToString("F2", CultureInfo.InvariantCulture)

                    Case "Sec2Hour"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return Math.Round(numValue / 3600.0, 2).ToString("F2", CultureInfo.InvariantCulture)

                    Case "Sec2Day"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return Math.Round(numValue / 86400.0, 2).ToString("F2", CultureInfo.InvariantCulture)

                    Case "Sec2Week"
                        Dim numValue = ParseHexToDecimal(hexValue)
                        Return Math.Round(numValue / 604800.0, 2).ToString("F2", CultureInfo.InvariantCulture)

                    Case "RotateBytes"
                        Return ReverseHexBytes(hexValue)

                    Case "Phone2BCD"
                        Return ConvertPhone2BCD(hexValue)

                    Case Else
                        Return ParseHexToDecimal(hexValue).ToString(CultureInfo.InvariantCulture)
                End Select

            Catch ex As Exception
                Debug.WriteLine($"[CONVERSION ERROR] Type='{conversionType}', Value='{hexValue}', Error: {ex.GetType().Name}: {ex.Message}")
                Return hexValue
            End Try
        End Function

        Private Shared Function ParseHexToDecimal(hexValue As String) As Double
            Try
                Dim cleanHex = hexValue.Replace("0x", "").Replace("0X", "").Replace(" ", "").Trim()

                If String.IsNullOrWhiteSpace(cleanHex) Then
                    Return 0
                End If

                ' WICHTIG: Prüfe ob es sich um ASCII-codierten Text handelt
                If cleanHex.Length > 16 Then
                    Try
                        Dim asciiText = ConvertHexToAscii(cleanHex)
                        Dim numValue As Double
                        If Double.TryParse(asciiText, Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, numValue) Then
                            Debug.WriteLine($"[PARSE ASCII] HexValue='{hexValue}' -> ASCII='{asciiText}' -> Decimal={numValue}")
                            Return numValue
                        End If
                        Debug.WriteLine($"[PARSE ASCII NON-NUMERIC] HexValue='{hexValue}' -> ASCII='{asciiText}' (nicht numerisch)")
                        Return 0
                    Catch
                    End Try
                End If

                ' 2 Bytes (4 Hex-Zeichen)
                If cleanHex.Length = 4 Then
                    Dim lowByte = Convert.ToInt32(cleanHex.Substring(2, 2), 16)
                    Dim highByte = Convert.ToInt32(cleanHex.Substring(0, 2), 16)
                    Dim value = (highByte << 8) Or lowByte

                    If value > 32767 Then
                        value -= 65536
                    End If

                    Return value
                End If

                ' 1 Byte (2 Hex-Zeichen)
                If cleanHex.Length = 2 Then
                    Dim value = Convert.ToInt32(cleanHex, 16)

                    If value > 127 Then
                        value -= 256
                    End If

                    Return value
                End If

                ' 4 Bytes (8 Hex-Zeichen)
                If cleanHex.Length = 8 Then
                    Dim byte0 = Convert.ToInt64(cleanHex.Substring(6, 2), 16)
                    Dim byte1 = Convert.ToInt64(cleanHex.Substring(4, 2), 16)
                    Dim byte2 = Convert.ToInt64(cleanHex.Substring(2, 2), 16)
                    Dim byte3 = Convert.ToInt64(cleanHex.Substring(0, 2), 16)
                    Dim value = (byte3 << 24) Or (byte2 << 16) Or (byte1 << 8) Or byte0

                    If value > 2147483647 Then
                        value -= 4294967296L
                    End If

                    Return value
                End If

                ' 8 Bytes (16 Hex-Zeichen)
                If cleanHex.Length = 16 Then
                    Try
                        Dim uValue = Convert.ToUInt64(cleanHex, 16)
                        If uValue > Long.MaxValue Then
                            Debug.WriteLine($"[PARSE WARNING] Wert zu groß für signed Int64: {hexValue}")
                            Return uValue
                        End If
                        Return uValue
                    Catch ex As OverflowException
                        Debug.WriteLine($"[PARSE OVERFLOW 64bit] HexValue='{hexValue}' ist zu groß")
                        Return 0
                    End Try
                End If

                ' Fallback
                If cleanHex.Length <= 16 Then
                    Try
                        Return Convert.ToUInt64(cleanHex, 16)
                    Catch
                        Debug.WriteLine($"[PARSE ERROR] Unbekanntes Format (Länge={cleanHex.Length}): '{hexValue}'")
                        Return 0
                    End Try
                End If

                Debug.WriteLine($"[PARSE ERROR] Hex-String zu lang (Länge={cleanHex.Length}): '{hexValue}'")
                Return 0

            Catch ex As OverflowException
                Debug.WriteLine($"[PARSE OVERFLOW] HexValue='{hexValue}', Error: {ex.GetType().Name}: {ex.Message}")
                Return 0
            Catch ex As Exception
                Debug.WriteLine($"[PARSE ERROR] HexValue='{hexValue}', Error: {ex.GetType().Name}: {ex.Message}")
                Return 0
            End Try
        End Function

        Private Shared Function ConvertHexToAscii(hexString As String) As String
            Try
                If String.IsNullOrWhiteSpace(hexString) OrElse hexString.Length Mod 2 <> 0 Then
                    Return hexString
                End If

                Dim sb As New StringBuilder()
                For i = 0 To hexString.Length - 1 Step 2
                    Dim hexByte = hexString.Substring(i, 2)
                    Dim byteValue = Convert.ToByte(hexByte, 16)
                    If byteValue >= 32 AndAlso byteValue <= 126 Then
                        Dim unused1 = sb.Append(Convert.ToChar(byteValue))
                    Else
                        Dim unused = sb.Append("?"c)
                    End If
                Next
                Return sb.ToString()
            Catch
                Return hexString
            End Try
        End Function

        Private Shared Function ConvertToIPAddress(hexValue As String) As String
            Try
                Dim cleanHex = hexValue.Replace("0x", "").Replace(" ", "").Trim()
                If cleanHex.Length <> 8 Then Return hexValue

                Dim b3 = Convert.ToInt32(cleanHex.Substring(0, 2), 16)
                Dim b2 = Convert.ToInt32(cleanHex.Substring(2, 2), 16)
                Dim b1 = Convert.ToInt32(cleanHex.Substring(4, 2), 16)
                Dim b0 = Convert.ToInt32(cleanHex.Substring(6, 2), 16)
                Return $"{b0}.{b1}.{b2}.{b3}"
            Catch
                Return hexValue
            End Try
        End Function

        Private Shared Function ConvertBCDToDateTime(hexValue As String) As String
            Try
                Dim cleanHex = hexValue.Replace("0x", "").Replace(" ", "").Trim()
                If cleanHex.Length < 16 Then Return hexValue

                Dim year = BCDToDec(cleanHex.Substring(0, 4))
                Dim month = BCDToDec(cleanHex.Substring(4, 2))
                Dim day = BCDToDec(cleanHex.Substring(6, 2))
                Dim hour = BCDToDec(cleanHex.Substring(10, 2))
                Dim minute = BCDToDec(cleanHex.Substring(12, 2))
                Dim second = BCDToDec(cleanHex.Substring(14, 2))

                Dim dt As New DateTime(year, month, day, hour, minute, second)
                Return dt.ToString("yyyy-MM-dd HH:mm:ss")
            Catch
                Return hexValue
            End Try
        End Function

        Private Shared Function BCDToDec(bcdHex As String) As Integer
            Try
                Dim result = 0
                For i = 0 To bcdHex.Length - 1
                    Dim digit = Convert.ToInt32(bcdHex.Substring(i, 1), 16)
                    result = (result * 10) + digit
                Next
                Return result
            Catch
                Return 0
            End Try
        End Function

        Private Shared Function ConvertTime53(hexValue As String) As String
            Try
                Dim cleanHex = hexValue.Replace("0x", "").Replace(" ", "").Trim()
                If cleanHex.Length < 2 Then Return hexValue

                Dim byteValue = Convert.ToInt32(cleanHex.Substring(cleanHex.Length - 2, 2), 16)
                If byteValue = &HFF Then Return ""

                Dim hours = byteValue >> 3
                Dim minutes = (byteValue And 7) * 10
                Return $"{hours:D2}:{minutes:D2}"
            Catch
                Return hexValue
            End Try
        End Function

        Private Shared Function ReverseHexBytes(hexValue As String) As String
            Try
                Dim cleanHex = hexValue.Replace("0x", "").Replace(" ", "").Trim()
                If cleanHex.Length Mod 2 <> 0 Then
                    Return hexValue
                End If

                Dim sb As New StringBuilder()
                For i = cleanHex.Length - 2 To 0 Step -2
                    Dim unused = sb.Append(cleanHex.AsSpan(i, 2))
                Next

                Return "0x" & sb.ToString()
            Catch
                Return hexValue
            End Try
        End Function

        Private Shared Function ConvertPhone2BCD(hexValue As String) As String
            Try
                Dim cleanHex = hexValue.Replace("0x", "").Replace(" ", "").Trim()
                Dim sb As New StringBuilder()

                For i = 0 To cleanHex.Length - 1
                    Dim digit = Convert.ToInt32(cleanHex.Substring(i, 1), 16)
                    If digit <= 9 Then
                        Dim unused = sb.Append(digit)
                    End If
                Next

                Return sb.ToString()
            Catch
                Return hexValue
            End Try
        End Function
    End Class
End Namespace
