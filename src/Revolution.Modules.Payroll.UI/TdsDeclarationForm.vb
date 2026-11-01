Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Core
Imports Revolution.Modules.Payroll

''' <summary>
''' Income tax declaration screen, plain version for testing (Brave; Sigma restyles it). Type an employee id and the financial year, Load, edit the items, rent periods and previous employer on the
''' tabs, then Save the whole declaration and move it: Submit, Lock, or back to Draft. A landlord's PAN is never shown after saving (only whether one is held), so type it again when saving rent
''' that needs it. Every action is checked again by TdsDeclarationService.
''' </summary>
Friend NotInheritable Class TdsDeclarationForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 800
    Private Const MaxMoney As Decimal = 999999999D

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _employeeId As New NumericUpDown()
    Private ReadOnly _year As New NumericUpDown()
    Private ReadOnly _regime As New ComboBox()
    Private ReadOnly _tabs As New TabControl()
    Private ReadOnly _items As New DataGridView()
    Private ReadOnly _rents As New DataGridView()
    Private ReadOnly _hasPrev As New CheckBox()
    Private ReadOnly _prevName As New TextBox()
    Private ReadOnly _prevTan As New TextBox()
    Private ReadOnly _prevSalary As New NumericUpDown()
    Private ReadOnly _prevPerq As New NumericUpDown()
    Private ReadOnly _prevPf As New NumericUpDown()
    Private ReadOnly _prevPt As New NumericUpDown()
    Private ReadOnly _prevTax As New NumericUpDown()
    Private ReadOnly _load As New Button()
    Private ReadOnly _save As New Button()
    Private ReadOnly _submit As New Button()
    Private ReadOnly _lock As New Button()
    Private ReadOnly _draft As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As PayrollServices)
        MyBase.New("Income tax declaration", "Declare investments, rent and previous employer pay for the year, then submit it. Locking needs approval rights.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        _tabs.Dock = DockStyle.Fill
        _tabs.TabPages.Add(GridPage("Items", _items, {"Section", "Sub-limit key", "Description", "Declared", "Proof reference", "Verified (blank if not)"}))
        _tabs.TabPages.Add(GridPage("Rent", _rents, {"From month (yyyymm)", "To month (yyyymm)", "Monthly rent", "City", "Landlord", "Relationship", "Address", "Landlord PAN"}))
        _tabs.TabPages.Add(PrevPage())
        Content.Controls.Add(_tabs)

        _employeeId.Minimum = 1D
        _employeeId.Maximum = Integer.MaxValue
        _year.Minimum = 2000D
        _year.Maximum = 9998D
        _year.Value = FinancialYearCalendar.StartYear(Date.Today)
        _regime.DropDownStyle = ComboBoxStyle.DropDownList
        _regime.DataSource = [Enum].GetValues(GetType(TdsRegime))
        _regime.SelectedItem = TdsRegime.New

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Employee id", _employeeId)
        AddField(layout, "Financial year starting in", _year)
        AddField(layout, "Tax regime", _regime)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        bar.Controls.Add(_exit)
        For Each pair In {Tuple.Create(_draft, "Back to draft"), Tuple.Create(_lock, "Lock"), Tuple.Create(_submit, "Submit"), Tuple.Create(_save, "Save declaration"), Tuple.Create(_load, "Load")}
            pair.Item1.Text = pair.Item2
            pair.Item1.AutoSize = True
            bar.Controls.Add(pair.Item1)
        Next
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _save.Click, Sub(sender As Object, e As EventArgs) DoSave()
        AddHandler _submit.Click, Sub(sender As Object, e As EventArgs) DoStatus(TdsDeclarationStatus.Submitted, "Submitted.")
        AddHandler _lock.Click, Sub(sender As Object, e As EventArgs) DoStatus(TdsDeclarationStatus.Locked, "Locked.")
        AddHandler _draft.Click, Sub(sender As Object, e As EventArgs) DoStatus(TdsDeclarationStatus.Draft, "Back to draft.")
    End Sub

    Private Shared Function GridPage(title As String, grid As DataGridView, headers As String()) As TabPage
        grid.Dock = DockStyle.Fill
        grid.AllowUserToAddRows = True
        grid.AllowUserToDeleteRows = True
        grid.RowHeadersVisible = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        For Each header As String In headers
            grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = header})
        Next
        Dim page As New TabPage(title)
        page.Controls.Add(grid)
        Return page
    End Function

    Private Function PrevPage() As TabPage
        Dim page As New TabPage("Previous employer")
        Dim panel As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .AutoScroll = True, .Padding = New Padding(8)}
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40.0F))
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60.0F))
        _hasPrev.Text = "Salary was earned from another employer this year"
        _hasPrev.AutoSize = True
        _prevName.MaxLength = 200
        _prevTan.MaxLength = 10
        For Each box As NumericUpDown In {_prevSalary, _prevPerq, _prevPf, _prevPt, _prevTax}
            box.Maximum = MaxMoney
        Next
        AddRow(panel, "Previous employer", _hasPrev)
        AddRow(panel, "Employer name", _prevName)
        AddRow(panel, "Employer TAN", _prevTan)
        AddRow(panel, "Salary income", _prevSalary)
        AddRow(panel, "Perquisites", _prevPerq)
        AddRow(panel, "Provident fund", _prevPf)
        AddRow(panel, "Professional tax", _prevPt)
        AddRow(panel, "Tax deducted", _prevTax)
        page.Controls.Add(panel)
        Return page
    End Function

    Private Shared Sub AddRow(panel As TableLayoutPanel, label As String, control As Control)
        panel.Controls.Add(New Label With {.Text = label, .AutoSize = True, .Anchor = AnchorStyles.Left, .Padding = New Padding(0, 6, 0, 6)})
        control.Dock = DockStyle.Fill
        panel.Controls.Add(control)
    End Sub

    Private Function Year() As Integer
        Return CInt(_year.Value)
    End Function

    Private Function EmployeeId() As Integer
        Return CInt(_employeeId.Value)
    End Function

    Private Sub DoLoad()
        ClearValidation()
        Dim result As OperationResult(Of TdsDeclarationBundle) = _services.Tds.GetDeclaration(EmployeeId(), Year())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _items.Rows.Clear()
        _rents.Rows.Clear()
        ClearPrev()
        If result.Value Is Nothing Then
            ShowStatus("No declaration yet for this employee and year. Fill the tabs and Save.")
            SetActions(Nothing)
            Return
        End If
        Dim bundle As TdsDeclarationBundle = result.Value
        _regime.SelectedItem = bundle.Header.Regime
        For Each item As TdsItem In bundle.Items
            _items.Rows.Add(item.SectionCode, item.SubLimitKey, item.Description, Amount(item.DeclaredAmount), item.ProofRef, If(item.VerifiedAmount.HasValue, Amount(item.VerifiedAmount.Value), String.Empty))
        Next
        For Each rent As TdsRentView In bundle.Rents
            _rents.Rows.Add(rent.FromMonthKey, rent.ToMonthKey, Amount(rent.MonthlyRent), rent.City, rent.LandlordName, rent.LandlordRelationship, rent.LandlordAddress, String.Empty)
        Next
        ShowPrev(bundle.Prev)
        Dim withPan As Integer = bundle.Rents.Where(Function(r) r.HasPan).Count()
        ShowStatus($"Declaration loaded, {bundle.Header.Status}. Landlord PAN is not shown: {withPan.ToString(CultureInfo.InvariantCulture)} of {bundle.Rents.Count.ToString(CultureInfo.InvariantCulture)} rent periods hold one.")
        SetActions(bundle.Header.Status)
    End Sub

    Private Sub DoSave()
        ClearValidation()
        Dim items As New List(Of TdsItem)()
        Dim rents As New List(Of TdsRentInput)()
        If Not ReadItems(items) OrElse Not ReadRents(rents) Then Return
        Dim prev As TdsPrevEmployer = If(_hasPrev.Checked, New TdsPrevEmployer(_prevName.Text, _prevTan.Text, _prevSalary.Value, _prevPerq.Value, _prevPf.Value, _prevPt.Value, _prevTax.Value), Nothing)
        Dim result As OperationResult(Of Integer) = _services.Tds.Save(EmployeeId(), Year(), CType(_regime.SelectedItem, TdsRegime), items, rents, prev)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus("Declaration saved.")
    End Sub

    Private Sub DoStatus(status As TdsDeclarationStatus, done As String)
        ClearValidation()
        Dim result As OperationResult = _services.Tds.SetStatus(EmployeeId(), Year(), status)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus(done)
    End Sub

    Private Function ReadItems(items As List(Of TdsItem)) As Boolean
        For Each row As DataGridViewRow In _items.Rows
            If row.IsNewRow Then Continue For
            Dim declared As Decimal, verified As Decimal
            If Not TryMoney(row, 3, declared) Then Return Fail("Items", row, "declared amount")
            Dim verifiedText As String = CellText(row, 5)
            If verifiedText.Length > 0 AndAlso Not TryMoney(row, 5, verified) Then Return Fail("Items", row, "verified amount")
            items.Add(New TdsItem(CellText(row, 0), CellText(row, 1), CellText(row, 2), declared, CellText(row, 4), If(verifiedText.Length > 0, CType(verified, Decimal?), Nothing)))
        Next
        Return True
    End Function

    Private Function ReadRents(rents As List(Of TdsRentInput)) As Boolean
        For Each row As DataGridViewRow In _rents.Rows
            If row.IsNewRow Then Continue For
            Dim fromKey As Integer, toKey As Integer, rent As Decimal
            If Not Integer.TryParse(CellText(row, 0), NumberStyles.None, CultureInfo.InvariantCulture, fromKey) Then Return Fail("Rent", row, "from month")
            If Not Integer.TryParse(CellText(row, 1), NumberStyles.None, CultureInfo.InvariantCulture, toKey) Then Return Fail("Rent", row, "to month")
            If Not TryMoney(row, 2, rent) Then Return Fail("Rent", row, "monthly rent")
            rents.Add(New TdsRentInput(fromKey, toKey, rent, CellText(row, 3), CellText(row, 4), CellText(row, 5), CellText(row, 6), CellText(row, 7)))
        Next
        Return True
    End Function

    Private Function Fail(tab As String, row As DataGridViewRow, what As String) As Boolean
        ShowError($"{tab}, row {(row.Index + 1).ToString(CultureInfo.InvariantCulture)}: enter a valid {what}.")
        Return False
    End Function

    Private Shared Function CellText(row As DataGridViewRow, column As Integer) As String
        Dim value As Object = row.Cells(column).Value
        Return If(value Is Nothing, String.Empty, Convert.ToString(value, CultureInfo.InvariantCulture).Trim())
    End Function

    Private Shared Function TryMoney(row As DataGridViewRow, column As Integer, ByRef value As Decimal) As Boolean
        Return Decimal.TryParse(CellText(row, column), NumberStyles.Number, CultureInfo.InvariantCulture, value) AndAlso value >= 0D
    End Function

    Private Shared Function Amount(value As Decimal) As String
        Return value.ToString("0.##", CultureInfo.InvariantCulture)
    End Function

    Private Sub ClearPrev()
        _hasPrev.Checked = False
        _prevName.Clear()
        _prevTan.Clear()
        For Each box As NumericUpDown In {_prevSalary, _prevPerq, _prevPf, _prevPt, _prevTax}
            box.Value = 0D
        Next
    End Sub

    Private Sub ShowPrev(prev As TdsPrevEmployer)
        If prev Is Nothing Then Return
        _hasPrev.Checked = True
        _prevName.Text = prev.EmployerName
        _prevTan.Text = prev.EmployerTan
        _prevSalary.Value = prev.SalaryIncome
        _prevPerq.Value = prev.Perquisites
        _prevPf.Value = prev.ProvidentFund
        _prevPt.Value = prev.ProfessionalTax
        _prevTax.Value = prev.TaxDeducted
    End Sub

    ''' <summary>Save is open until the declaration is locked; Submit from Draft, Lock from Submitted, Back to draft from Submitted or Locked (the service decides who may).</summary>
    Private Sub SetActions(status As TdsDeclarationStatus?)
        Dim s As TdsDeclarationStatus = If(status, TdsDeclarationStatus.Draft)
        _save.Enabled = Not status.HasValue OrElse s <> TdsDeclarationStatus.Locked
        _submit.Enabled = status.HasValue AndAlso s = TdsDeclarationStatus.Draft
        _lock.Enabled = status.HasValue AndAlso s = TdsDeclarationStatus.Submitted
        _draft.Enabled = status.HasValue AndAlso s <> TdsDeclarationStatus.Draft
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub
End Class
