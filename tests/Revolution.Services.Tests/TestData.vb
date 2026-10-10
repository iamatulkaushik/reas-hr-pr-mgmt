Imports Revolution.Core
Imports Revolution.Services

Public NotInheritable Class TestData
    Private Shared ReadOnly Hasher As New PasswordHasher()
    Private Shared _nextId As Integer = 1

    Private Sub New()
    End Sub

    Public Shared Function User(userName As String, password As String, role As UserRole, companyId As Integer?,
                                Optional mustChange As Boolean = False, Optional lockedUntilUtc As DateTime? = Nothing) As UserRecord
        Dim h = Hasher.Create(password)
        Dim id = Threading.Interlocked.Increment(_nextId)
        Return New UserRecord(id, userName, companyId, role, True, mustChange, h.Hash, h.Salt, h.Iterations, lockedUntilUtc)
    End Function

    Public Shared Function Session(role As UserRole, plan As PlanTier, Optional mustChange As Boolean = False) As UserSession
        Return New UserSession(100, "tester", role, If(role = UserRole.Developer, 0, 1), plan, mustChange)
    End Function
End Class
