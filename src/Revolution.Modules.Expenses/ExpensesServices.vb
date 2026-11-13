''' <summary>The services of the Expenses module that screens use.</summary>
Public NotInheritable Class ExpensesServices
    Public Sub New(expenses As ExpenseService)
        If expenses Is Nothing Then Throw New ArgumentNullException(NameOf(expenses))
        Me.Expenses = expenses
    End Sub

    Public ReadOnly Property Expenses As ExpenseService
End Class
