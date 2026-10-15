Imports System.Globalization
Imports System.Text

''' <summary>Builds the EPFO ECR text: one line per member, eleven fields, #~# between fields, no header.</summary>
Public NotInheritable Class EcrFileWriter
    Public Const Separator As String = "#~#"

    Private Sub New()
    End Sub

    Public Shared Function Build(members As IReadOnlyCollection(Of EcrMember), wageCeiling As Decimal) As String
        Dim problems As IReadOnlyList(Of String) = EcrValidator.Validate(members, wageCeiling)
        If problems.Count > 0 Then
            Throw New InvalidOperationException("ECR is not valid: " & String.Join(" ", problems))
        End If
        Dim text As New StringBuilder()
        For Each m As EcrMember In members
            text.Append(Line(m)).Append(vbCrLf)
        Next
        Return text.ToString()
    End Function

    Public Shared Function Line(m As EcrMember) As String
        Dim fields As String() = {
            m.Uan,
            EcrNameCleaner.Clean(m.Name),
            Whole(m.GrossWages),
            Whole(m.EpfWages),
            Whole(m.EpsWages),
            Whole(m.EdliWages),
            Whole(m.EmployeeEpf),
            Whole(m.EpsContribution),
            Whole(m.EmployerDifference),
            m.NcpDays.ToString(CultureInfo.InvariantCulture),
            Whole(m.RefundOfAdvances)
        }
        Return String.Join(Separator, fields)
    End Function

    Private Shared Function Whole(amount As Decimal) As String
        Return Decimal.Truncate(amount).ToString(CultureInfo.InvariantCulture)
    End Function
End Class
