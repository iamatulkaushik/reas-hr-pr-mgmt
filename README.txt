Brave: payroll file source and export screens (ADR 0019). Copy these files over the repo, same paths.

DELETE: src/Revolution.Modules.Payroll/ExportSheet.vb   (Core's ExportSheet replaces it)

Sigma edits (not done here):
1. Revolution.slnx: add
     <Project Path="src/Revolution.Modules.Payroll.UI/Revolution.Modules.Payroll.UI.vbproj" />
2. Revolution.App.vbproj: ProjectReference to Revolution.Modules.Payroll and Revolution.Modules.Payroll.UI.
3. Program.vb BuildModules: keep the Employees services in a variable and add the Payroll shell module:
     Dim employees = EmployeesModule.Compose(services.Host)
     ... New EmployeesShellModule(employees, services.Permissions),
         New AttendanceShellModule(AttendanceModule.Compose(services.Host), services.Permissions),
         New PayrollShellModule(PayrollModule.Compose(services.Host, employees.Export), services.Permissions)
4. MenuLayoutModule: place three screens (keys payroll.export.ecr, payroll.export.esic, payroll.export.bank).

Not compiled, not run: no .NET SDK or NuGet where this was written. Run dotnet build and dotnet test first.
Open: ESIC reason code has no source yet (stays 0). Migration 0309 not run on SQL Server.
