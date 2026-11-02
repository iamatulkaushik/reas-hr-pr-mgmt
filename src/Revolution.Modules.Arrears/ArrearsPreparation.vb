Imports Revolution.Modules.Payroll

''' <summary>What an arrears claim would be, before it is saved: one input per wage month and the calculator's answer. Nothing is stored.</summary>
Public NotInheritable Class ArrearsPreparation
    Public Sub New(inputs As IReadOnlyList(Of ArrearsMonthInput), result As ArrearsResult)
        If inputs Is Nothing Then Throw New ArgumentNullException(NameOf(inputs))
        If result Is Nothing Then Throw New ArgumentNullException(NameOf(result))
        Me.Inputs = inputs
        Me.Result = result
    End Sub

    Public ReadOnly Property Inputs As IReadOnlyList(Of ArrearsMonthInput)
    Public ReadOnly Property Result As ArrearsResult

    ''' <summary>False when every month comes to nothing: there is nothing to claim.</summary>
    Public ReadOnly Property HasDifference As Boolean
        Get
            Return Result.Months.Any(Function(m As ArrearsMonthResult) m.GrossDifference <> 0D OrElse m.EmployeeDeductions <> 0D OrElse m.EmployerEpf <> 0D OrElse m.EsiEmployer <> 0D)
        End Get
    End Property
End Class
