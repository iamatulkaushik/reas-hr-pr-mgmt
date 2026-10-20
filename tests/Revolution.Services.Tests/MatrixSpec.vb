Imports Revolution.Core

''' <summary>
''' The intended role x module x action matrix, written out by hand and independent of the seed scripts (ADR 0003, 0012, 0013) and the
''' plan x feature split (ADR 0003). If a business rule changes, change this file and add a new migration in the same pull request:
''' RolePlanLicenceMatrixTests fails until both agree.
''' </summary>
Friend NotInheritable Class MatrixSpec
    Private Const View As PermissionAction = PermissionAction.View
    Private Const Create As PermissionAction = PermissionAction.Create
    Private Const Edit As PermissionAction = PermissionAction.Edit
    Private Const Delete As PermissionAction = PermissionAction.Delete
    Private Const Approve As PermissionAction = PermissionAction.Approve
    Private Const Export As PermissionAction = PermissionAction.Export
    Private Const Reopen As PermissionAction = PermissionAction.Reopen
    Private Const Reveal As PermissionAction = PermissionAction.Reveal

    Private Sub New()
    End Sub

    ''' <summary>Every granted role/module/action as "role/module/action" numbers. Anything not in the set is denied.</summary>
    Friend Shared ReadOnly Grants As HashSet(Of String) = BuildGrants()

    Friend Shared Function Key(role As UserRole, moduleKey As ModuleKey, action As PermissionAction) As String
        Return KeyOf(CInt(role), CInt(moduleKey), CInt(action))
    End Function

    Friend Shared Function KeyOf(roleId As Integer, moduleId As Integer, actionId As Integer) As String
        Return $"{roleId}/{moduleId}/{actionId}"
    End Function

    ''' <summary>Turns "3/4/5" back into "Associate Payroll Approve" for a readable failure message.</summary>
    Friend Shared Function Describe(key As String) As String
        Dim parts = key.Split("/"c)
        Return $"{CType(CInt(parts(0)), UserRole)} {CType(CInt(parts(1)), ModuleKey)} {CType(CInt(parts(2)), PermissionAction)}"
    End Function

    ''' <summary>Features a plan includes, cumulative: each plan includes everything below it (ADR 0003).</summary>
    Friend Shared Function FeaturesOf(plan As PlanTier) As HashSet(Of PlanFeature)
        Dim features As New HashSet(Of PlanFeature)({PlanFeature.Employees, PlanFeature.Attendance, PlanFeature.BasicPayroll, PlanFeature.Payslips})
        If plan >= PlanTier.Silver Then
            features.UnionWith({PlanFeature.EpfEsi, PlanFeature.ProfessionalTax, PlanFeature.Loans, PlanFeature.BankFiles})
        End If
        If plan >= PlanTier.Gold Then
            features.UnionWith({PlanFeature.Tds, PlanFeature.Form16And24Q, PlanFeature.Arrears, PlanFeature.FullAndFinal,
                                PlanFeature.Biometric, PlanFeature.ShiftOtLop})
        End If
        If plan >= PlanTier.Platinum Then
            features.UnionWith({PlanFeature.DualApproval, PlanFeature.FullAuditTrail, PlanFeature.AssetsAndExpenses, PlanFeature.MultiCompany})
        End If
        Return features
    End Function

    Private Shared Function BuildGrants() As HashSet(Of String)
        Dim grants As New HashSet(Of String)()
        AddDeveloper(grants)
        AddOwner(grants)
        AddAssociate(grants)
        AddOperator(grants)
        Return grants
    End Function

    Private Shared Sub Add(grants As HashSet(Of String), role As UserRole, moduleKey As ModuleKey, ParamArray actions() As PermissionAction)
        For Each action In actions
            grants.Add(Key(role, moduleKey, action))
        Next
    End Sub

    ''' <summary>Developer: licence, user setup, diagnostics. No employer data.</summary>
    Private Shared Sub AddDeveloper(grants As HashSet(Of String))
        Add(grants, UserRole.Developer, ModuleKey.Security, View, Create, Edit)
        Add(grants, UserRole.Developer, ModuleKey.Licence, View, Create, Edit)
        Add(grants, UserRole.Developer, ModuleKey.Audit, View, Export)
    End Sub

    ''' <summary>Owner: everything for the employer except licence assignment; final approver; the only role that deletes or reopens.</summary>
    Private Shared Sub AddOwner(grants As HashSet(Of String))
        Dim role = UserRole.Owner
        Add(grants, role, ModuleKey.Masters, View, Create, Edit, Delete, Approve, Export)
        Add(grants, role, ModuleKey.Employees, View, Create, Edit, Delete, Approve, Export, Reveal)
        Add(grants, role, ModuleKey.Attendance, View, Create, Edit, Delete, Approve, Export, Reopen)
        Add(grants, role, ModuleKey.Payroll, View, Create, Edit, Delete, Approve, Export, Reopen)
        Add(grants, role, ModuleKey.Statutory, View, Create, Edit, Delete, Approve, Export)
        Add(grants, role, ModuleKey.Reports, View, Export)
        Add(grants, role, ModuleKey.Biometric, View, Create, Edit)
        Add(grants, role, ModuleKey.Assets, View, Create, Edit, Delete, Approve, Export)
        Add(grants, role, ModuleKey.Expenses, View, Create, Edit, Delete, Approve, Export)
        Add(grants, role, ModuleKey.Security, View, Create, Edit)
        Add(grants, role, ModuleKey.Audit, View, Export)
    End Sub

    ''' <summary>Associate: prepares payroll and filings; second approver on Payroll only; creates companies and Owner logins; never deletes or reopens.</summary>
    Private Shared Sub AddAssociate(grants As HashSet(Of String))
        Dim role = UserRole.Associate
        Add(grants, role, ModuleKey.Masters, View, Create, Edit)
        Add(grants, role, ModuleKey.Employees, View, Create, Edit, Export, Reveal)
        Add(grants, role, ModuleKey.Attendance, View, Create, Edit, Export)
        Add(grants, role, ModuleKey.Payroll, View, Create, Edit, Approve, Export)
        Add(grants, role, ModuleKey.Statutory, View, Create, Edit, Export)
        Add(grants, role, ModuleKey.Reports, View, Export)
        Add(grants, role, ModuleKey.Biometric, View, Create, Edit)
        Add(grants, role, ModuleKey.Assets, View, Create, Edit)
        Add(grants, role, ModuleKey.Expenses, View, Create, Edit, Export)
        Add(grants, role, ModuleKey.Security, View, Create)
    End Sub

    ''' <summary>Operator: data entry only.</summary>
    Private Shared Sub AddOperator(grants As HashSet(Of String))
        Dim role = UserRole.Operator
        Add(grants, role, ModuleKey.Masters, View)
        Add(grants, role, ModuleKey.Employees, View, Create, Edit)
        Add(grants, role, ModuleKey.Attendance, View, Create, Edit)
        Add(grants, role, ModuleKey.Assets, View)
        Add(grants, role, ModuleKey.Expenses, View, Create)
    End Sub
End Class
