''' <summary>One earning head a company defines (pay.SalaryComponent). ComponentId is Nothing for a head not saved yet.</summary>
Public NotInheritable Class SalaryComponentItem
    Public Sub New(componentId As Integer?, code As String, name As String, isBasic As Boolean, isDa As Boolean,
                   isOtBase As Boolean, sortOrder As Integer, isActive As Boolean, Optional isHra As Boolean = False)
        Me.IsHra = isHra
        Me.ComponentId = componentId
        Me.Code = code
        Me.Name = name
        Me.IsBasic = isBasic
        Me.IsDa = isDa
        Me.IsOtBase = isOtBase
        Me.SortOrder = sortOrder
        Me.IsActive = isActive
    End Sub

    Public ReadOnly Property ComponentId As Integer?
    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property IsBasic As Boolean
    Public ReadOnly Property IsDa As Boolean
    Public ReadOnly Property IsOtBase As Boolean
    ''' <summary>House rent allowance head (CR 0019). Income tax reads HRA received from it. Never Basic or DA at the same time.</summary>
    Public ReadOnly Property IsHra As Boolean
    Public ReadOnly Property SortOrder As Integer
    Public ReadOnly Property IsActive As Boolean
End Class
