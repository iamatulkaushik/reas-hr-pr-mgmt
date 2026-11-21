''' <summary>
''' The Developer's licence issuing from inside the app (ADR 0018, amended). The private signing key stays in a password-protected file on the
''' Developer's PC: it is read for the moment of signing and never stored in the database or the app.
''' </summary>
Public Interface ILicenceIssueService
    ''' <summary>
    ''' Makes a new signing key pair and saves the private half, protected by the password (at least 12 characters), to a new file.
    ''' An existing file is never overwritten. The result carries the public key line to paste into LicenceKeys.Trusted before a new build.
    ''' </summary>
    Function CreateSigningKey(keyId As String, filePath As String, password As String) As OperationResult(Of SigningKeyInfo)

    ''' <summary>
    ''' Signs a licence for this installation, imports it for the Associate, and saves a backup copy and a log line beside the key file.
    ''' Needs create rights on the Licence module.
    ''' </summary>
    Function Issue(request As LicenceIssueRequest) As OperationResult(Of LicenceIssueResult)
End Interface
