# ADR 0011: Field encryption

Status: accepted. Key store written, not yet run on Windows. Owner: Sigma. Decides change request 0010. Builds on ADR 0004 (ids encrypted in the application) and ADR 0010 (columns).

## Decisions
- **What is protected.** Aadhaar, PAN, UAN, ESI IP, bank account number, document numbers. Stored as `<Id>Enc` (ciphertext) and `<Id>Last4` (masked display). The four employee ids also have `<Id>Hash` (blind index).
- **Cipher.** AES-256-GCM, 12-byte random nonce per value, 16-byte tag. Stored bytes are nonce + tag + ciphertext (at most 53 bytes for a 25 character value; the columns allow 256).
- **Context.** The company, owner and field are authenticated with every value (`FieldContext.Build`). A ciphertext copied to another employee, company or field does not decrypt. For document numbers add the document type to the context.
- **Blind index.** HMAC-SHA256 over company, field and the id (case, spaces, hyphens removed). 32 bytes. A filtered unique index per company on each hash column finds duplicates without decrypting. A different key from the encryption key.
- **Keys.** Two 32-byte keys, outside the database, versioned. `IdKeyVersion` on the row says which encryption key made it. A key rotation adds a version: new writes use it, old rows still decrypt, and a background job re-encrypts the rest. The index key stays at version 1; changing it means rebuilding every hash column.
- **Failure.** A wrong key, wrong context or changed byte gives one plain `CryptographicException`. Nothing is logged: no plain value, no cipher value, no key.
- **Reveal.** Showing an id in clear needs the Reveal permission (0013) and writes `aud.RevealLog`. Lists show last four characters only.

## Key store
- Built: `FieldKeySet` (keys by version, serialises), `FieldKeyBackup` (password backup: PBKDF2-SHA256 210,000 rounds, AES-256-GCM, 12 character minimum), `DpapiFieldKeyStore` (DPAPI, machine scope, refuses to overwrite) and Setup commands `keys-init`, `keys-info`, `keys-add-version`, `keys-backup`, `keys-restore`. Default file `C:\ProgramData\RevolutionHRMS\keys\fieldkeys.dat`.
- Tested here: the key set, rotation and the password backup (pure code, run on Linux). Compiled here with warnings as errors, but NOT run: the DPAPI store and the Setup commands. Their tests return at once on non-Windows; the Windows CI run is the first real test. Package `System.Security.Cryptography.ProtectedData 10.0.0` has not been restored yet: CI restore confirms the version.
- Machine scope lets any process on the computer open the file. Before the pilot, restrict the folder to the service account and administrators (Phase 10).
- A machine-scope key means a restored database on another computer cannot read ids until the key file is restored too. This must be in the Phase 10 restore test.

## Not decided
- Whether UAN and ESI IP duplicates are blocked for all employment types (the schema blocks them per company). Confirm with the CA.
