''' <summary>The file a run is loaded for. It decides which employees and which ids are released (ADR 0019).</summary>
Public Enum PayrollFileKind
    ''' <summary>EPFO ECR text. EPF members only. UAN is released.</summary>
    Ecr = 1

    ''' <summary>ESIC upload sheet. ESI members only. UAN and ESI IP number are released.</summary>
    Esic = 2

    ''' <summary>Bank file. Employees paid by bank with net pay above zero. Account number is released.</summary>
    BankFile = 3
End Enum
