Imports Revolution.Core

''' <summary>
''' Adds the claims under each section and sub-limit and caps the total at the stat.TdsDeductionLimit row.
''' A claim with no limit row counts as zero, never as unlimited (CR 0012). Matching ignores case.
''' </summary>
Public NotInheritable Class TdsDeductionCapper
    Private Sub New()
    End Sub

    Public Shared Function Total(claims As IEnumerable(Of TdsDeductionClaim), limits As IEnumerable(Of TdsDeductionLimit)) As Decimal
        If claims Is Nothing OrElse limits Is Nothing Then
            Return 0D
        End If
        Dim claimed As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        For Each claim As TdsDeductionClaim In claims
            Dim key As String = KeyOf(claim.SectionCode, claim.SubLimitKey)
            claimed(key) = claimed.GetValueOrDefault(key) + claim.Amount
        Next
        Dim sum As Decimal = 0D
        For Each limit As TdsDeductionLimit In limits
            Dim key As String = KeyOf(limit.SectionCode, limit.SubLimitKey)
            If claimed.ContainsKey(key) Then
                sum += Math.Min(claimed(key), limit.MaxAmount)
            End If
        Next
        Return sum
    End Function

    Private Shared Function KeyOf(sectionCode As String, subLimitKey As String) As String
        Return sectionCode.Trim() & "|" & subLimitKey.Trim()
    End Function
End Class
