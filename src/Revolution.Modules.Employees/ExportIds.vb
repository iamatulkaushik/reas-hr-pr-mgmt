''' <summary>
''' Plain ids of one employee for one export. Only the fields the export kind needs are filled; the rest stay empty.
''' Holds plain values: keep it inside the export call and never log it.
''' </summary>
Public NotInheritable Class ExportIds
    Public Property Uan As String = String.Empty
    Public Property EsiIp As String = String.Empty
    Public Property AccountNumber As String = String.Empty
End Class
