''' <summary>Checks ECR lines before upload. Mirrors the main EPFO return errors. Ceiling comes from the EPF rate row.</summary>
Public NotInheritable Class EcrValidator
    Private Sub New()
    End Sub

    Public Shared Function Validate(members As IEnumerable(Of EcrMember), wageCeiling As Decimal) As IReadOnlyList(Of String)
        If members Is Nothing Then
            Throw New ArgumentNullException(NameOf(members))
        End If
        Dim errors As New List(Of String)()
        Dim row As Integer = 0
        For Each m As EcrMember In members
            row += 1
            CheckIdentity(m, row, errors)
            CheckWages(m, row, wageCeiling, errors)
            CheckAmounts(m, row, errors)
        Next
        If row = 0 Then
            errors.Add("ECR has no member lines.")
        End If
        Return errors
    End Function

    Private Shared Sub CheckIdentity(m As EcrMember, row As Integer, errors As List(Of String))
        If m.Uan Is Nothing OrElse m.Uan.Length <> 12 OrElse Not m.Uan.All(AddressOf Char.IsDigit) Then
            errors.Add($"Line {row}: UAN must be 12 digits.")
        End If
        If EcrNameCleaner.Clean(m.Name).Length = 0 Then
            errors.Add($"Line {row}: member name is empty.")
        End If
    End Sub

    Private Shared Sub CheckWages(m As EcrMember, row As Integer, wageCeiling As Decimal, errors As List(Of String))
        If m.GrossWages <= 0D AndAlso (m.EpfWages > 0D OrElse m.EpsWages > 0D OrElse m.EdliWages > 0D) Then
            errors.Add($"Line {row}: gross wages are zero but other wages are not.")
        End If
        If m.EpfWages > m.GrossWages Then
            errors.Add($"Line {row}: EPF wages are above gross wages.")
        End If
        If m.EpsWages > m.EpfWages Then
            errors.Add($"Line {row}: EPS wages are above EPF wages.")
        End If
        If m.EpsWages > wageCeiling Then
            errors.Add($"Line {row}: EPS wages are above the wage ceiling.")
        End If
        If m.EdliWages > wageCeiling Then
            errors.Add($"Line {row}: EDLI wages are above the wage ceiling.")
        End If
    End Sub

    Private Shared Sub CheckAmounts(m As EcrMember, row As Integer, errors As List(Of String))
        Dim amounts As Decimal() = {m.GrossWages, m.EpfWages, m.EpsWages, m.EdliWages, m.EmployeeEpf,
                                    m.EpsContribution, m.EmployerDifference, m.RefundOfAdvances}
        If amounts.Any(Function(a) a < 0D OrElse a <> Decimal.Truncate(a)) Then
            errors.Add($"Line {row}: amounts must be whole rupees and not negative.")
        End If
        If m.NcpDays < 0 OrElse m.NcpDays > 31 Then
            errors.Add($"Line {row}: NCP days must be 0 to 31.")
        End If
    End Sub
End Class
