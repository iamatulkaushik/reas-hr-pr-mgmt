''' <summary>How an employee is paid, as stored in mst.EmployeeBank.PaymentModeID. Only bank transfer is fixed by the database (it needs an account and an IFSC).</summary>
Public Enum PaymentMode
    BankTransfer = 1
    Cash = 2
    Cheque = 3
End Enum
