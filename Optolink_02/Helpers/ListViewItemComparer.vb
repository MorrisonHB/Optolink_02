Imports System.Windows.Forms

''' <summary>
''' Comparer für ListView Sortierung nach Spalten
''' </summary>
Public Class ListViewItemComparer
    Implements IComparer

    Private ReadOnly _column As Integer
    Private ReadOnly _sortOrder As SortOrder

    Public Sub New(column As Integer, sortOrder As SortOrder)
        _column = column
        _sortOrder = sortOrder
    End Sub

    Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Dim itemX = TryCast(x, ListViewItem)
        Dim itemY = TryCast(y, ListViewItem)

        If itemX Is Nothing OrElse itemY Is Nothing Then
            Return 0
        End If

        ' Hole Text aus der jeweiligen Spalte
        Dim textX = If(_column < itemX.SubItems.Count, itemX.SubItems(_column).Text, "")
        Dim textY = If(_column < itemY.SubItems.Count, itemY.SubItems(_column).Text, "")

        ' Versuche numerischen Vergleich (für Werte-Spalte)
        Dim numX As Double
        Dim numY As Double
        Dim isNumericX = Double.TryParse(textX.Replace(",", "."), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, numX)
        Dim isNumericY = Double.TryParse(textY.Replace(",", "."), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, numY)

        Dim result As Integer
        If isNumericX AndAlso isNumericY Then
            ' Numerischer Vergleich
            result = numX.CompareTo(numY)
        Else
            ' String-Vergleich (case-insensitive)
            result = String.Compare(textX, textY, StringComparison.OrdinalIgnoreCase)
        End If

        ' Sortier-Reihenfolge anwenden
        If _sortOrder = SortOrder.Descending Then
            result = -result
        End If

        Return result
    End Function
End Class
