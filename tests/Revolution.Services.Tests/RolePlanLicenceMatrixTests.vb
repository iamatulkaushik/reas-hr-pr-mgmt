Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>
''' The Phase 2 gate: every role x plan x licence state x module x action combination behaves as documented (ADR 0003, 0012, 0013, 0018).
''' The expected answers come from MatrixSpec, written by hand; the seed scripts are checked against it, and the real services are checked
''' against it. A failure message lists every combination that disagrees.
''' </summary>
Public Class RolePlanLicenceMatrixTests
    Private Shared ReadOnly AllRoles As UserRole() = [Enum].GetValues(Of UserRole)()
    Private Shared ReadOnly AllPlans As PlanTier() = [Enum].GetValues(Of PlanTier)()
    Private Shared ReadOnly AllModules As ModuleKey() = [Enum].GetValues(Of ModuleKey)()
    Private Shared ReadOnly AllActions As PermissionAction() = [Enum].GetValues(Of PermissionAction)()
    Private Shared ReadOnly AllFeatures As PlanFeature() = [Enum].GetValues(Of PlanFeature)()

    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _licence As New FakeLicenceService()
    Private ReadOnly _permissions As PermissionService
    Private ReadOnly _entitlements As EntitlementService

    Public Sub New()
        Dim repository As New FakePermissionRepository()
        For Each role In AllRoles
            For Each moduleKey In AllModules
                For Each action In AllActions
                    If MatrixSpec.Grants.Contains(MatrixSpec.Key(role, moduleKey, action)) Then
                        repository.Grant(role, moduleKey, action)
                    End If
                Next
            Next
        Next
        _permissions = New PermissionService(_session, repository, _licence)
        _entitlements = New EntitlementService(_session, _licence)
    End Sub

    Private Shared Function Licensed(state As LicenceState, plan As PlanTier, modules As ModuleKey()) As LicenceStatus
        Dim grant As New LicenceGrant(Guid.NewGuid(), "k1", "developer", New DateOnly(2026, 10, 1), Guid.NewGuid(), "associate1",
                                      New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), plan, modules, 15, 40)
        Return LicenceStatus.ForLicence(grant, state, 100)
    End Function

    ' ---- The seed scripts match the documented matrix ----

    <Fact>
    Public Sub SeedScripts_GrantExactlyTheDocumentedMatrix()
        Dim seed = SeedGrantReader.ReadGrants()
        Dim missing = MatrixSpec.Grants.Except(seed).Select(Function(key) MatrixSpec.Describe(key)).OrderBy(Function(text) text).ToList()
        Dim unexpected = seed.Except(MatrixSpec.Grants).Select(Function(key) MatrixSpec.Describe(key)).OrderBy(Function(text) text).ToList()
        Assert.True(seed.Count > 0, "No grants were read from the migration scripts.")
        Assert.Empty(missing.Select(Function(text) "documented but not seeded: " & text))
        Assert.Empty(unexpected.Select(Function(text) "seeded but not documented: " & text))
    End Sub

    <Fact>
    Public Sub NoScript_RemovesGrants()
        Assert.Empty(SeedGrantReader.ScriptsThatRemoveGrants())
    End Sub

    ' ---- Role x module x action ----

    <Fact>
    Public Sub EveryRoleModuleAction_FollowsTheMatrix()
        Dim mismatches As New List(Of String)()
        For Each role In AllRoles
            _session.SignIn(TestData.Session(role, PlanTier.Platinum))
            For Each moduleKey In AllModules
                For Each action In AllActions
                    Dim expected = MatrixSpec.Grants.Contains(MatrixSpec.Key(role, moduleKey, action))
                    If _permissions.IsAllowed(moduleKey, action) <> expected Then
                        mismatches.Add($"{role} {moduleKey} {action}: expected {expected}")
                    End If
                Next
            Next
        Next
        Assert.Empty(mismatches)
    End Sub

    <Fact>
    Public Sub MustChangePassword_DeniesTheWholeMatrix_ForEveryRole()
        Dim allowed As New List(Of String)()
        For Each role In AllRoles
            _session.SignIn(TestData.Session(role, PlanTier.Platinum, mustChange:=True))
            For Each moduleKey In AllModules
                For Each action In AllActions
                    If _permissions.IsAllowed(moduleKey, action) Then
                        allowed.Add($"{role} {moduleKey} {action}")
                    End If
                Next
            Next
        Next
        Assert.Empty(allowed)
    End Sub

    ' ---- Rules that must hold whatever else changes ----

    <Fact>
    Public Sub Developer_HasNoEmployerData()
        _session.SignIn(TestData.Session(UserRole.Developer, PlanTier.Platinum))
        Dim employerModules = {ModuleKey.Masters, ModuleKey.Employees, ModuleKey.Attendance, ModuleKey.Payroll, ModuleKey.Statutory,
                               ModuleKey.Reports, ModuleKey.Biometric, ModuleKey.Assets, ModuleKey.Expenses}
        Dim leaks = (From moduleKey In employerModules
                     From action In AllActions
                     Where _permissions.IsAllowed(moduleKey, action)
                     Select $"{moduleKey} {action}").ToList()
        Assert.Empty(leaks)
    End Sub

    <Fact>
    Public Sub OnlyTheOwner_CanDeleteOrReopen()
        Dim offenders As New List(Of String)()
        For Each role In AllRoles.Where(Function(r) r <> UserRole.Owner)
            _session.SignIn(TestData.Session(role, PlanTier.Platinum))
            For Each moduleKey In AllModules
                For Each action In {PermissionAction.Delete, PermissionAction.Reopen}
                    If _permissions.IsAllowed(moduleKey, action) Then
                        offenders.Add($"{role} {moduleKey} {action}")
                    End If
                Next
            Next
        Next
        Assert.Empty(offenders)
    End Sub

    <Fact>
    Public Sub Operator_IsDataEntryOnly()
        _session.SignIn(TestData.Session(UserRole.Operator, PlanTier.Platinum))
        Dim offenders = (From moduleKey In AllModules
                         From action In {PermissionAction.Delete, PermissionAction.Approve, PermissionAction.Export,
                                         PermissionAction.Reopen, PermissionAction.Reveal}
                         Where _permissions.IsAllowed(moduleKey, action)
                         Select $"{moduleKey} {action}").ToList()
        Assert.Empty(offenders)
    End Sub

    <Fact>
    Public Sub Associate_ApprovesOnlyPayroll_AndRevealsOnlyEmployeeIds()
        _session.SignIn(TestData.Session(UserRole.Associate, PlanTier.Platinum))
        Dim approves = AllModules.Where(Function(m) _permissions.IsAllowed(m, PermissionAction.Approve)).ToList()
        Dim reveals = AllModules.Where(Function(m) _permissions.IsAllowed(m, PermissionAction.Reveal)).ToList()
        Assert.Equal({ModuleKey.Payroll}, approves)
        Assert.Equal({ModuleKey.Employees}, reveals)
    End Sub

    <Fact>
    Public Sub OnlyTheDeveloper_TouchesTheLicence()
        Dim offenders As New List(Of String)()
        For Each role In AllRoles.Where(Function(r) r <> UserRole.Developer)
            _session.SignIn(TestData.Session(role, PlanTier.Platinum))
            For Each action In AllActions
                If _permissions.IsAllowed(ModuleKey.Licence, action) Then
                    offenders.Add($"{role} {action}")
                End If
            Next
        Next
        Assert.Empty(offenders)
    End Sub

    ' ---- Plan x feature ----

    <Fact>
    Public Sub EveryFeature_BelongsToAPlan()
        Dim unplaced = AllFeatures.Where(Function(f) Not MatrixSpec.FeaturesOf(PlanTier.Platinum).Contains(f)).ToList()
        Assert.Empty(unplaced)
    End Sub

    <Fact>
    Public Sub EachPlan_IncludesExactlyItsFeatures()
        Dim mismatches As New List(Of String)()
        For Each role In AllRoles
            For Each plan In AllPlans
                _session.SignIn(TestData.Session(role, plan))
                Dim expected = MatrixSpec.FeaturesOf(plan)
                For Each feature In AllFeatures
                    If _entitlements.IsEnabled(feature) <> expected.Contains(feature) Then
                        mismatches.Add($"{role} on {plan}: {feature} expected {expected.Contains(feature)}")
                    End If
                Next
            Next
        Next
        Assert.Empty(mismatches)
    End Sub

    <Fact>
    Public Sub LicencePlan_LowersTheCompanyPlan_AndNeverRaisesIt()
        Dim mismatches As New List(Of String)()
        For Each companyPlan In AllPlans
            For Each licencePlan In AllPlans
                _session.SignIn(TestData.Session(UserRole.Owner, companyPlan))
                _licence.CurrentStatus = Licensed(LicenceState.Valid, licencePlan, AllModules)
                Dim effective = If(licencePlan < companyPlan, licencePlan, companyPlan)
                Dim expected = MatrixSpec.FeaturesOf(effective)
                For Each feature In AllFeatures
                    If _entitlements.IsEnabled(feature) <> expected.Contains(feature) Then
                        mismatches.Add($"company {companyPlan}, licence {licencePlan}: {feature} expected {expected.Contains(feature)}")
                    End If
                Next
            Next
        Next
        Assert.Empty(mismatches)
    End Sub

    ' ---- Licence state x role x module x action ----

    <Fact>
    Public Sub EveryLicenceState_LimitsTheMatrixAsDocumented()
        Dim twoModules = {ModuleKey.Employees, ModuleKey.Attendance}
        Dim cases As New List(Of Tuple(Of String, LicenceStatus, ModuleKey(), Boolean)) From {
            Tuple.Create("not limited", LicenceStatus.NotLimited(), AllModules, False),
            Tuple.Create("valid, all modules", Licensed(LicenceState.Valid, PlanTier.Platinum, AllModules), AllModules, False),
            Tuple.Create("valid, two modules", Licensed(LicenceState.Valid, PlanTier.Platinum, twoModules), twoModules, False),
            Tuple.Create("expiring", Licensed(LicenceState.Expiring, PlanTier.Platinum, AllModules), AllModules, False),
            Tuple.Create("grace", Licensed(LicenceState.Grace, PlanTier.Platinum, AllModules), AllModules, False),
            Tuple.Create("expired", Licensed(LicenceState.Expired, PlanTier.Platinum, AllModules), AllModules, True),
            Tuple.Create("expired, two modules", Licensed(LicenceState.Expired, PlanTier.Platinum, twoModules), twoModules, True),
            Tuple.Create("trial, days left", LicenceStatus.ForTrial(5, LicenceProblem.None, String.Empty), AllModules, False),
            Tuple.Create("trial ended", LicenceStatus.ForTrial(-1, LicenceProblem.None, String.Empty), AllModules, True)}
        Dim mismatches As New List(Of String)()
        For Each licenceCase In cases
            _licence.CurrentStatus = licenceCase.Item2
            For Each role In AllRoles
                _session.SignIn(TestData.Session(role, PlanTier.Platinum))
                For Each moduleKey In AllModules
                    For Each action In AllActions
                        Dim viewOrReveal = (action = PermissionAction.View OrElse action = PermissionAction.Reveal)
                        Dim expected = MatrixSpec.Grants.Contains(MatrixSpec.Key(role, moduleKey, action)) AndAlso
                                       licenceCase.Item3.Contains(moduleKey) AndAlso (Not licenceCase.Item4 OrElse viewOrReveal)
                        If _permissions.IsAllowed(moduleKey, action) <> expected Then
                            mismatches.Add($"{licenceCase.Item1}: {role} {moduleKey} {action} expected {expected}")
                        End If
                    Next
                Next
            Next
        Next
        Assert.Empty(mismatches)
    End Sub

    ' ---- Screens: a menu entry shows only when the role may view it and the plan includes it ----

    <Fact>
    Public Sub MenuEntries_AreVisibleOnlyWhenRoleAndPlanBothAllow()
        Dim entries As New List(Of MenuEntry) From {
            New MenuEntry("m.masters", "Master", "Masters", ModuleKey.Masters),
            New MenuEntry("m.employees", "Employee", "Employees", ModuleKey.Employees, PlanFeature.Employees),
            New MenuEntry("m.attendance", "Attendance", "Attendance", ModuleKey.Attendance, PlanFeature.Attendance),
            New MenuEntry("m.payroll", "Payroll", "Payroll", ModuleKey.Payroll, PlanFeature.BasicPayroll),
            New MenuEntry("m.epf", "Statutory", "EPF and ESI", ModuleKey.Statutory, PlanFeature.EpfEsi),
            New MenuEntry("m.tds", "Statutory", "TDS", ModuleKey.Statutory, PlanFeature.Tds),
            New MenuEntry("m.reports", "Reports", "Reports", ModuleKey.Reports),
            New MenuEntry("m.biometric", "Attendance", "Biometric", ModuleKey.Biometric, PlanFeature.Biometric),
            New MenuEntry("m.assets", "Assets", "Assets", ModuleKey.Assets, PlanFeature.AssetsAndExpenses),
            New MenuEntry("m.expenses", "Assets", "Expenses", ModuleKey.Expenses, PlanFeature.AssetsAndExpenses),
            New MenuEntry("m.security", "Administration", "Users", ModuleKey.Security),
            New MenuEntry("m.licence", "Administration", "Licences", ModuleKey.Licence),
            New MenuEntry("m.audit", "Administration", "Audit trail", ModuleKey.Audit, PlanFeature.FullAuditTrail)}
        Dim mismatches As New List(Of String)()
        For Each role In AllRoles
            For Each plan In AllPlans
                _session.SignIn(TestData.Session(role, plan))
                Dim shown = MenuFilter.Visible(entries, _permissions, _entitlements).Select(Function(entry) entry.EntryKey).ToHashSet()
                For Each entry In entries
                    Dim expected = MatrixSpec.Grants.Contains(MatrixSpec.Key(role, entry.ModuleKey, PermissionAction.View)) AndAlso
                                   (Not entry.RequiredFeature.HasValue OrElse MatrixSpec.FeaturesOf(plan).Contains(entry.RequiredFeature.Value))
                    If shown.Contains(entry.EntryKey) <> expected Then
                        mismatches.Add($"{role} on {plan}: '{entry.Title}' expected visible={expected}")
                    End If
                Next
            Next
        Next
        Assert.Empty(mismatches)
    End Sub
End Class
