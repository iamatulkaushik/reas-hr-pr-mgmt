''' <summary>
''' The ids printed on a payslip. Either masked (last four characters) or, for an approved run when the user asked for full ids, plain.
''' Plain values live only in the data handed to the report helper: never log them and delete the helper's file after printing (ADR 0019).
''' </summary>
Public NotInheritable Class PayslipIds
    Public Sub New(uan As String, esiIp As String, accountNumber As String, isMasked As Boolean)
        Me.Uan = uan
        Me.EsiIp = esiIp
        Me.AccountNumber = accountNumber
        Me.IsMasked = isMasked
    End Sub

    Public ReadOnly Property Uan As String
    Public ReadOnly Property EsiIp As String
    Public ReadOnly Property AccountNumber As String
    Public ReadOnly Property IsMasked As Boolean
End Class
