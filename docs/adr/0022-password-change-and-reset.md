# ADR 0022: Changing and resetting passwords

Status: accepted (Owner, 2026-11-08). Owner: Sigma.

- **Own password.** Every signed-in user changes their own password from File > Change Password. No permission is needed; the current password is asked for (`IAuthService.ChangePassword`). At first sign-in, or after a reset, the same window opens by itself (the user must change it).
- **Another user's password: reset to a temporary one.** The caller needs Edit on Security (Developer, Owner and, from migration 0032, Associate). The database decides who may reset whom, in one place (`sec.fn_ResettableUsers`, migration 0031), reading the caller from the session:
  - Developer resets an Associate.
  - Associate resets the Owner of the company that is open in the session, and only a company the Associate works on.
  - Owner resets an Operator of the own company.
  - Nobody resets themselves here, a Developer, or an inactive user.
- A reset sets a temporary password (policy: 10 characters, upper, lower, digit, symbol), makes the user change it at the next sign-in, clears the failed-attempt count and the lock, and writes an audit entry (`PasswordReset`). The hash is made by the app; the database never sees a plain password. The screen is Administration > Reset user password.
- **Developer emergency reset.** `RevolutionSetup reset-password <userName>` sets a temporary password for any active user (including a Developer). It needs the setup connection, which the running app never has.
- **Forgotten password.** The user asks the person above them in this list; there is no self-service reset (no e-mail or SMS channel in v1).
- **Not changed:** `sec.usp_User_SetPassword` (0003) stays as it is. It is called while a user signs in (first-login change), before any session user exists, so it cannot check a caller. It is protected by the secrecy of the app login, as the other sign-in procedures are; an in-app reset never uses it.
