''' <summary>Supplies the field keys. Keys live outside the database (ADR 0011). A key is 32 bytes.</summary>
Public Interface IFieldKeyProvider
    ''' <summary>The key version used for new encryption.</summary>
    ReadOnly Property CurrentVersion As Byte

    ''' <summary>Returns the key for a purpose and version. Throws if that version is not available. Never logs the key.</summary>
    Function GetKey(purpose As FieldKeyPurpose, version As Byte) As Byte()
End Interface
