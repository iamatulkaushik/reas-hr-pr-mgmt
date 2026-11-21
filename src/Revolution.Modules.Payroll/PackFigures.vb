''' <summary>
''' What the Monthly Pack reads from the month's payroll (ADR 0027): the run chosen for the month and, only when that run is approved, locked or paid, the totals.
''' RunId is empty when the month has no run. The totals are zero for a run that is not yet approved.
''' </summary>
Public NotInheritable Class PackFigures
    Public Sub New(companyName As String, runId As Integer?, runStatus As PayrollRunStatus?, periodTo As Date?,
                   epfEmployee As Decimal, epfEmployerEpf As Decimal, epfEmployerEps As Decimal, edli As Decimal, epfAdmin As Decimal, epfCount As Integer,
                   esiEmployee As Decimal, esiEmployer As Decimal, esiCount As Integer, professionalTax As IReadOnlyList(Of PackStateAmount))
        Me.CompanyName = If(companyName, String.Empty)
        Me.RunId = runId
        Me.RunStatus = runStatus
        Me.PeriodTo = periodTo
        Me.EpfEmployee = epfEmployee
        Me.EpfEmployerEpf = epfEmployerEpf
        Me.EpfEmployerEps = epfEmployerEps
        Me.Edli = edli
        Me.EpfAdmin = epfAdmin
        Me.EpfCount = epfCount
        Me.EsiEmployee = esiEmployee
        Me.EsiEmployer = esiEmployer
        Me.EsiCount = esiCount
        Me.ProfessionalTax = If(professionalTax, New List(Of PackStateAmount)())
    End Sub

    Public ReadOnly Property CompanyName As String
    Public ReadOnly Property RunId As Integer?
    Public ReadOnly Property RunStatus As PayrollRunStatus?
    Public ReadOnly Property PeriodTo As Date?
    Public ReadOnly Property EpfEmployee As Decimal
    Public ReadOnly Property EpfEmployerEpf As Decimal
    Public ReadOnly Property EpfEmployerEps As Decimal
    Public ReadOnly Property Edli As Decimal
    Public ReadOnly Property EpfAdmin As Decimal
    Public ReadOnly Property EpfCount As Integer
    Public ReadOnly Property EsiEmployee As Decimal
    Public ReadOnly Property EsiEmployer As Decimal
    Public ReadOnly Property EsiCount As Integer
    Public ReadOnly Property ProfessionalTax As IReadOnlyList(Of PackStateAmount)

    ''' <summary>True when the month has a run that is approved, locked or paid, so its figures may be used.</summary>
    Public ReadOnly Property HasApprovedRun As Boolean
        Get
            Return RunId.HasValue AndAlso RunStatus.HasValue AndAlso RunStatus.Value >= PayrollRunStatus.Approved
        End Get
    End Property

    Public ReadOnly Property EpfTotal As Decimal
        Get
            Return EpfEmployee + EpfEmployerEpf + EpfEmployerEps + Edli + EpfAdmin
        End Get
    End Property

    Public ReadOnly Property EsiTotal As Decimal
        Get
            Return EsiEmployee + EsiEmployer
        End Get
    End Property
End Class
