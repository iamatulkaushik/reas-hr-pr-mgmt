''' <summary>Arrears for an employee over one or more wage months. Tax is not included (open decision F).</summary>
Public NotInheritable Class ArrearsResult
    Public Sub New(months As IReadOnlyList(Of ArrearsMonthResult))
        Me.Months = months
    End Sub

    Public ReadOnly Property Months As IReadOnlyList(Of ArrearsMonthResult)

    Public ReadOnly Property GrossTotal As Decimal
        Get
            Return Months.Sum(Function(m As ArrearsMonthResult) m.GrossDifference)
        End Get
    End Property

    Public ReadOnly Property EmployeeDeductionTotal As Decimal
        Get
            Return Months.Sum(Function(m As ArrearsMonthResult) m.EmployeeDeductions)
        End Get
    End Property

    Public ReadOnly Property EmployerContributionTotal As Decimal
        Get
            Return Months.Sum(Function(m As ArrearsMonthResult) m.EmployerEpf + m.EmployerEps + m.Edli + m.Admin + m.EsiEmployer)
        End Get
    End Property

    ''' <summary>Gross arrears less employee EPF and ESI. Tax is taken by the run's tax step.</summary>
    Public ReadOnly Property NetBeforeTax As Decimal
        Get
            Return GrossTotal - EmployeeDeductionTotal
        End Get
    End Property
End Class
