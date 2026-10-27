''' <summary>One row of the leave ledger as stored. Days is signed: rows that take leave away are negative.</summary>
Public NotInheritable Class LeaveEntryView
    Public Sub New(ledgerId As Long, leaveTypeId As Integer, leaveYear As Integer, entryDate As Date, kind As LeaveEntryKind, days As Decimal, note As String)
        Me.LedgerId = ledgerId
        Me.LeaveTypeId = leaveTypeId
        Me.LeaveYear = leaveYear
        Me.EntryDate = entryDate
        Me.Kind = kind
        Me.Days = days
        Me.Note = If(note, String.Empty)
    End Sub

    Public ReadOnly Property LedgerId As Long
    Public ReadOnly Property LeaveTypeId As Integer
    Public ReadOnly Property LeaveYear As Integer
    Public ReadOnly Property EntryDate As Date
    Public ReadOnly Property Kind As LeaveEntryKind
    Public ReadOnly Property Days As Decimal
    Public ReadOnly Property Note As String
End Class
