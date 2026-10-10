Imports Revolution.Core
Imports Revolution.Services

Public NotInheritable Class AuthFixture
    Public ReadOnly Property Users As New FakeUserRepository()
    Public ReadOnly Property Companies As New FakeCompanyRepository()
    Public ReadOnly Property AuditRows As New FakeAuditRepository()
    Public ReadOnly Property Session As New SessionContext()
    Public ReadOnly Property Time As New FixedTimeProvider(New DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero))
    Public ReadOnly Property Service As AuthService

    Public Sub New()
        Dim entitlements As New EntitlementService(Session)
        Dim audit As New AuditService(AuditRows, Session, entitlements)
        Service = New AuthService(Users, Companies, New PasswordHasher(), Session, audit, Time)
    End Sub
End Class
