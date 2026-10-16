''' <summary>The installation a licence is bound to and the day it was created (ADR 0018). The day starts the 28-day trial.</summary>
Public NotInheritable Class InstallationInfo
    Public Sub New(id As Guid, createdOn As DateOnly)
        Me.Id = id
        Me.CreatedOn = createdOn
    End Sub

    Public ReadOnly Property Id As Guid
    Public ReadOnly Property CreatedOn As DateOnly
End Class
