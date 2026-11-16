''' <summary>The monthly statutory items of a company (ADR 0027). Each is one line of the Monthly Pack.</summary>
Public Enum ComplianceKind
    ''' <summary>Provident fund: the ECR file and the amount to pay.</summary>
    Epf = 1
    ''' <summary>State insurance: the upload sheet and the contribution to pay.</summary>
    Esic = 2
    ''' <summary>Professional tax, one line for each state that levies it.</summary>
    ProfessionalTax = 3
    ''' <summary>Tax deducted from salary: the amount to deposit.</summary>
    Tds = 4
    ''' <summary>Labour welfare fund, in the months its rule falls due (half-yearly or yearly), one line for each state.</summary>
    LabourWelfare = 5
End Enum
