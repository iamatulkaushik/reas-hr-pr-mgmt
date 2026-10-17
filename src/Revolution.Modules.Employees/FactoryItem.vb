''' <summary>A factory (work location) of the company. State drives professional tax and state labour limits.</summary>
Public NotInheritable Class FactoryItem
    Public Property FactoryId As Integer
    Public Property Code As String = String.Empty
    Public Property Name As String = String.Empty
    Public Property AddressLine1 As String = String.Empty
    Public Property AddressLine2 As String = String.Empty
    Public Property City As String = String.Empty
    Public Property StateId As Integer
    Public Property LicenceNo As String = String.Empty
    Public Property LicenceValidTo As Date?
    Public Property MaxWorkers As Integer?
    Public Property ManagerName As String = String.Empty
    Public Property EpfSubCode As String = String.Empty
    Public Property EsiSubCode As String = String.Empty
    Public Property IsActive As Boolean = True
End Class
