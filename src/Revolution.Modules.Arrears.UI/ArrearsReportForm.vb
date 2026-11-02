Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports Revolution.Modules.Arrears

''' <summary>
''' Arrears report screen, plain version for testing (Brave; Sigma restyles it, and the printed layout waits for the Owner's design, ADR 0005).
''' Pick the first and last wage month and Load: one line per employee and wage month from the open claims, with the totals in the status bar.
''' </summary>
Friend NotInheritable Class ArrearsReportForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 640
    Private Const MonthFormat As String = "MMM yyyy"
    Private Const PlainIdWarning As String = "The file holds plain UAN numbers. Keep it safe, upload it to EPFO, then delete it."

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As ArrearsServices
    Private ReadOnly _fromMonth As New DateTimePicker()
    Private ReadOnly _toMonth As New DateTimePicker()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _ecr As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As ArrearsServices)
        MyBase.New("Arrears report", "Arrears by employee and wage month, from the open claims.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        ConfigureMonth(_fromMonth)
        ConfigureMonth(_toMonth)
        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "First wage month", _fromMonth)
        AddField(layout, "Last wage month", _toMonth)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _load.Text = "Load"
        _load.AutoSize = True
        _ecr.Text = "Arrear ECR (first month)"
        _ecr.AutoSize = True
        _ecr.Visible = _services.Ecr IsNot Nothing
        bar.Controls.Add(_exit)
        bar.Controls.Add(_ecr)
        bar.Controls.Add(_load)
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _ecr.Click, Sub(sender As Object, e As EventArgs) DoEcr()
    End Sub

    Private Shared Sub ConfigureMonth(picker As DateTimePicker)
        picker.Format = DateTimePickerFormat.Custom
        picker.CustomFormat = MonthFormat
        picker.ShowUpDown = True
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(ReportLine.EmpCode), "Code", 9, False)
        AddColumn(NameOf(ReportLine.FullName), "Name", 20, False)
        AddColumn(NameOf(ReportLine.WageMonth), "Wage month", 10, False)
        AddColumn(NameOf(ReportLine.Gross), "Gross arrears", 12, True)
        AddColumn(NameOf(ReportLine.EmployeeEpf), "Employee EPF", 11, True)
        AddColumn(NameOf(ReportLine.EsiEmployee), "Employee ESI", 11, True)
        AddColumn(NameOf(ReportLine.Net), "Net before tax", 12, True)
        AddColumn(NameOf(ReportLine.Employer), "Employer share", 12, True)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    Private Sub DoLoad()
        ClearValidation()
        Dim result As OperationResult(Of ArrearsReport) = _services.Arrears.Report(_fromMonth.Value, _toMonth.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim report As ArrearsReport = result.Value
        _grid.DataSource = report.Rows.Select(Function(r) New ReportLine(r)).ToList()
        ShowStatus($"{report.EmployeeCount.ToString(CultureInfo.InvariantCulture)} employees. Gross {Money(report.GrossTotal)}, EPF and ESI {Money(report.EmployeeDeductionTotal)}, " &
                   $"net before tax {Money(report.NetBeforeTax)}, employer share {Money(report.EmployerTotal)}.")
    End Sub

    ''' <summary>Builds the arrear ECR for the first wage month picked, from the open claims, and saves it as text with no byte order mark.</summary>
    Private Sub DoEcr()
        ClearValidation()
        Dim result As OperationResult(Of Revolution.Modules.Payroll.FileOutput(Of String)) = _services.Ecr.Build(_fromMonth.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim place As String = AskFile(result.Value.FileName)
        If place Is Nothing Then Return
        Try
            File.WriteAllText(place, result.Value.Content, New UTF8Encoding(False))
        Catch ex As IOException
            ShowError("The file could not be saved. Check the folder and try again.")
            Return
        Catch ex As UnauthorizedAccessException
            ShowError("The file could not be saved. Check the folder and try again.")
            Return
        End Try
        ShowStatus($"Saved {Path.GetFileName(place)}. Left out: {result.Value.Problems.Count.ToString(CultureInfo.InvariantCulture)}. " & String.Join(" ", result.Value.Problems))
        MessageBox.Show(Me, PlainIdWarning, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private Function AskFile(suggested As String) As String
        Using dialog As New SaveFileDialog With {.Title = Text, .Filter = "Text files (*.txt)|*.txt", .FileName = suggested, .AddExtension = True, .OverwritePrompt = True}
            If dialog.ShowDialog(Me) <> DialogResult.OK Then Return Nothing
            Return dialog.FileName
        End Using
    End Function

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private NotInheritable Class ReportLine
        Public Sub New(row As ArrearsReportRow)
            EmpCode = row.EmpCode
            FullName = row.FullName
            WageMonth = row.WageMonth.ToString(MonthFormat, CultureInfo.InvariantCulture)
            Gross = Money(row.GrossDifference)
            EmployeeEpf = Money(row.EmployeeEpf)
            EsiEmployee = Money(row.EsiEmployee)
            Net = Money(row.GrossDifference - row.EmployeeDeductions)
            Employer = Money(row.EmployerTotal)
        End Sub

        Public ReadOnly Property EmpCode As String
        Public ReadOnly Property FullName As String
        Public ReadOnly Property WageMonth As String
        Public ReadOnly Property Gross As String
        Public ReadOnly Property EmployeeEpf As String
        Public ReadOnly Property EsiEmployee As String
        Public ReadOnly Property Net As String
        Public ReadOnly Property Employer As String
    End Class
End Class
