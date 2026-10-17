''' <summary>The seven skill categories of a designation (mst.Designation.CategoryID). Names are for display only.</summary>
Public NotInheritable Class DesignationCategories
    Private Shared ReadOnly _all As IReadOnlyList(Of KeyValuePair(Of Integer, String)) = New List(Of KeyValuePair(Of Integer, String)) From {
        New KeyValuePair(Of Integer, String)(1, "Unskilled"),
        New KeyValuePair(Of Integer, String)(2, "Semi-skilled"),
        New KeyValuePair(Of Integer, String)(3, "Skilled"),
        New KeyValuePair(Of Integer, String)(4, "Highly skilled"),
        New KeyValuePair(Of Integer, String)(5, "Clerical"),
        New KeyValuePair(Of Integer, String)(6, "Supervisory"),
        New KeyValuePair(Of Integer, String)(7, "Managerial")}

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property All As IReadOnlyList(Of KeyValuePair(Of Integer, String))
        Get
            Return _all
        End Get
    End Property

    Public Shared Function DisplayName(categoryId As Integer) As String
        For Each pair In _all
            If pair.Key = categoryId Then Return pair.Value
        Next
        Return String.Empty
    End Function
End Class
