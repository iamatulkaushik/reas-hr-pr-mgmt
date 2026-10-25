Imports Revolution.Core

''' <summary>The services of the Payroll module that screens use. Grows as screens ship.</summary>
Public NotInheritable Class PayrollServices
    Public Sub New(salary As SalaryService, runs As PayrollRunService, payslips As PayslipService, loans As LoanService, files As PayrollFileService, rates As IStatutoryRateService, exporter As ITableExporter, bankWriters As IReadOnlyList(Of IBankFileWriter))
        If salary Is Nothing Then Throw New ArgumentNullException(NameOf(salary))
        If runs Is Nothing Then Throw New ArgumentNullException(NameOf(runs))
        If payslips Is Nothing Then Throw New ArgumentNullException(NameOf(payslips))
        If loans Is Nothing Then Throw New ArgumentNullException(NameOf(loans))
        If files Is Nothing Then Throw New ArgumentNullException(NameOf(files))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If exporter Is Nothing Then Throw New ArgumentNullException(NameOf(exporter))
        If bankWriters Is Nothing OrElse bankWriters.Count = 0 Then Throw New ArgumentException("At least one bank file writer is needed.", NameOf(bankWriters))
        Me.Salary = salary
        Me.Runs = runs
        Me.Payslips = payslips
        Me.Loans = loans
        Me.Files = files
        Me.Rates = rates
        Me.Exporter = exporter
        Me.BankWriters = bankWriters
    End Sub

    ''' <summary>Company pay policy, salary components and effective-dated employee salary.</summary>
    Public ReadOnly Property Salary As SalaryService

    ''' <summary>Create, process, submit, approve, lock, reopen and pay a payroll run.</summary>
    Public ReadOnly Property Runs As PayrollRunService

    ''' <summary>Payslip data for the report helper: masked ids by default, full ids for an approved run with Export on Payroll.</summary>
    Public ReadOnly Property Payslips As PayslipService

    ''' <summary>Loan types, sanction with schedule, cancel and list.</summary>
    Public ReadOnly Property Loans As LoanService

    ''' <summary>ECR, ESIC and bank file builders for an approved run.</summary>
    Public ReadOnly Property Files As PayrollFileService

    ''' <summary>Effective-dated rates. The ECR screen reads the EPF wage ceiling here.</summary>
    Public ReadOnly Property Rates As IStatutoryRateService

    ''' <summary>Writes the ESIC .xls (NPOI behind Core's ITableExporter).</summary>
    Public ReadOnly Property Exporter As ITableExporter

    ''' <summary>One writer per supported bank. State Bank of India first (Owner decision).</summary>
    Public ReadOnly Property BankWriters As IReadOnlyList(Of IBankFileWriter)
End Class
