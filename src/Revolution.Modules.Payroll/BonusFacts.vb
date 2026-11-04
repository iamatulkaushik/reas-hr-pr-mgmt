''' <summary>What the database knows for a bonus run: every employee's months of the accounting year, the bonus each employee was already paid for it, and the provident fund flags.</summary>
Public NotInheritable Class BonusFacts
    Public Sub New(months As IReadOnlyList(Of BonusMonthFact), alreadyPaid As IReadOnlyDictionary(Of Integer, Decimal), Optional flags As IReadOnlyDictionary(Of Integer, BonusEmployeeFlags) = Nothing)
        If months Is Nothing Then Throw New ArgumentNullException(NameOf(months))
        If alreadyPaid Is Nothing Then Throw New ArgumentNullException(NameOf(alreadyPaid))
        Me.Months = months
        Me.AlreadyPaid = alreadyPaid
        Me.Flags = If(flags, New Dictionary(Of Integer, BonusEmployeeFlags)())
    End Sub

    Public ReadOnly Property Months As IReadOnlyList(Of BonusMonthFact)
    Public ReadOnly Property AlreadyPaid As IReadOnlyDictionary(Of Integer, Decimal)
    ''' <summary>Provident fund flags by employee id. An employee without an entry is not a member.</summary>
    Public ReadOnly Property Flags As IReadOnlyDictionary(Of Integer, BonusEmployeeFlags)
End Class
