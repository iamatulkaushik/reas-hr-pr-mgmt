''' <summary>
''' Splits a pay period at every date a new EPF rate row starts, so a month that spans a change (September 2026: ceiling 15,000
''' to 16 September, 25,000 from 17 September) is paid and filed in parts. The rate lookup is passed in, so any failure from
''' the rate service (missing row, row not signed off) stops the split and is returned as it is. Parts come back oldest first.
''' </summary>
Public NotInheritable Class EpfPeriodSplitter
    Private Const MaxParts As Integer = 12

    Private Sub New()
    End Sub

    Public Shared Function Split(periodFrom As Date, periodTo As Date, getRate As Func(Of Date, OperationResult(Of EpfRate))) As OperationResult(Of IReadOnlyList(Of EpfPeriodPart))
        If getRate Is Nothing Then Throw New ArgumentNullException(NameOf(getRate))
        If periodTo.Date < periodFrom.Date Then Throw New ArgumentException("The period ends before it starts.", NameOf(periodTo))
        Dim parts As New List(Of EpfPeriodPart)()
        Dim last As Date = periodTo.Date
        While parts.Count < MaxParts
            Dim found As OperationResult(Of EpfRate) = getRate(last)
            If Not found.Succeeded Then Return OperationResult(Of IReadOnlyList(Of EpfPeriodPart)).Fail(found.Message)
            Dim startsOn As Date = found.Value.EffectiveFrom.Date
            If startsOn <= periodFrom.Date Then
                parts.Insert(0, New EpfPeriodPart(periodFrom, last, found.Value))
                Return OperationResult(Of IReadOnlyList(Of EpfPeriodPart)).Ok(parts)
            End If
            parts.Insert(0, New EpfPeriodPart(startsOn, last, found.Value))
            last = startsOn.AddDays(-1)
        End While
        Return OperationResult(Of IReadOnlyList(Of EpfPeriodPart)).Fail("The period spans too many EPF rate changes.")
    End Function
End Class
