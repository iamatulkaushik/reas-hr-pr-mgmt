''' <summary>The services of the Arrears module that screens use.</summary>
Public NotInheritable Class ArrearsServices
    Public Sub New(arrears As ArrearsService, prepare As ArrearsPrepareService, Optional ecr As ArrearsEcrService = Nothing)
        If arrears Is Nothing Then Throw New ArgumentNullException(NameOf(arrears))
        If prepare Is Nothing Then Throw New ArgumentNullException(NameOf(prepare))
        Me.Arrears = arrears
        Me.Prepare = prepare
        Me.Ecr = ecr
    End Sub

    Public ReadOnly Property Arrears As ArrearsService
    Public ReadOnly Property Prepare As ArrearsPrepareService
    ''' <summary>Nothing when the module was composed without the Employees export (no UAN release), so the screen hides the arrear ECR button.</summary>
    Public ReadOnly Property Ecr As ArrearsEcrService
End Class
