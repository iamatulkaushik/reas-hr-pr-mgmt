Imports Revolution.Core

''' <summary>The age band for the basic exemption: age on the last day of the financial year (31 March), not the day tax is deducted.</summary>
Public NotInheritable Class TdsAgeBandResolver
    Private Sub New()
    End Sub

    Public Shared Function Resolve(dateOfBirth As Date, financialYear As Integer) As TdsAgeBand
        Dim yearEnd As New Date(financialYear + 1, 3, 31)
        If dateOfBirth > yearEnd Then
            Throw New ArgumentOutOfRangeException(NameOf(dateOfBirth), "Date of birth is after the financial year.")
        End If
        Dim age As Integer = yearEnd.Year - dateOfBirth.Year
        If dateOfBirth.AddYears(age) > yearEnd Then
            age -= 1
        End If
        If age >= 80 Then
            Return TdsAgeBand.SuperSenior
        End If
        Return If(age >= 60, TdsAgeBand.Senior, TdsAgeBand.General)
    End Function
End Class
