''' <summary>How loan interest is worked out. Same numbers as pay.Loan.InterestMethodID.</summary>
Public Enum InterestMethod
    ''' <summary>Interest on the full principal for the whole term, spread evenly.</summary>
    Flat = 1
    ''' <summary>Interest each month on the balance still owed.</summary>
    Reducing = 2
End Enum
