''' <summary>Agent keys (sec.usp_Agent_Issue, _List, _Retire; special build, database/special/biometric 0900). The procedures check that the user is the Owner or the Associate of the company.</summary>
Public Interface IAgentKeyRepository
    Function Issue(label As String) As IssuedAgentKey
    Function List() As IReadOnlyList(Of AgentKeyItem)
    Sub Retire(registrationId As Integer)
End Interface
