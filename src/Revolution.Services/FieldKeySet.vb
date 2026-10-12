Imports System.Globalization
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' The field keys (ADR 0011): encryption keys by version and the blind-index key. Serialises to bytes for the key store.
''' Implements IFieldKeyProvider, so AesGcmFieldCrypto can use it directly. Never logs or prints a key.
''' </summary>
Public NotInheritable Class FieldKeySet
    Implements IFieldKeyProvider

    Private Const KeySize As Integer = 32
    Private Const Magic As String = "RVFK1"

    Private ReadOnly _keys As New Dictionary(Of (FieldKeyPurpose, Byte), Byte())()

    Private Sub New()
    End Sub

    Public ReadOnly Property CurrentVersion As Byte Implements IFieldKeyProvider.CurrentVersion

    ''' <summary>A new set: encryption key version 1 and blind-index key version 1.</summary>
    Public Shared Function CreateNew() As FieldKeySet
        Dim set1 As New FieldKeySet()
        set1._keys((FieldKeyPurpose.Encryption, CByte(1))) = RandomNumberGenerator.GetBytes(KeySize)
        set1._keys((FieldKeyPurpose.BlindIndex, CByte(1))) = RandomNumberGenerator.GetBytes(KeySize)
        set1._CurrentVersion = 1
        Return set1
    End Function

    ''' <summary>Adds the next encryption key version and makes it current. Old versions stay so old rows still decrypt. The index key is not touched.</summary>
    Public Function AddEncryptionVersion() As Byte
        Dim newest = _keys.Keys.Where(Function(k) k.Item1 = FieldKeyPurpose.Encryption).Max(Function(k) k.Item2)
        If newest = Byte.MaxValue Then
            Throw New InvalidOperationException("No more key versions are available.")
        End If
        Dim nextVersion = CByte(newest + 1)
        _keys((FieldKeyPurpose.Encryption, nextVersion)) = RandomNumberGenerator.GetBytes(KeySize)
        _CurrentVersion = nextVersion
        Return nextVersion
    End Function

    ''' <summary>The encryption key versions in the set, lowest first. Versions only, never key material.</summary>
    Public Function EncryptionVersions() As IReadOnlyList(Of Byte)
        Return _keys.Keys.Where(Function(k) k.Item1 = FieldKeyPurpose.Encryption).Select(Function(k) k.Item2).OrderBy(Function(v) v).ToList()
    End Function

    Public Function GetKey(purpose As FieldKeyPurpose, version As Byte) As Byte() Implements IFieldKeyProvider.GetKey
        Dim key As Byte() = Nothing
        If Not _keys.TryGetValue((purpose, version), key) Then
            Throw New InvalidOperationException("Key version not available.")
        End If
        Return key
    End Function

    Public Function Serialize() As Byte()
        Dim text As New StringBuilder()
        text.Append(Magic).Append(vbLf)
        text.Append("current=").Append(CurrentVersion.ToString(CultureInfo.InvariantCulture)).Append(vbLf)
        For Each pair In _keys.OrderBy(Function(p) p.Key.Item1).ThenBy(Function(p) p.Key.Item2)
            text.Append(CInt(pair.Key.Item1).ToString(CultureInfo.InvariantCulture)).Append("|")
            text.Append(pair.Key.Item2.ToString(CultureInfo.InvariantCulture)).Append("|")
            text.Append(Convert.ToBase64String(pair.Value)).Append(vbLf)
        Next
        Return Encoding.UTF8.GetBytes(text.ToString())
    End Function

    Public Shared Function Deserialize(data As Byte()) As FieldKeySet
        Try
            Dim lines = Encoding.UTF8.GetString(data).Split(New Char() {ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
            If lines.Length < 3 OrElse lines(0) <> Magic OrElse Not lines(1).StartsWith("current=", StringComparison.Ordinal) Then
                Throw New FormatException()
            End If
            Dim result As New FieldKeySet()
            result._CurrentVersion = Byte.Parse(lines(1).Substring(8), CultureInfo.InvariantCulture)
            For i = 2 To lines.Length - 1
                Dim parts = lines(i).Split("|"c)
                Dim purpose = CType(Integer.Parse(parts(0), CultureInfo.InvariantCulture), FieldKeyPurpose)
                Dim key = Convert.FromBase64String(parts(2))
                If key.Length <> KeySize OrElse Not [Enum].IsDefined(purpose) Then
                    Throw New FormatException()
                End If
                result._keys((purpose, Byte.Parse(parts(1), CultureInfo.InvariantCulture))) = key
            Next
            If Not result._keys.ContainsKey((FieldKeyPurpose.Encryption, result._CurrentVersion)) _
               OrElse Not result._keys.ContainsKey((FieldKeyPurpose.BlindIndex, CByte(1))) Then
                Throw New FormatException()
            End If
            Return result
        Catch ex As Exception When TypeOf ex Is FormatException OrElse TypeOf ex Is OverflowException OrElse TypeOf ex Is IndexOutOfRangeException OrElse TypeOf ex Is ArgumentException
            Throw New InvalidDataException("The key data is damaged or not a key file.")
        End Try
    End Function
End Class
