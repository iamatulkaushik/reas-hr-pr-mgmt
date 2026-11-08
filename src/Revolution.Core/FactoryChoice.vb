''' <summary>A location (factory) of the current company, as a picker needs it: id, code and name.</summary>
Public NotInheritable Class FactoryChoice
    Public Sub New(factoryId As Integer, code As String, name As String)
        Me.FactoryId = factoryId
        Me.Code = If(code, String.Empty)
        Me.Name = If(name, String.Empty)
    End Sub

    Public ReadOnly Property FactoryId As Integer
    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String

    ''' <summary>What a picker shows: the name, with the code when there is one.</summary>
    Public Overrides Function ToString() As String
        Return If(Code.Length = 0, Name, Name & " (" & Code & ")")
    End Function
End Class
