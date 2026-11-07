Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance
Imports Revolution.Modules.Biometric

''' <summary>
''' Attendance from punches, plain version for testing (Brave; Sigma restyles it). Pick the first and last date (at most nine days) and Preview: for each employee-day found in the punches the screen
''' shows the shift, the in and out and what would be done. When an employee has only one punch the exit is marked at the end of the assigned shift (Owner, 2026-11-04) and the line says so.
''' Apply marks the days that can be marked. Days that already have a mark, weekly offs, holidays, closed months, days with no shift, lone punches after the shift end and shifts that have not ended are listed and left alone.
''' </summary>
Friend NotInheritable Class PunchAttendanceForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1100
    Private Const FormHeight As Integer = 700
    Private Const TimeFormat As String = "dd-MM-yyyy HH:mm"

    Private ReadOnly _services As BiometricServices
    Private ReadOnly _from As New DateTimePicker()
    Private ReadOnly _to As New DateTimePicker()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _preview As New Button()
    Private ReadOnly _apply As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As BiometricServices)
        MyBase.New("Attendance from punches", "Mark attendance from biometric and card punches. A lone punch gets its exit at the shift end.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _from.Format = DateTimePickerFormat.Short
        _to.Format = DateTimePickerFormat.Short
        _from.Value = Date.Today.AddDays(-1)
        _to.Value = Date.Today.AddDays(-1)
        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "First date", _from)
        AddField(layout, "Last date", _to)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _apply.Text = "Apply"
        _apply.AutoSize = True
        _apply.Enabled = False
        _preview.Text = "Preview"
        _preview.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_apply)
        bar.Controls.Add(_preview)
        AddHandler _from.ValueChanged, Sub(sender As Object, e As EventArgs) ForgetPreview()
        AddHandler _to.ValueChanged, Sub(sender As Object, e As EventArgs) ForgetPreview()
        AddHandler _preview.Click, Sub(sender As Object, e As EventArgs) DoPreview()
        AddHandler _apply.Click, Sub(sender As Object, e As EventArgs) DoApply()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(DayLine.EmployeeId), "Employee id", 9)
        AddColumn(NameOf(DayLine.Day), "Date", 10)
        AddColumn(NameOf(DayLine.Shift), "Shift", 9)
        AddColumn(NameOf(DayLine.PunchIn), "In", 14)
        AddColumn(NameOf(DayLine.PunchOut), "Out", 14)
        AddColumn(NameOf(DayLine.Result), "Result", 10)
        AddColumn(NameOf(DayLine.Action), "What happens", 14)
        AddColumn(NameOf(DayLine.Note), "Note", 20)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_})
    End Sub

    Private Sub ForgetPreview()
        _grid.DataSource = Nothing
        _apply.Enabled = False
    End Sub

    Private Sub DoPreview()
        ClearValidation()
        ForgetPreview()
        Dim result As OperationResult(Of PunchAttendancePlan) = _services.PunchAttendance.Preview(_from.Value, _to.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim plan As PunchAttendancePlan = result.Value
        _grid.DataSource = plan.Rows.Select(Function(r) New DayLine(r)).ToList()
        _apply.Enabled = plan.Count(PunchAttendanceAction.Mark) > 0
        ShowStatus($"{plan.Count(PunchAttendanceAction.Mark).ToString(CultureInfo.InvariantCulture)} days can be marked, {plan.Count(PunchAttendanceAction.NeedsReview).ToString(CultureInfo.InvariantCulture)} need a look, " &
                   $"{(plan.Rows.Count - plan.Count(PunchAttendanceAction.Mark) - plan.Count(PunchAttendanceAction.NeedsReview)).ToString(CultureInfo.InvariantCulture)} are left alone.")
    End Sub

    Private Sub DoApply()
        ClearValidation()
        Dim answer As DialogResult = MessageBox.Show(Me, "Mark attendance from these punches? Days that already have a mark are not changed.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If answer <> DialogResult.Yes Then Return
        Dim result As OperationResult(Of PunchApplyResult) = _services.PunchAttendance.Apply(_from.Value, _to.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ForgetPreview()
        If result.Value.Failures.Count > 0 Then
            ShowError(String.Join(" ", result.Value.Failures.Take(3)))
        End If
        ShowStatus($"{result.Value.Marked.ToString(CultureInfo.InvariantCulture)} days marked, {result.Value.Failures.Count.ToString(CultureInfo.InvariantCulture)} refused, {result.Value.Skipped.ToString(CultureInfo.InvariantCulture)} left alone.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoPreview()
    End Sub

    Private NotInheritable Class DayLine
        Public Sub New(row As PunchAttendanceRow)
            EmployeeId = row.EmployeeId.ToString(CultureInfo.InvariantCulture)
            Day = row.WorkDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            Shift = If(row.ShiftName, String.Empty)
            PunchIn = If(row.PunchIn.HasValue, row.PunchIn.Value.ToString(TimeFormat, CultureInfo.InvariantCulture), String.Empty)
            PunchOut = If(row.PunchOut.HasValue, row.PunchOut.Value.ToString(TimeFormat, CultureInfo.InvariantCulture) & If(row.AutoExit, " (auto)", String.Empty), String.Empty)
            Result = If(row.Status.HasValue, row.Status.Value.ToString(), String.Empty)
            Action = ActionText(row.Action)
            Note = If(row.Note, String.Empty)
        End Sub

        Public ReadOnly Property EmployeeId As String
        Public ReadOnly Property Day As String
        Public ReadOnly Property Shift As String
        Public ReadOnly Property PunchIn As String
        Public ReadOnly Property PunchOut As String
        Public ReadOnly Property Result As String
        Public ReadOnly Property Action As String
        Public ReadOnly Property Note As String

        Private Shared Function ActionText(action As PunchAttendanceAction) As String
            Select Case action
                Case PunchAttendanceAction.Mark : Return "Will be marked"
                Case PunchAttendanceAction.AlreadyMarked : Return "Already marked"
                Case PunchAttendanceAction.WeeklyOff : Return "Weekly off"
                Case PunchAttendanceAction.Holiday : Return "Holiday"
                Case PunchAttendanceAction.MonthClosed : Return "Month closed"
                Case PunchAttendanceAction.NoShift : Return "No shift"
                Case PunchAttendanceAction.NeedsReview : Return "Needs a look"
                Case Else : Return "Shift not ended"
            End Select
        End Function
    End Class
End Class
