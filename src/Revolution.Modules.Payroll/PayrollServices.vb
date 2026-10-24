Imports Revolution.Core

''' <summary>The services of the Payroll module that screens use. Grows as screens ship.</summary>
Public NotInheritable Class PayrollServices
    Public Sub New(files As PayrollFileService, rates As IStatutoryRateService, exporter As ITableExporter, bankWriters As IReadOnlyList(Of IBankFileWriter))
        If files Is Nothing Then Throw New ArgumentNullException(NameOf(files))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If exporter Is Nothing Then Throw New ArgumentNullException(NameOf(exporter))
        If bankWriters Is Nothing OrElse bankWriters.Count = 0 Then Throw New ArgumentException("At least one bank file writer is needed.", NameOf(bankWriters))
        Me.Files = files
        Me.Rates = rates
        Me.Exporter = exporter
        Me.BankWriters = bankWriters
    End Sub

    ''' <summary>ECR, ESIC and bank file builders for an approved run.</summary>
    Public ReadOnly Property Files As PayrollFileService

    ''' <summary>Effective-dated rates. The ECR screen reads the EPF wage ceiling here.</summary>
    Public ReadOnly Property Rates As IStatutoryRateService

    ''' <summary>Writes the ESIC .xls (NPOI behind Core's ITableExporter).</summary>
    Public ReadOnly Property Exporter As ITableExporter

    ''' <summary>One writer per supported bank. State Bank of India first (Owner decision).</summary>
    Public ReadOnly Property BankWriters As IReadOnlyList(Of IBankFileWriter)
End Class
