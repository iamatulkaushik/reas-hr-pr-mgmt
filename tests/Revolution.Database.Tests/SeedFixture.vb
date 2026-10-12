Imports Microsoft.Data.SqlClient

''' <summary>Two companies, each with a factory and an employee, made once through the real procedures as the app role.</summary>
Public NotInheritable Class SeedFixture
    Public Property CompanyA As Integer
    Public Property CompanyB As Integer
    Public Property FactoryA As Integer
    Public Property FactoryB As Integer
    Public Property EmployeeA As Integer
    Public Property EmployeeB As Integer

    Public Sub New()
        If Not Db.Available Then
            Return
        End If
        Using admin = Db.Open()
            Db.Execute(admin, "IF DATABASE_PRINCIPAL_ID('rv_test_app') IS NULL CREATE USER rv_test_app WITHOUT LOGIN; ALTER ROLE hrms_app ADD MEMBER rv_test_app;")
            Db.Execute(admin, "IF DATABASE_PRINCIPAL_ID('rv_test_reader') IS NULL CREATE USER rv_test_reader WITHOUT LOGIN; GRANT SELECT ON SCHEMA::mst TO rv_test_reader; GRANT SELECT ON SCHEMA::att TO rv_test_reader;")
            Dim tag = Guid.NewGuid().ToString("N").Substring(0, 8)
            CompanyA = CInt(Db.Proc(admin, "mst.usp_Company_Create", ("@CompanyName", "Leak test A " & tag), ("@PlanTierID", CByte(4)))(0))
            CompanyB = CInt(Db.Proc(admin, "mst.usp_Company_Create", ("@CompanyName", "Leak test B " & tag), ("@PlanTierID", CByte(4)))(0))
        End Using
        FactoryA = MakeFactory(CompanyA)
        FactoryB = MakeFactory(CompanyB)
        EmployeeA = MakeEmployee(CompanyA, FactoryA, "Anita A", Nothing)
        EmployeeB = MakeEmployee(CompanyB, FactoryB, "Bharat B", Nothing)
    End Sub

    Public Function MakeFactory(companyId As Integer) As Integer
        Using conn = Db.Open(companyId, runAs:="rv_test_app")
            Return CInt(Db.Proc(conn, "mst.usp_Factory_Save", ("@FactoryCode", "F1"), ("@FactoryName", "Main works"), ("@StateID", CByte(6)))(0))
        End Using
    End Function

    ''' <summary>Creates an employee through mst.usp_Employee_Create. A Nothing code takes the next number for the company.</summary>
    Public Function MakeEmployee(companyId As Integer, factoryId As Integer, fullName As String, empCode As String) As Integer
        Using conn = Db.Open(companyId, runAs:="rv_test_app")
            Return CInt(Db.Proc(conn, "mst.usp_Employee_Create", ("@EmpCode", empCode), ("@FullName", fullName), ("@Gender", "M"),
                                ("@DateOfBirth", New Date(1990, 1, 1)), ("@JoinDate", New Date(2020, 4, 1)), ("@EmploymentTypeID", CByte(1)),
                                ("@PayTypeID", CByte(2)), ("@FactoryID", factoryId))(0))
        End Using
    End Function
End Class
