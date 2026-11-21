''' <summary>The answer of stat.usp_DueDate_Resolve: the date when a verified monthly rule covers the month, otherwise whether a rule exists at all.</summary>
Public NotInheritable Class PackDue
    Public Sub New(dueDate As Date?, hasRule As Boolean, isVerified As Boolean)
        Me.DueDate = dueDate
        Me.HasRule = hasRule
        Me.IsVerified = isVerified
    End Sub

    Public ReadOnly Property DueDate As Date?
    Public ReadOnly Property HasRule As Boolean
    Public ReadOnly Property IsVerified As Boolean
End Class
