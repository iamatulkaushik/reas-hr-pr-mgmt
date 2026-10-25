''' <summary>An open loan installment and the employee it is owed by.</summary>
Public NotInheritable Class EmployeeInstallment
    Public Sub New(employeeId As Integer, installment As OpenInstallment)
        If installment Is Nothing Then Throw New ArgumentNullException(NameOf(installment))
        Me.EmployeeId = employeeId
        Me.Installment = installment
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property Installment As OpenInstallment
End Class
