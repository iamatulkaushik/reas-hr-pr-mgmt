''' <summary>A licence that was signed and imported.</summary>
Public NotInheritable Class LicenceIssueResult
    Public Sub New(licenceId As Guid, summary As String, backupFile As String, warning As String)
        Me.LicenceId = licenceId
        Me.Summary = summary
        Me.BackupFile = backupFile
        Me.Warning = warning
    End Sub

    Public ReadOnly Property LicenceId As Guid
    ''' <summary>One line saying what the licence grants.</summary>
    Public ReadOnly Property Summary As String
    ''' <summary>The saved copy of the licence file. Empty when it could not be saved (see Warning).</summary>
    Public ReadOnly Property BackupFile As String
    ''' <summary>Empty when everything worked. Otherwise what did not: the licence is already imported.</summary>
    Public ReadOnly Property Warning As String
End Class
