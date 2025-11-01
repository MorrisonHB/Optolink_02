Imports System.Data
Imports System.IO
Imports System.Windows.Forms

Namespace Services
    ''' <summary>
    ''' Service zum Exportieren von DataGridView-Daten in verschiedene Formate
    ''' </summary>
    Public Class DataGridExportService

        ''' <summary>
        ''' Exportiert eine DataGridView in eine XML-Datei
        ''' </summary>
        ''' <param name="grid">Das DataGridView-Control, dessen Daten exportiert werden sollen</param>
   ''' <param name="defaultFileName">Standard-Dateiname für den SaveFileDialog</param>
   ''' <returns>True wenn erfolgreich, sonst False</returns>
      Public Shared Function ExportToXml(grid As DataGridView, Optional defaultFileName As String = "Export.xml") As Boolean
       Try
  ' Prüfe ob Daten vorhanden sind
    If grid Is Nothing OrElse grid.DataSource Is Nothing Then
   MessageBox.Show("Keine Daten zum Exportieren vorhanden.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning)
     Return False
           End If

           ' Hole die DataTable aus der DataSource
          Dim dt As DataTable = Nothing

          If TypeOf grid.DataSource Is DataTable Then
  dt = DirectCast(grid.DataSource, DataTable)
     ElseIf TypeOf grid.DataSource Is BindingSource Then
     Dim bs = DirectCast(grid.DataSource, BindingSource)
          If TypeOf bs.DataSource Is DataTable Then
     dt = DirectCast(bs.DataSource, DataTable)
     End If
                End If

  If dt Is Nothing OrElse dt.Rows.Count = 0 Then
          MessageBox.Show("Keine Daten zum Exportieren vorhanden.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning)
  Return False
                End If

' WICHTIG: Setze TableName, falls nicht vorhanden (benötigt für XML-Serialisierung)
         If String.IsNullOrWhiteSpace(dt.TableName) Then
         dt.TableName = "ViessmannData"
         End If

   ' SaveFileDialog anzeigen
          Using sfd As New SaveFileDialog()
             sfd.Filter = "XML-Dateien (*.xml)|*.xml|Alle Dateien (*.*)|*.*"
     sfd.DefaultExt = "xml"
   sfd.FileName = defaultFileName
              sfd.Title = "XML-Export"
         sfd.OverwritePrompt = True

 If sfd.ShowDialog() = DialogResult.OK Then
          ' Exportiere DataTable als XML
         dt.WriteXml(sfd.FileName, XmlWriteMode.WriteSchema)

             MessageBox.Show($"Export erfolgreich!{Environment.NewLine}{Environment.NewLine}Datei: {sfd.FileName}{Environment.NewLine}Zeilen: {dt.Rows.Count}",
"Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
     Return True
               End If
       End Using

     Return False

            Catch ex As Exception
              MessageBox.Show($"Fehler beim XML-Export: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
     Debug.WriteLine($"[ERROR] DataGridExportService.ExportToXml: {ex}")
      Return False
     End Try
    End Function

        ''' <summary>
   ''' Exportiert eine DataGridView in eine XML-Datei (nur Daten, ohne Schema)
        ''' </summary>
    ''' <param name="grid">Das DataGridView-Control, dessen Daten exportiert werden sollen</param>
        ''' <param name="defaultFileName">Standard-Dateiname für den SaveFileDialog</param>
        ''' <returns>True wenn erfolgreich, sonst False</returns>
 Public Shared Function ExportToXmlDataOnly(grid As DataGridView, Optional defaultFileName As String = "Export.xml") As Boolean
  Try
    ' Prüfe ob Daten vorhanden sind
 If grid Is Nothing OrElse grid.DataSource Is Nothing Then
  MessageBox.Show("Keine Daten zum Exportieren vorhanden.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning)
 Return False
       End If

    ' Hole die DataTable aus der DataSource
   Dim dt As DataTable = Nothing

       If TypeOf grid.DataSource Is DataTable Then
  dt = DirectCast(grid.DataSource, DataTable)
     ElseIf TypeOf grid.DataSource Is BindingSource Then
   Dim bs = DirectCast(grid.DataSource, BindingSource)
           If TypeOf bs.DataSource Is DataTable Then
        dt = DirectCast(bs.DataSource, DataTable)
End If
     End If

      If dt Is Nothing OrElse dt.Rows.Count = 0 Then
  MessageBox.Show("Keine Daten zum Exportieren vorhanden.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning)
   Return False
     End If

            ' WICHTIG: Setze TableName, falls nicht vorhanden (benötigt für XML-Serialisierung)
        If String.IsNullOrWhiteSpace(dt.TableName) Then
          dt.TableName = "ViessmannData"
      End If

     ' SaveFileDialog anzeigen
       Using sfd As New SaveFileDialog()
     sfd.Filter = "XML-Dateien (*.xml)|*.xml|Alle Dateien (*.*)|*.*"
         sfd.DefaultExt = "xml"
        sfd.FileName = defaultFileName
        sfd.Title = "XML-Export (nur Daten)"
     sfd.OverwritePrompt = True

    If sfd.ShowDialog() = DialogResult.OK Then
        ' Exportiere DataTable als XML (nur Daten, kein Schema)
    dt.WriteXml(sfd.FileName, XmlWriteMode.IgnoreSchema)

      MessageBox.Show($"Export erfolgreich!{Environment.NewLine}{Environment.NewLine}Datei: {sfd.FileName}{Environment.NewLine}Zeilen: {dt.Rows.Count}",
  "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
      Return True
         End If
          End Using

            Return False

            Catch ex As Exception
    MessageBox.Show($"Fehler beim XML-Export: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error)
     Debug.WriteLine($"[ERROR] DataGridExportService.ExportToXmlDataOnly: {ex}")
        Return False
     End Try
        End Function

    End Class
End Namespace
