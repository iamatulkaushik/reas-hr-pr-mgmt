''' <summary>Limits for a file import or a grid entry (ADR 0025). They keep one import to a size the screen and the database handle comfortably.</summary>
Public NotInheritable Class ImportLimits
    Private Sub New()
    End Sub

    ''' <summary>Largest file read, in bytes (5 MB).</summary>
    Public Const MaxFileBytes As Long = 5L * 1024L * 1024L
    ''' <summary>Most data rows in one import or one grid.</summary>
    Public Const MaxRows As Integer = 2000
    ''' <summary>Most columns in a template or a file.</summary>
    Public Const MaxColumns As Integer = 60
End Class
