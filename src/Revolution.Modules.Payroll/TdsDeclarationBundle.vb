''' <summary>A declaration with everything under it. Prev is Nothing when there is no previous employer.</summary>
Public NotInheritable Class TdsDeclarationBundle
    Public Sub New(header As TdsDeclarationHeader, items As IReadOnlyList(Of TdsItem), rents As IReadOnlyList(Of TdsRentView), prev As TdsPrevEmployer)
        Me.Header = header
        Me.Items = items
        Me.Rents = rents
        Me.Prev = prev
    End Sub

    Public ReadOnly Property Header As TdsDeclarationHeader
    Public ReadOnly Property Items As IReadOnlyList(Of TdsItem)
    Public ReadOnly Property Rents As IReadOnlyList(Of TdsRentView)
    Public ReadOnly Property Prev As TdsPrevEmployer
End Class
