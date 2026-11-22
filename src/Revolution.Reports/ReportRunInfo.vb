Imports System.Globalization

''' <summary>A payroll run as the report screens list it. The company name and period text feed the report's parameters.</summary>
Public NotInheritable Class ReportRunInfo
    Private Const DatePattern As String = "dd-MM-yyyy"

    Public Sub New(runId As Integer, periodFrom As Date, periodTo As Date, isDraft As Boolean, companyName As String)
        Me.RunId = runId
        Me.PeriodFrom = periodFrom
        Me.PeriodTo = periodTo
        Me.IsDraft = isDraft
        Me.CompanyName = If(companyName, String.Empty)
    End Sub

    Public ReadOnly Property RunId As Integer
    Public ReadOnly Property PeriodFrom As Date
    Public ReadOnly Property PeriodTo As Date
    ''' <summary>True while the run is below Approved: its reports carry the DRAFT mark.</summary>
    Public ReadOnly Property IsDraft As Boolean
    Public ReadOnly Property CompanyName As String

    Public ReadOnly Property PeriodText As String
        Get
            Return PeriodFrom.ToString(DatePattern, CultureInfo.InvariantCulture) & " to " & PeriodTo.ToString(DatePattern, CultureInfo.InvariantCulture)
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return PeriodText & If(IsDraft, " (draft)", String.Empty)
    End Function
End Class
