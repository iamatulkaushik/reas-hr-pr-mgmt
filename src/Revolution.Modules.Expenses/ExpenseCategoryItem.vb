''' <summary>One expense category with its limits, for lists and pickers. A limit of Nothing means no limit.</summary>
Public NotInheritable Class ExpenseCategoryItem
    Public Sub New(categoryId As Integer, name As String, perClaimLimit As Decimal?, perMonthLimit As Decimal?, isActive As Boolean)
        Me.CategoryId = categoryId
        Me.Name = name
        Me.PerClaimLimit = perClaimLimit
        Me.PerMonthLimit = perMonthLimit
        Me.IsActive = isActive
    End Sub

    Public ReadOnly Property CategoryId As Integer
    Public ReadOnly Property Name As String
    ''' <summary>Most one claim may be, in rupees, or Nothing.</summary>
    Public ReadOnly Property PerClaimLimit As Decimal?
    ''' <summary>Most one employee may claim in this category in a calendar month, in rupees, or Nothing.</summary>
    Public ReadOnly Property PerMonthLimit As Decimal?
    Public ReadOnly Property IsActive As Boolean
End Class
