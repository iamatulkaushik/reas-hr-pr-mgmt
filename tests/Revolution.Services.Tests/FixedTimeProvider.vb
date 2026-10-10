Public NotInheritable Class FixedTimeProvider
    Inherits TimeProvider

    Private ReadOnly _now As DateTimeOffset

    Public Sub New(now As DateTimeOffset)
        _now = now
    End Sub

    Public Overrides Function GetUtcNow() As DateTimeOffset
        Return _now
    End Function
End Class
