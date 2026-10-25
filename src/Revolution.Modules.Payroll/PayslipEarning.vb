''' <summary>One earning head on a payslip.</summary>
Public NotInheritable Class PayslipEarning
    Public Sub New(code As String, name As String, sortOrder As Integer, amount As Decimal)
        Me.Code = code
        Me.Name = name
        Me.SortOrder = sortOrder
        Me.Amount = amount
    End Sub

    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property SortOrder As Integer
    Public ReadOnly Property Amount As Decimal
End Class
