''' <summary>One salary credit in a bank batch.</summary>
Public NotInheritable Class BankPayee
    Public Sub New(name As String, accountNumber As String, branchOrIfsc As String, isSameBank As Boolean,
                   amount As Decimal, reference As String, mode As PaymentMode, Optional beneficiaryCode As String = "")
        Me.Name = name
        Me.AccountNumber = accountNumber
        Me.BranchOrIfsc = branchOrIfsc
        Me.IsSameBank = isSameBank
        Me.Amount = amount
        Me.Reference = reference
        Me.Mode = mode
        Me.BeneficiaryCode = If(beneficiaryCode, String.Empty)
    End Sub

    Public ReadOnly Property Name As String
    Public ReadOnly Property AccountNumber As String
    ''' <summary>5 digit branch code for same bank, 11 character IFSC for other bank.</summary>
    Public ReadOnly Property BranchOrIfsc As String
    Public ReadOnly Property IsSameBank As Boolean
    Public ReadOnly Property Amount As Decimal
    ''' <summary>Unique within the batch. Maximum 15 characters.</summary>
    Public ReadOnly Property Reference As String
    Public ReadOnly Property Mode As PaymentMode
    Public ReadOnly Property BeneficiaryCode As String
End Class
