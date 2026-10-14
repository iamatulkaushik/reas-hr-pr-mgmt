Imports Revolution.Core

''' <summary>
''' Builds the installment schedule of a loan. Everything is in whole rupees, nearest rupee.
''' Flat: total interest is principal times yearly rate times months over 12. Total payable is split evenly. The last installment takes the remainder.
''' Reducing: EMI by the standard formula, interest each month on the balance, the last installment clears the balance.
''' Principal parts always add up to the principal exactly.
''' </summary>
Public NotInheritable Class LoanScheduleCalculator
    Private Sub New()
    End Sub

    Public Shared Function Build(principal As Decimal, annualInterestPct As Decimal, installments As Integer, method As InterestMethod, firstDueMonthKey As Integer) As IReadOnlyList(Of LoanInstallmentPlan)
        If principal <= 0D OrElse principal <> Math.Floor(principal) Then Throw New ArgumentOutOfRangeException(NameOf(principal), "Principal must be a whole rupee amount above zero.")
        If annualInterestPct < 0D OrElse annualInterestPct > 100D Then Throw New ArgumentOutOfRangeException(NameOf(annualInterestPct), "Rate must be between 0 and 100.")
        If installments < 1 OrElse installments > 360 Then Throw New ArgumentOutOfRangeException(NameOf(installments), "Installments must be 1 to 360.")
        If firstDueMonthKey < 190001 OrElse firstDueMonthKey > 999912 OrElse firstDueMonthKey Mod 100 < 1 OrElse firstDueMonthKey Mod 100 > 12 Then
            Throw New ArgumentOutOfRangeException(NameOf(firstDueMonthKey), "Month must be YYYYMM.")
        End If
        Select Case method
            Case InterestMethod.Flat
                Return BuildFlat(principal, annualInterestPct, installments, firstDueMonthKey)
            Case InterestMethod.Reducing
                Return BuildReducing(principal, annualInterestPct, installments, firstDueMonthKey)
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(method))
        End Select
    End Function

    Private Shared Function BuildFlat(principal As Decimal, pct As Decimal, n As Integer, first As Integer) As IReadOnlyList(Of LoanInstallmentPlan)
        Dim totalInterest As Decimal = Rupee.Round(principal * pct / 100D * n / 12D, RateRounding.Nearest)
        Dim principalEach As Decimal = Math.Floor(principal / n)
        Dim interestEach As Decimal = Math.Floor(totalInterest / n)
        Dim result As New List(Of LoanInstallmentPlan)
        For i As Integer = 1 To n
            Dim p As Decimal = If(i = n, principal - principalEach * (n - 1), principalEach)
            Dim x As Decimal = If(i = n, totalInterest - interestEach * (n - 1), interestEach)
            result.Add(New LoanInstallmentPlan(i, AddMonths(first, i - 1), p, x))
        Next
        Return result
    End Function

    Private Shared Function BuildReducing(principal As Decimal, pct As Decimal, n As Integer, first As Integer) As IReadOnlyList(Of LoanInstallmentPlan)
        Dim r As Decimal = pct / 1200D
        Dim emi As Decimal
        If r = 0D Then
            emi = principal / n
        Else
            Dim growth As Decimal = 1D
            For k As Integer = 1 To n
                growth *= 1D + r
            Next
            emi = principal * r * growth / (growth - 1D)
        End If
        emi = Rupee.Round(emi, RateRounding.Nearest)
        Dim result As New List(Of LoanInstallmentPlan)
        Dim balance As Decimal = principal
        For i As Integer = 1 To n
            Dim interest As Decimal = Rupee.Round(balance * r, RateRounding.Nearest)
            Dim p As Decimal
            If i = n Then
                p = balance
            Else
                p = Math.Min(Math.Max(emi - interest, 0D), balance)
            End If
            If p = 0D AndAlso interest = 0D Then Exit For
            result.Add(New LoanInstallmentPlan(i, AddMonths(first, i - 1), p, interest))
            balance -= p
        Next
        Return result
    End Function

    Private Shared Function AddMonths(monthKey As Integer, months As Integer) As Integer
        Dim total As Integer = (monthKey \ 100) * 12 + (monthKey Mod 100 - 1) + months
        Return (total \ 12) * 100 + total Mod 12 + 1
    End Function
End Class
