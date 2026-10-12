Imports Revolution.Modules.Employees

Friend NotInheritable Class EmployeeTestData
    Public Const Pan As String = "ABCPE1234F"
    Public Const Aadhaar As String = "234567890124"
    Public Const Uan As String = "100123456789"
    Public Const EsiIp As String = "1234567890"

    Private Sub New()
    End Sub

    Public Shared Function Draft() As EmployeeDraft
        Return New EmployeeDraft With {
            .FullName = "Ravi Kumar", .DateOfBirth = New Date(1995, 4, 12), .JoinDate = New Date(2026, 4, 1),
            .PayType = PayType.MonthlySalary, .Mobile = "9876543210", .Pan = Pan, .Aadhaar = Aadhaar, .Uan = Uan, .EsiIp = EsiIp}
    End Function

    Public Shared Function Placement() As EmployeePlacement
        Return New EmployeePlacement With {.Gender = "M", .EmploymentTypeId = 1, .WorkerTypeId = 1, .FactoryId = 5}
    End Function
End Class
