# Database tests

Tests that need a real SQL Server: tenant isolation, EXECUTE-only access, composite keys, unique ids, and that a failed procedure leaves no open transaction.

## Run
1. Make an **empty, disposable** database (for example `HRMS_Test`) on SQL Server. Never a real one: the tests create companies and employees.
2. Apply the scripts: `set HRMS_SETUP_CONNECTION=<db_owner connection string>` then `dotnet run --project src/Revolution.Setup -- migrate`.
3. `set HRMS_TEST_CONNECTION=<the same connection string>` then `dotnet test tests/Revolution.Database.Tests`.

Without `HRMS_TEST_CONNECTION` every test returns at once and passes without checking anything. To make that a failure (for CI), also set `HRMS_REQUIRE_DB_TESTS=1`.

## What each test proves
| Test | Proves |
|---|---|
| EveryTableWithCompanyId_HasAnEnabledFilterPolicy | no tenant table was left without a working policy (this is what a skipped `CREATE SECURITY POLICY` would break) |
| EmployeeList / GetById / Update across companies | procedures never show or change another company's rows |
| NoCompanyInSession_Fails | no session company means no data, error 50002 |
| AppRole_CannotReadTablesDirectly | the app role is EXECUTE only |
| RowFilter_HidesOtherCompanyRows_ForADirectReader | the row filter works without the procedures |
| DeveloperSession_SeesAllCompanies | the developer bypass is the only bypass |
| CompositeKey_RefusesAnEmployeeWithAnotherCompanysFactory | the database refuses cross-company links |
| EmployeeCode_IsUniquePerCompany_NotAcrossCompanies, SamePanHash_InOneCompany_IsRefused | unique codes and duplicate ids are caught in the database |
| MonthClose_Twice_FailsAndLeavesNoOpenTransaction | migration 0207: `XACT_ABORT` is inside the procedure |
