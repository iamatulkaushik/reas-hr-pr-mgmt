''' <summary>The file an export is for. It decides the permission, the ids released, and the reason in the reveal log (ADR 0019).</summary>
Public Enum ExportKind
    ''' <summary>EPFO ECR text: UAN. Needs Export on Statutory.</summary>
    Ecr = 1

    ''' <summary>ESIC upload sheet: UAN and ESI IP number. Needs Export on Statutory.</summary>
    Esic = 2

    ''' <summary>Bank file: account number. Needs Export on Payroll.</summary>
    BankFile = 3
End Enum
