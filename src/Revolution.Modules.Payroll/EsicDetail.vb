''' <summary>One ESIC row plus the figures for the optional detail sheet. Placement of that sheet is pending (runtime testing).</summary>
Public NotInheritable Class EsicDetail
    Public Sub New(employeeCode As String, row As EsicRow, grossEarnings As Decimal, employeeShare As Decimal, employerShare As Decimal)
        Me.EmployeeCode = employeeCode
        Me.Row = row
        Me.GrossEarnings = grossEarnings
        Me.EmployeeShare = employeeShare
        Me.EmployerShare = employerShare
    End Sub

    Public ReadOnly Property EmployeeCode As String
    Public ReadOnly Property Row As EsicRow
    Public ReadOnly Property GrossEarnings As Decimal
    Public ReadOnly Property EmployeeShare As Decimal
    Public ReadOnly Property EmployerShare As Decimal
End Class
