''' <summary>A payslip ready for the report: the data row, the ids to print, the net pay in words, and whether it must carry a DRAFT mark (run below Approved).</summary>
Public NotInheritable Class Payslip
    Public Sub New(row As PayslipRow, ids As PayslipIds, netPayInWords As String, isDraft As Boolean)
        If row Is Nothing Then Throw New ArgumentNullException(NameOf(row))
        If ids Is Nothing Then Throw New ArgumentNullException(NameOf(ids))
        Me.Row = row
        Me.Ids = ids
        Me.NetPayInWords = netPayInWords
        Me.IsDraft = isDraft
    End Sub

    Public ReadOnly Property Row As PayslipRow
    Public ReadOnly Property Ids As PayslipIds
    Public ReadOnly Property NetPayInWords As String
    Public ReadOnly Property IsDraft As Boolean
End Class
