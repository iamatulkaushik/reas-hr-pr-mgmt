''' <summary>Storage for arrears claims (pay.usp_ArrearsClaim_*, usp_ArrearsReport).</summary>
Public Interface IArrearsRepository
    ''' <summary>Stores the claim and its months together. Returns the claim id.</summary>
    Function Add(claim As NewArrearsClaim) As Integer
    Function List(filter As ArrearsFilter) As IReadOnlyList(Of ArrearsClaimRow)
    Function [Get](claimId As Integer) As ArrearsClaimDetail
    Sub Cancel(claimId As Integer, reason As String, userId As Integer?)
    Function Report(fromMonth As Date, toMonth As Date) As IReadOnlyList(Of ArrearsReportRow)
End Interface
