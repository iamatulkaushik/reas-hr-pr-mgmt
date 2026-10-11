Imports System.Globalization

''' <summary>
''' Builds the context string that binds a ciphertext to its owner (ADR 0011). A value copied to another employee,
''' company or field fails to decrypt. For a document number add the document type: Build(c, e, DocumentNumber) + ":" + typeId.
''' </summary>
Public NotInheritable Class FieldContext
    Private Sub New()
    End Sub

    Public Const Aadhaar As String = "Aadhaar"
    Public Const Pan As String = "PAN"
    Public Const Uan As String = "UAN"
    Public Const EsiIp As String = "ESIIP"
    Public Const BankAccount As String = "BankAccount"
    Public Const DocumentNumber As String = "DocNo"

    Public Shared Function Build(companyId As Integer, ownerId As Integer, field As String) As String
        If String.IsNullOrWhiteSpace(field) Then
            Throw New ArgumentException("A field name is required.", NameOf(field))
        End If
        Return String.Concat(companyId.ToString(CultureInfo.InvariantCulture), ":", ownerId.ToString(CultureInfo.InvariantCulture), ":", field)
    End Function
End Class
