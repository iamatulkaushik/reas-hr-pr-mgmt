Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Revolution.Core
Imports Revolution.Modules.Employees

''' <summary>
''' Checks a declaration before it is stored. The database repeats the hard rules; these give the Associate a message first.
''' LandlordPanRentThreshold is the yearly rent above which Form 124 needs the landlord PAN. It has no stat table yet, so it is a flagged
''' constant here until CR 0018 (stat.TdsRuleValue) is done; the CA signs the value.
''' </summary>
Public NotInheritable Class TdsDeclarationValidator
    Friend Const LandlordPanRentThreshold As Decimal = 100000D

    Private Shared ReadOnly _tan As New Regex("^[A-Z]{4}[0-9]{5}[A-Z]$", RegexOptions.CultureInvariant Or RegexOptions.Compiled)

    Private Sub New()
    End Sub

    Public Shared Function ValidateItems(items As IReadOnlyList(Of TdsItem)) As OperationResult
        If items Is Nothing Then Return OperationResult.Fail("Items are missing.")
        For Each item As TdsItem In items
            If item.SectionCode.Length = 0 Then Return OperationResult.Fail("Every deduction needs a section.")
            If item.DeclaredAmount < 0D OrElse (item.VerifiedAmount.HasValue AndAlso item.VerifiedAmount.Value < 0D) Then
                Return OperationResult.Fail("A deduction amount cannot be negative.")
            End If
        Next
        Return OperationResult.Ok()
    End Function

    Public Shared Function ValidateRents(rents As IReadOnlyList(Of TdsRentInput), financialYear As Integer) As OperationResult
        If rents Is Nothing Then Return OperationResult.Fail("Rent rows are missing.")
        For Each rent As TdsRentInput In rents
            Dim one As OperationResult = ValidateRent(rent, financialYear)
            If Not one.Succeeded Then Return one
        Next
        For index As Integer = 0 To rents.Count - 1
            For other As Integer = index + 1 To rents.Count - 1
                If rents(index).FromMonthKey <= rents(other).ToMonthKey AndAlso rents(other).FromMonthKey <= rents(index).ToMonthKey Then
                    Return OperationResult.Fail("Rent months overlap.")
                End If
            Next
        Next
        Return CheckLandlordPan(rents)
    End Function

    Public Shared Function ValidatePrevEmployer(prev As TdsPrevEmployer) As OperationResult
        If prev Is Nothing Then Return OperationResult.Ok()
        If prev.EmployerName.Length = 0 Then Return OperationResult.Fail("The previous employer needs a name.")
        If prev.EmployerTan.Length > 0 AndAlso Not _tan.IsMatch(prev.EmployerTan) Then Return OperationResult.Fail("The previous employer TAN is not valid.")
        If prev.SalaryIncome < 0D OrElse prev.Perquisites < 0D OrElse prev.ProvidentFund < 0D OrElse prev.ProfessionalTax < 0D OrElse prev.TaxDeducted < 0D Then
            Return OperationResult.Fail("Previous employer amounts cannot be negative.")
        End If
        Return OperationResult.Ok()
    End Function

    ''' <summary>Months from the first to the last, both included.</summary>
    Public Shared Function MonthCount(fromMonthKey As Integer, toMonthKey As Integer) As Integer
        Return ((toMonthKey \ 100) * 12 + toMonthKey Mod 100) - ((fromMonthKey \ 100) * 12 + fromMonthKey Mod 100) + 1
    End Function

    Private Shared Function ValidateRent(rent As TdsRentInput, financialYear As Integer) As OperationResult
        Dim firstKey As Integer = financialYear * 100 + 4
        Dim lastKey As Integer = (financialYear + 1) * 100 + 3
        If Not IsMonthKey(rent.FromMonthKey) OrElse Not IsMonthKey(rent.ToMonthKey) OrElse rent.FromMonthKey > rent.ToMonthKey Then
            Return OperationResult.Fail("A rent row needs valid from and to months.")
        End If
        If rent.FromMonthKey < firstKey OrElse rent.ToMonthKey > lastKey Then
            Return OperationResult.Fail("Rent months must be inside the financial year, April " & financialYear.ToString(CultureInfo.InvariantCulture) & " to March " & (financialYear + 1).ToString(CultureInfo.InvariantCulture) & ".")
        End If
        If rent.MonthlyRent <= 0D Then Return OperationResult.Fail("Rent must be more than zero.")
        If rent.City.Length = 0 OrElse rent.LandlordName.Length = 0 OrElse rent.LandlordRelationship.Length = 0 Then
            Return OperationResult.Fail("A rent row needs the city, the landlord name and the relationship.")
        End If
        If Not String.IsNullOrWhiteSpace(rent.LandlordPan) AndAlso Not PanValidator.IsValid(rent.LandlordPan) Then
            Return OperationResult.Fail("The landlord PAN is not valid.")
        End If
        Return OperationResult.Ok()
    End Function

    Private Shared Function CheckLandlordPan(rents As IReadOnlyList(Of TdsRentInput)) As OperationResult
        Dim yearly As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        For Each rent As TdsRentInput In rents
            yearly(rent.LandlordName) = yearly.GetValueOrDefault(rent.LandlordName) + rent.MonthlyRent * MonthCount(rent.FromMonthKey, rent.ToMonthKey)
        Next
        For Each rent As TdsRentInput In rents
            If yearly(rent.LandlordName) > LandlordPanRentThreshold AndAlso String.IsNullOrWhiteSpace(rent.LandlordPan) Then
                Return OperationResult.Fail("The landlord PAN is needed when yearly rent to one landlord is above " & LandlordPanRentThreshold.ToString("N0", CultureInfo.InvariantCulture) & ".")
            End If
        Next
        Return OperationResult.Ok()
    End Function

    Private Shared Function IsMonthKey(key As Integer) As Boolean
        Return key >= 190001 AndAlso key <= 999912 AndAlso key Mod 100 >= 1 AndAlso key Mod 100 <= 12
    End Function
End Class
