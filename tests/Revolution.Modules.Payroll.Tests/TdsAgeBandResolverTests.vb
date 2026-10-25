Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Age is taken on 31 March, the last day of the financial year.</summary>
Public Class TdsAgeBandResolverTests
    <Fact>
    Public Sub Turns60OnLastDayOfYear_IsSenior()
        Assert.Equal(TdsAgeBand.Senior, TdsAgeBandResolver.Resolve(New Date(1967, 3, 31), 2026))
    End Sub

    <Fact>
    Public Sub Turns60DayAfterYearEnd_IsGeneral()
        Assert.Equal(TdsAgeBand.General, TdsAgeBandResolver.Resolve(New Date(1967, 4, 1), 2026))
    End Sub

    <Fact>
    Public Sub Turns80InYear_IsSuperSenior()
        Assert.Equal(TdsAgeBand.SuperSenior, TdsAgeBandResolver.Resolve(New Date(1947, 2, 10), 2026))
    End Sub

    <Fact>
    Public Sub LeapDayBirth_Handled()
        Assert.Equal(TdsAgeBand.General, TdsAgeBandResolver.Resolve(New Date(1968, 2, 29), 2026))
    End Sub

    <Fact>
    Public Sub BornAfterYear_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() TdsAgeBandResolver.Resolve(New Date(2027, 4, 1), 2026))
    End Sub
End Class
