''' <summary>One report of the Reports menu: its layout file (reports/layouts), the rpt procedure that gives its rows, and what the user chooses first (ADR 0028).</summary>
Public NotInheritable Class ReportEntry
    Public Sub New(key As String, title As String, layoutFile As String, procedure As String, input As ReportInput)
        If String.IsNullOrWhiteSpace(key) Then Throw New ArgumentException("A report needs a key.", NameOf(key))
        If String.IsNullOrWhiteSpace(title) Then Throw New ArgumentException("A report needs a title.", NameOf(title))
        If String.IsNullOrWhiteSpace(layoutFile) Then Throw New ArgumentException("A report needs a layout file.", NameOf(layoutFile))
        If String.IsNullOrWhiteSpace(procedure) Then Throw New ArgumentException("A report needs a procedure.", NameOf(procedure))
        Me.Key = key
        Me.Title = title
        Me.LayoutFile = layoutFile
        Me.Procedure = procedure
        Me.Input = input
    End Sub

    Public ReadOnly Property Key As String
    Public ReadOnly Property Title As String
    Public ReadOnly Property LayoutFile As String
    Public ReadOnly Property Procedure As String
    Public ReadOnly Property Input As ReportInput
End Class
