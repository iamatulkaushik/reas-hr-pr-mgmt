''' <summary>
''' Checks ESIC sheet rows before upload, per the ESIC template instructions.
''' Codes that need a last working day come from the format row, not from code.
''' </summary>
Public NotInheritable Class EsicValidator
    Public Const MaxReasonCode As Integer = 13
    Public Const OutOfCoverageCode As Integer = 4

    Private Sub New()
    End Sub

    Public Shared Function Validate(rows As IEnumerable(Of EsicRow), lastDayCodes As IReadOnlyCollection(Of Integer), wageMonth As Integer) As IReadOnlyList(Of String)
        If rows Is Nothing Then
            Throw New ArgumentNullException(NameOf(rows))
        End If
        If lastDayCodes Is Nothing Then
            Throw New ArgumentNullException(NameOf(lastDayCodes))
        End If
        Dim errors As New List(Of String)()
        Dim number As Integer = 0
        For Each r As EsicRow In rows
            number += 1
            CheckIdentity(r, number, errors)
            CheckAmounts(r, number, errors)
            CheckReason(r, number, wageMonth, errors)
            CheckLastDay(r, number, lastDayCodes, errors)
        Next
        If number = 0 Then
            errors.Add("ESIC sheet has no rows.")
        End If
        Return errors
    End Function

    Private Shared Sub CheckIdentity(r As EsicRow, number As Integer, errors As List(Of String))
        If r.IpNumber Is Nothing OrElse r.IpNumber.Length <> 10 OrElse Not r.IpNumber.All(AddressOf Char.IsDigit) Then
            errors.Add($"Row {number}: IP number must be 10 digits.")
        End If
        If EcrNameCleaner.Clean(r.IpName).Length = 0 Then
            errors.Add($"Row {number}: IP name is empty.")
        End If
    End Sub

    Private Shared Sub CheckAmounts(r As EsicRow, number As Integer, errors As List(Of String))
        If r.DaysPaid < 0 OrElse r.DaysPaid > 31 Then
            errors.Add($"Row {number}: days must be 0 to 31.")
        End If
        If r.TotalWages < 0D OrElse r.TotalWages <> Decimal.Truncate(r.TotalWages) Then
            errors.Add($"Row {number}: wages must be whole rupees and not negative.")
        End If
        If r.DaysPaid = 0 AndAlso r.TotalWages > 0D Then
            errors.Add($"Row {number}: days are zero but wages are not.")
        End If
    End Sub

    Private Shared Sub CheckReason(r As EsicRow, number As Integer, wageMonth As Integer, errors As List(Of String))
        Dim code As Integer = EffectiveReason(r)
        If code < 0 OrElse code > MaxReasonCode Then
            errors.Add($"Row {number}: reason code must be 0 to {MaxReasonCode}.")
        End If
        If code <> 0 AndAlso r.DaysPaid > 0 AndAlso r.TotalWages > 0D Then
            errors.Add($"Row {number}: reason code must be 0 when wages are paid.")
        End If
        If code = OutOfCoverageCode AndAlso wageMonth <> 4 AndAlso wageMonth <> 10 Then
            errors.Add($"Row {number}: out of coverage is valid only for April or October.")
        End If
    End Sub

    Private Shared Sub CheckLastDay(r As EsicRow, number As Integer, lastDayCodes As IReadOnlyCollection(Of Integer), errors As List(Of String))
        Dim needed As Boolean = r.DaysPaid = 0 AndAlso lastDayCodes.Contains(EffectiveReason(r))
        If needed AndAlso Not r.LastWorkingDay.HasValue Then
            errors.Add($"Row {number}: last working day is required for this reason code.")
        End If
        If Not needed AndAlso r.LastWorkingDay.HasValue Then
            errors.Add($"Row {number}: last working day must be blank.")
        End If
    End Sub

    Public Shared Function EffectiveReason(r As EsicRow) As Integer
        Return r.ReasonCode.GetValueOrDefault(0)
    End Function
End Class
