''' <summary>The arrears report for a range of wage months: the rows and the column totals. Only open claims are in it.</summary>
Public NotInheritable Class ArrearsReport
    Public Sub New(fromMonth As Date, toMonth As Date, rows As IReadOnlyList(Of ArrearsReportRow))
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        Me.FromMonth = fromMonth
        Me.ToMonth = toMonth
        Me.Rows = rows
    End Sub

    Public ReadOnly Property FromMonth As Date
    Public ReadOnly Property ToMonth As Date
    Public ReadOnly Property Rows As IReadOnlyList(Of ArrearsReportRow)

    Public ReadOnly Property EmployeeCount As Integer
        Get
            Return Rows.Select(Function(r As ArrearsReportRow) r.EmployeeId).Distinct().Count()
        End Get
    End Property

    Public ReadOnly Property GrossTotal As Decimal
        Get
            Return Rows.Sum(Function(r As ArrearsReportRow) r.GrossDifference)
        End Get
    End Property

    Public ReadOnly Property EmployeeDeductionTotal As Decimal
        Get
            Return Rows.Sum(Function(r As ArrearsReportRow) r.EmployeeDeductions)
        End Get
    End Property

    Public ReadOnly Property EmployerTotal As Decimal
        Get
            Return Rows.Sum(Function(r As ArrearsReportRow) r.EmployerTotal)
        End Get
    End Property

    Public ReadOnly Property NetBeforeTax As Decimal
        Get
            Return GrossTotal - EmployeeDeductionTotal
        End Get
    End Property
End Class
