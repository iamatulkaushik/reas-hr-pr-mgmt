''' <summary>
''' Encrypts protected ids for storage (ADR 0004, ADR 0011): Aadhaar, PAN, UAN, ESI IP, bank account, document numbers.
''' Callers never log a plain or cipher value. Masked display stays in the module (SensitiveIdMask).
''' </summary>
Public Interface IFieldCrypto
    ''' <summary>The key version to store in IdKeyVersion beside every new ciphertext.</summary>
    ReadOnly Property CurrentKeyVersion As Byte

    ''' <summary>AES-256-GCM. Fresh random nonce per call. Output is nonce (12) + tag (16) + ciphertext. The context is authenticated, not stored.</summary>
    Function Encrypt(plain As String, context As String) As Byte()

    ''' <summary>Decrypts a value made with the same context and key version. Throws CryptographicException if anything differs or was changed.</summary>
    Function Decrypt(cipher As Byte(), keyVersion As Byte, context As String) As String

    ''' <summary>
    ''' HMAC-SHA256 of company, field and the id (spaces and hyphens removed, upper case). 32 bytes. Same input gives the same output,
    ''' so a unique index finds duplicates without decrypting. The index key never changes with the encryption key version.
    ''' </summary>
    Function BlindIndex(companyId As Integer, field As String, plain As String) As Byte()
End Interface
