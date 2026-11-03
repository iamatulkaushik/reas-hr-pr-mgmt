''' <summary>Storage for arrears claims (pay.usp_ArrearsClaim_*, usp_ArrearsReport).</summary>
Public Interface IArrearsRepository
    ''' <summary>Stores the claim and its months together. Returns the claim id.</summary>
    Function Add(claim As NewArrearsClaim) As Integer
    Function List(filter As ArrearsFilter) As IReadOnlyList(Of ArrearsClaimRow)
    Function [Get](claimId As Integer) As ArrearsClaimDetail
    Sub Cancel(claimId As Integer, reason As String, userId As Integer?)
    ''' <summary>The open claims' arrear amounts for one wage month, one line per employee (pay.usp_ArrearsEcr_List).</summary>
    Function EcrLines(wageMonth As Date) As IReadOnlyList(Of ArrearsEcrLine)
    ''' <summary>Marks open claims Paid by an arrears run, all or none (pay.usp_ArrearsClaim_Apply).</summary>
    Sub MarkPaid(runId As Integer, claimIds As IReadOnlyCollection(Of Integer))
    ''' <summary>Puts the claims of a run back to Open when the run could not be filled. Only while the run has no lines (pay.usp_ArrearsClaim_Release).</summary>
    Sub Release(runId As Integer)
    Function Report(fromMonth As Date, toMonth As Date) As IReadOnlyList(Of ArrearsReportRow)
End Interface
