''' <summary>One line of a settlement. Amount is whole rupees and always positive; the kind says whether it is paid to the employee or taken from them.</summary>
Public NotInheritable Class FnfLine
    Public Sub New(kind As FnfComponentKind, description As String, amount As Decimal)
        If Not [Enum].IsDefined(GetType(FnfComponentKind), kind) Then Throw New ArgumentOutOfRangeException(NameOf(kind))
        If String.IsNullOrWhiteSpace(description) Then Throw New ArgumentException("A line needs a description.", NameOf(description))
        If amount <= 0D Then Throw New ArgumentOutOfRangeException(NameOf(amount), "A line amount must be more than zero.")
        Me.Kind = kind
        Me.Description = description.Trim()
        Me.Amount = amount
    End Sub

    Public ReadOnly Property Kind As FnfComponentKind
    Public ReadOnly Property Description As String
    Public ReadOnly Property Amount As Decimal

    Public ReadOnly Property IsDeduction As Boolean
        Get
            Return FnfCalculator.IsDeduction(Kind)
        End Get
    End Property
End Class
