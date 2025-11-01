Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Public Module SqlQueries

    ' Globale SQL-Konstante zur Abfrage der Event-Daten
    Public Const SQL_DeviceEventQuery As String =
   "WITH ConditionData AS ( " &
 "  SELECT et.Id AS EventTypeId, " &
    "CASE WHEN COUNT(Expr) = 0 THEN '' " &
  "  ELSE N'HIDDEN:(' + STRING_AGG(CAST(Expr AS NVARCHAR(MAX)), N'') + N')' " &
 "    END AS ConditionText " &
  "  FROM ecnEventType et " &
   "  OUTER APPLY ( " &
 "    SELECT DISTINCT Expr FROM ( " &
 "      /* AND-Gruppen für DIREKTE Event-Conditions (EventTypeIdDest = et.Id) */ " &
    "  SELECT grp.Expr FROM ( " &
   "   SELECT a.Id, N'(' + STRING_AGG(CAST(a.SingleExpr AS NVARCHAR(MAX)), N' AND ') + N')' AS Expr " &
   " FROM ( " &
  " SELECT g.Id, " &
 "  CASE etCond.[Name] " &
  "      WHEN N'(00) Heizkreis-Warmwasserschema' THEN N'00_WWS' " &
        "      WHEN N'(54) Solarregelung' THEN N'54_SR' " &
    "      WHEN N'(7F) Unterscheidung Einfamilienhaus - Mehrparteienhaus' THEN N'7F_EFH' " &
    "      WHEN N'Software-Index des Gerätes' THEN N'SWIdx' " &
        "      WHEN N'SW-Index Solarmodul SM1' THEN N'SWIdxSolarSM1' " &
        "   WHEN N'HW-Index Solarmodul SM1' THEN N'HWIdxSolarSM1' " &
   "      WHEN N'(00) Anlagen-Warmwasserschema' THEN N'00_AnlWWS' " &
     "      WHEN N'(76) Konfiguration Kommunikationsmodul' THEN N'76_KonfKommMod' " &
 "      WHEN N'(76) Kommunikationsmodul' THEN N'76_KommMod' " &
  "      WHEN N'(A0) Kennung Fernbedienung A1M1' THEN N'A0_KennFBA1M1' " &
        "      WHEN N'(A0) Kennung Fernbedienung M2' THEN N'A0_KennFBM2' " &
        "  WHEN N'(A0) Kennung Fernbedienung M3' THEN N'A0_KennFBM3' " &
  "      WHEN N'Fernbedienung Heizkreis A1M1' THEN N'FBA1M1' " &
        "      WHEN N'Fernbedienung Heizkreis M2' THEN N'FBM2' " &
        "  WHEN N'Fernbedienung Heizkreis M3' THEN N'FBM3' " &
        "      WHEN N'(E5) Kennung Pumpe Heizkreis A1' THEN N'E5_KennPumpHzkA1' " &
        "      WHEN N'(E5) Kennung Pumpe Heizkreis M2' THEN N'E5_KennPumpHzkM2' " &
        "      WHEN N'(E5) Kennung Pumpe Heizkreis M3' THEN N'E5_KennPumpHzkM3' " &
        "      WHEN N'Status Raumtemp.-Sensor HK1' THEN N'RaumTempSensHK1' " &
        "      WHEN N'Status Raumtemp.-Sensor HK2' THEN N'RaumTempSensHK2' " &
        "      WHEN N'Status Raumtemp.-Sensor HK3' THEN N'RaumTempSensHK3' " &
  "      WHEN N'(30) Kennung Interne Umwälzpumpe' THEN N'30_KennIntUmwPumpe' " &
"      WHEN N'(91) Zuordnung externe Betriebsarten-umschaltung' THEN N'ZuordExtBetrUmsch' " &
        "      WHEN N'(35) Kennung Anschlusserweiterung EA1' THEN N'35_KennAnschlErwEA1' " &
        "      WHEN N'(5B) Kennung Anschlusserweiterung EA1' THEN N'5B_KennAnschlErwEA1' " &
        "      WHEN N'(32) Kennung Anschlusserweiterung AM1' THEN N'32_KennAnschlErwAM1' " &
        "      ELSE N'""' + etCond.[Name] + N'""' " &
        "    END + " &
 "CASE COALESCE(dc.[Condition], 0) " &
"    WHEN 0 THEN N'=' WHEN 1 THEN N'≠' WHEN 2 THEN N'>' WHEN 3 THEN N'≥' " &
" WHEN 4 THEN N'<' WHEN 5 THEN N'≤' END + " &
 "  N'""' + COALESCE( " &
  "CASE WHEN COALESCE(dc.[Condition], 0) IN (2, 3, 4, 5) THEN CAST(dc.ConditionValue AS nvarchar(100)) END, " &
 "     NULLIF(evIn.EnumReplaceValue, ''), " &
  "  CASE WHEN evIn.[Name] LIKE '@@%' THEN evIn.[Name] END, " &
   "   CAST(evIn.EnumAddressValue AS nvarchar(100)), '') + N'""' " &
  "       AS SingleExpr " &
 "FROM ecnDisplayConditionGroup g " &
  " JOIN ecnDisplayCondition dc ON dc.ConditionGroupId = g.Id " &
    " JOIN ecnEventType etCond ON etCond.Id = dc.EventTypeIdCondition " &
 "   LEFT JOIN ecnEventValueType evIn ON evIn.Id = dc.EventTypeValueCondition " &
 "    WHERE g.EventTypeIdDest = et.Id " &
 "AND g.EventTypeIdDest <> -1 " &
        "  AND g.[Type] = 1 " &
   "        ) a " &
"   GROUP BY a.Id " &
  "  ) grp " &
" UNION ALL " &
"/* OR-Gruppen für DIREKTE Event-Conditions (EventTypeIdDest = et.Id) */ " &
    "  SELECT grp.Expr FROM ( " &
"    SELECT b.Id, N'(' + STRING_AGG(CAST(b.SingleExpr AS NVARCHAR(MAX)), N' OR ') + N')' AS Expr " &
   " FROM ( " &
   "  SELECT g.Id, " &
   "  CASE etCond.[Name] " &
   "      WHEN N'(00) Heizkreis-Warmwasserschema' THEN N'00_WWS' " &
        "      WHEN N'(54) Solarregelung' THEN N'54_SR' " &
        "   WHEN N'(7F) Unterscheidung Einfamilienhaus - Mehrparteienhaus' THEN N'7F_EFH' " &
 "      WHEN N'Software-Index des Gerätes' THEN N'SWIdx' " &
        "  WHEN N'SW-Index Solarmodul SM1' THEN N'SWIdxSolarSM1' " &
        " WHEN N'HW-Index Solarmodul SM1' THEN N'HWIdxSolarSM1' " &
        "   WHEN N'(00) Anlagen-Warmwasserschema' THEN N'00_AnlWWS' " &
        "      WHEN N'(76) Konfiguration Kommunikationsmodul' THEN N'76_KonfKommMod' " &
        "      WHEN N'(76) Kommunikationsmodul' THEN N'76_KommMod' " &
     " WHEN N'(A0) Kennung Fernbedienung A1M1' THEN N'A0_KennFBA1M1' " &
        "      WHEN N'(A0) Kennung Fernbedienung M2' THEN N'A0_KennFBM2' " &
        "      WHEN N'(A0) Kennung Fernbedienung M3' THEN N'A0_KennFBM3' " &
"      WHEN N'Fernbedienung Heizkreis A1M1' THEN N'FBA1M1' " &
   "      WHEN N'Fernbedienung Heizkreis M2' THEN N'FBM2' " &
        "      WHEN N'Fernbedienung Heizkreis M3' THEN N'FBM3' " &
        " WHEN N'(E5) Kennung Pumpe Heizkreis A1' THEN N'E5_KennPumpHzkA1' " &
        "      WHEN N'(E5) Kennung Pumpe Heizkreis M2' THEN N'E5_KennPumpHzkM2' " &
        "      WHEN N'(E5) Kennung Pumpe Heizkreis M3' THEN N'E5_KennPumpHzkM3' " &
        "  WHEN N'Status Raumtemp.-Sensor HK1' THEN N'RaumTempSensHK1' " &
        "    WHEN N'Status Raumtemp.-Sensor HK2' THEN N'RaumTempSensHK2' " &
        "      WHEN N'Status Raumtemp.-Sensor HK3' THEN N'RaumTempSensHK3' " &
        "WHEN N'(30) Kennung Interne Umwälzpumpe' THEN N'30_KennIntUmwPumpe' " &
    "      WHEN N'(91) Zuordnung externe Betriebsarten-umschaltung' THEN N'ZuordExtBetrUmsch' " &
        "WHEN N'(35) Kennung Anschlusserweiterung EA1' THEN N'35_KennAnschlErwEA1' " &
        "      WHEN N'(5B) Kennung Anschlusserweiterung EA1' THEN N'5B_KennAnschlErwEA1' " &
   "  WHEN N'(32) Kennung Anschlusserweiterung AM1' THEN N'32_KennAnschlErwAM1' " &
        "      ELSE N'""' + etCond.[Name] + N'""' " &
      "    END + " &
 " CASE COALESCE(dc.[Condition], 0) " &
     "    WHEN 0 THEN N'=' WHEN 1 THEN N'≠' WHEN 2 THEN N'>' WHEN 3 THEN N'≥' " &
 "    WHEN 4 THEN N'<' WHEN 5 THEN N'≤' END + " &
   "     N'""' + COALESCE( " &
     "  CASE WHEN COALESCE(dc.[Condition], 0) IN (2, 3, 4, 5) THEN CAST(dc.ConditionValue AS nvarchar(100)) END, " &
      " NULLIF(evIn2.EnumReplaceValue, ''), " &
 "    CASE WHEN evIn2.[Name] LIKE '@@%' THEN evIn2.[Name] END, " &
"  CAST(evIn2.EnumAddressValue AS nvarchar(100)), '') + N'""' " &
 " AS SingleExpr " &
   " FROM ecnDisplayConditionGroup g " &
   "   JOIN ecnDisplayCondition dc ON dc.ConditionGroupId = g.Id " &
 " JOIN ecnEventType etCond ON etCond.Id = dc.EventTypeIdCondition " &
  "  LEFT JOIN ecnEventValueType evIn2 ON evIn2.Id = dc.EventTypeValueCondition " &
    "WHERE g.EventTypeIdDest = et.Id " &
" AND g.EventTypeIdDest <> -1 " &
 "   AND g.[Type] = 2 " &
 "   ) b " &
"  GROUP BY b.Id " &
"   ) grp " &
 "  ) AllGroups " &
  "  ) X " &
  "  GROUP BY et.Id " &
   "), " &
      "ChildGroupPerEvent AS ( " &
     "SELECT etgl.EventTypeId, " &
 "    MIN(etgl.EventTypeGroupId) AS ChildGroupId " &
"  FROM ecnEventTypeEventTypeGroupLink etgl " &
 "INNER JOIN ecnEventTypeGroup etg ON etg.Id = etgl.EventTypeGroupId " &
        "WHERE etg.ParentId IS NOT NULL AND etg.ParentId <> -1 " &
   "  GROUP BY etgl.EventTypeId " &
    ") " &
     "SELECT et.Id AS EventTypeId, dp.Address AS Model, " &
  "et.Address AS Parameter, " &
  "SUBSTRING(et.Address, CHARINDEX('~', et.Address) + 1, 6) AS [Address], " &
   "  et.Conversion, " &
"  CASE et.Type WHEN 1 THEN 'R' WHEN 2 THEN 'R/W' WHEN 3 THEN 'W' ELSE '' END AS ReadWrite, " &
  "ete.ByteLength, et.DefaultValue, " &
"  ev.Unit, ev.Stepping, ev.DataType, ev.ValuePrecision, ev.LowerBorder, ev.UpperBorder, " &
   "  STRING_AGG(CAST(ev.EnumAddressValue AS NVARCHAR(MAX)), ',') AS [Values], " &
  "  STRING_AGG(CAST(ev.Description AS NVARCHAR(MAX)), ',') AS [Descriptions], " &
 "  cd.ConditionText AS [Condition], " &
  "  etg.[Name] AS [Gruppe], " &
 "etgParent.[Name] AS [Kategorie], " &
  "  CAST(1.0 AS FLOAT) AS ConversionFactor, " &
   "  CAST(0.0 AS FLOAT) AS ConversionOffset " &
  "FROM ecnDeviceTypeDataPointTypeLink dev " &
  "JOIN ecnDatapointType dp ON dev.DataPointTypeId = dp.Id " &
   "JOIN ecnDataPointTypeEventTypeLink dpe ON dp.Id = dpe.DataPointTypeId " &
   "JOIN ecnEventType et ON et.Id = dpe.EventTypeId " &
     "LEFT JOIN ecnEventTypeEventValueTypeLink etvl ON etvl.EventTypeId = et.Id " &
  "LEFT JOIN ecnEventValueType ev ON etvl.EventValueId = ev.Id " &
"LEFT JOIN [vsmEventTypeExtension] ete ON ete.EventTypeId = et.Id " &
   "LEFT JOIN ConditionData cd ON cd.EventTypeId = et.Id " &
   "INNER JOIN ChildGroupPerEvent cgpe ON cgpe.EventTypeId = et.Id " &
  "INNER JOIN ecnEventTypeGroup etg ON etg.Id = cgpe.ChildGroupId " &
   "LEFT JOIN ecnEventTypeGroup etgParent ON etgParent.Id = etg.ParentId " &
    "WHERE dp.Address = @DeviceName " &
"GROUP BY et.Id, et.Address, dp.Address, " &
      "  SUBSTRING(et.Address, CHARINDEX('~', et.Address) + 1, 6), " &
  "  et.Conversion, " &
  "  CASE et.Type WHEN 1 THEN 'R' WHEN 2 THEN 'R/W' WHEN 3 THEN 'W' ELSE '' END, " &
"  ete.ByteLength, et.DefaultValue, ev.Unit, ev.Stepping, ev.DataType, ev.ValuePrecision, ev.LowerBorder, ev.UpperBorder, " &
   "  cd.ConditionText, etg.[Name], etgParent.[Name]"

    Private ReadOnly ConfigParamArray As String() = {
        "7000", "00", "76", "7010"
    }

    Public Function BuildConnectionString() As String
        Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
        Dim dd As String = TryCast(AppDomain.CurrentDomain.GetData("DataDirectory"), String)
        If String.IsNullOrEmpty(dd) Then dd = baseDir

        Dim candidates As New List(Of String) From {
   Path.Combine(dd, "Datenbank", "ecnViessmann.mdf"),
Path.Combine(baseDir, "Datenbank", "ecnViessmann.mdf"),
   Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Datenbank", "ecnViessmann.mdf")),
  Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Datenbank", "ecnViessmann.mdf"))
       }
        Dim mdfPath As String = candidates.FirstOrDefault(Function(p) File.Exists(p))
        If String.IsNullOrEmpty(mdfPath) Then
            Throw New FileNotFoundException("ecnViessmann.mdf nicht gefunden.", String.Join(" | ", candidates.Select(Function(p) Path.GetFullPath(p))))
        End If
        Dim fullMdf = Path.GetFullPath(mdfPath)
        Dim workingMdf As String = EnsureWorkingCopy(fullMdf)
        Dim dbName As String = $"OptolinkEcn_{ComputeHashHex(workingMdf, 8)}"
    
        ' WICHTIG: Explizite Pool-Konfiguration gegen Connection-Leaks
        Return $"Data Source=(LocalDB)\MSSQLLocalDB;" &
    $"AttachDbFilename={workingMdf};" &
               $"Initial Catalog={dbName};" &
$"Integrated Security=True;" &
        $"Connect Timeout=60;" &      ' Erhöht von 30s auf 60s
         $"Encrypt=False;" &
               $"TrustServerCertificate=True;" &
       $"Pooling=True;" &' Explizit aktivieren
               $"Max Pool Size=50;" &       ' Limit gegen Erschöpfung
     $"Min Pool Size=5"      ' Mindest-Pool für Performance
    End Function

    Private Function EnsureWorkingCopy(srcMdf As String) As String
        Dim srcDir As String = Path.GetDirectoryName(srcMdf)
        Dim workDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Optolink_02", "Datenbank")
        Dim unused = Directory.CreateDirectory(workDir)
        Dim destMdf As String = Path.Combine(workDir, Path.GetFileName(srcMdf))
        Dim srcLdf As String = Path.Combine(srcDir, "ecnViessmann.ldf")
        Dim destLdf As String = Path.Combine(workDir, "ecnViessmann.ldf")

        Try
            If Not File.Exists(destMdf) OrElse File.GetLastWriteTimeUtc(srcMdf) > File.GetLastWriteTimeUtc(destMdf) Then
                File.Copy(srcMdf, destMdf, True)
            End If
        Catch
        End Try

        Try
            If File.Exists(srcLdf) Then
                If Not File.Exists(destLdf) OrElse File.GetLastWriteTimeUtc(srcLdf) > File.GetLastWriteTimeUtc(destLdf) Then
                    File.Copy(srcLdf, destLdf, True)
                End If
            End If
        Catch
        End Try

        Dim strayLog As String = Path.Combine(workDir, "ecnViessmann_log.ldf")
        Try
            If File.Exists(strayLog) Then File.Delete(strayLog)
        Catch
        End Try

        Return destMdf
    End Function

    Private Function ComputeHashHex(text As String, Optional bytes As Integer = 16) As String
        Dim data = Encoding.UTF8.GetBytes(text)
        Dim hash = SHA1.HashData(data)
        Dim take = Math.Min(bytes, hash.Length)
        Dim sb As New StringBuilder(take * 2)
        For i = 0 To take - 1
            Dim unused = sb.Append(hash(i).ToString("x2"))
        Next
        Return sb.ToString()
    End Function

End Module