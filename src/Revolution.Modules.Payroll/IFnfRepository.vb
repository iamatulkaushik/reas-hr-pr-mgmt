''' <summary>Storage for full and final settlements (pay.usp_Fnf_*). The acting user comes from the session, as the procedures read it.</summary>
Public Interface IFnfRepository
    ''' <summary>Stores the lines and moves the settlement to Calculated. Returns the settlement id.</summary>
    Function Save(separationId As Integer, lines As IReadOnlyList(Of FnfLine)) As Integer
    ''' <summary>The settlement for the separation, or Nothing when none is saved yet.</summary>
    Function [Get](separationId As Integer) As FnfSettlementView
    Sub ChangeStatus(fnfId As Integer, newStatus As FnfStatus)
End Interface
