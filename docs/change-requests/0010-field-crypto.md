# Change request 0010: Field encryption service (IFieldCrypto)

- Requested by: Brave
- Needed by module / phase: Phase 3 (employee repository and service). Replaces the note under CR 0006 in the Phase 3 plan.
- What is missing or wrong in the shared code: ADR 0004 says Aadhaar, PAN, UAN, ESI IP and bank account are AES-256 encrypted in the application. No encryption service exists in Core. The module cannot hold keys or call DPAPI itself without duplicating shared security code.
- Proposed signature or schema change:
  1. Core: `IFieldCrypto` with `Encrypt(plain As String) As Byte()`, `Decrypt(cipher As Byte()) As String`, `BlindIndex(plain As String) As Byte()`.
  2. Encrypt: AES-256-GCM, fresh random nonce per value, output = nonce + tag + ciphertext. Fits VARBINARY(256) for ids up to 18 characters.
  3. BlindIndex: HMAC-SHA256 of the normalised id with a separate key, 32 bytes. Used for the unique keys on Aadhaar and PAN per company. Same input must give the same output for the same company; mix CompanyID into the input.
  4. Keys: two keys (encrypt, index) kept outside the database, protected by Windows DPAPI (machine scope) and set up by Revolution.Setup. Key loss means ids cannot be read. Setup must warn and offer a backup file.
  5. Never log plain or cipher values. `Mask` stays in the Employees module (`SensitiveIdMask`).
  6. Schema is ready: migration 0100 has the five `*Enc` columns and the two `*Index` columns.
- Workaround until done: Brave keeps the employee repository and service on a branch. The columns stay empty. Nothing merges.
- Sigma's decision (accepted / changed / rejected) and date: **Accepted with changes, 2026-10-11.** Full design in ADR 0011. What changed from the proposal:
  1. Signatures. `Encrypt(plain, context)`, `Decrypt(cipher, keyVersion, context)`, `BlindIndex(companyId, field, plain)`, plus `CurrentKeyVersion`. The context binds a value to its company, employee and field (build it with `FieldContext.Build`). Store `CurrentKeyVersion` in `IdKeyVersion` beside the ciphertext; pass the stored version to `Decrypt`.
  2. Key rotation. Keys have versions, so old rows still decrypt after a new key is added. The index key does not rotate with the encryption key, so duplicate checks keep working.
  3. Blind index covers all four employee ids (Aadhaar, PAN, UAN, ESI IP), not only Aadhaar and PAN. The columns are in the ADR 0010 schema (`UanHash`, `EsiIpHash`, `PanHash`, `AadhaarHash`). The index normalises case, spaces and hyphens itself.
  4. Delivered by Sigma, in Core and Services: `IFieldCrypto`, `IFieldKeyProvider`, `FieldKeyPurpose`, `FieldContext`, `AesGcmFieldCrypto`, 21 test cases. Your module only calls `IFieldCrypto`.
  5. NOT delivered yet: the Windows key store (DPAPI, machine scope) and the key setup step in `Revolution.Setup`. Needs the Windows CI to compile and test. Until then the repository can be built and tested with a fake key provider, but the app cannot save ids on a real machine.
  6. Database. Your repository calls the `mst.usp_Employee_*` procedures of ADR 0010 (one set of migrations, 0100 to 0106). Your earlier 0100, 0103, 0105 and 0108 are superseded.
  7. Unblocked: EmployeeRepository, EmployeeService. Do not log plain or cipher values.
