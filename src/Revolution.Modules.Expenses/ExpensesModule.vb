Imports Revolution.Data

''' <summary>Composition class of the Expenses module (ADR 0015). Revolution.App calls ExpensesModule.Compose(services.Host) once, when the screens are wired (Phase 9 step 0603).</summary>
Public NotInheritable Class ExpensesModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As ExpensesServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Return New ExpensesServices(New ExpenseService(New SqlExpenseRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session, Function() Date.Today))
    End Function
End Class
