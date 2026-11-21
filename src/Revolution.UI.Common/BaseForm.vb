Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Base for every form (ADR 0006): header, toolbar, status bar, validation display, hotkeys, busy state.
''' Add your controls to Content. Override OnSaveRequested / OnRefreshRequested / OnHelpKeyPressed for Ctrl+S, F5, F1.
''' </summary>
Public MustInherit Class BaseForm
    Inherits Form

    Private ReadOnly _header As New Panel()
    Private ReadOnly _titleLabel As New Label()
    Private ReadOnly _subtitleLabel As New Label()
    Private ReadOnly _toolbar As New ToolStrip()
    Private ReadOnly _status As New StatusStrip()
    Private ReadOnly _statusLabel As New ToolStripStatusLabel()
    Private ReadOnly _progress As New ToolStripProgressBar()
    Private ReadOnly _infoPanels As New List(Of ToolStripStatusLabel)()
    Private ReadOnly _validationLabel As New Label()
    Private ReadOnly _content As New Panel()
    Private ReadOnly _errors As New ErrorProvider()
    Private _busy As Boolean
    Private _messageInline As Boolean
    Private _messageRoot As Control
    Private _fitRoot As Control
    Private _dialogRoot As TableLayoutPanel
    Private _dialogFields As TableLayoutPanel
    Private _dialogButtons As FlowLayoutPanel

    Protected Sub New(title As String, Optional subtitle As String = "")
        AutoScaleMode = AutoScaleMode.Dpi
        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        StartPosition = FormStartPosition.CenterParent
        Text = title
        ApplyWindowIcon()
        _errors.ContainerControl = Me
        _errors.BlinkStyle = ErrorBlinkStyle.NeverBlink
        BuildHeader(title, subtitle)
        BuildToolbar()
        BuildStatusBar()
        BuildValidationLabel()
        _content.Dock = DockStyle.Fill
        _content.Padding = New Padding(Theme.Space2)
        ' Fill control first, edge controls after: later-added edges dock first.
        Controls.Add(_content)
        Controls.Add(_validationLabel)
        Controls.Add(_status)
        Controls.Add(_toolbar)
        Controls.Add(_header)
        _content.BringToFront()
    End Sub

    ''' <summary>Where derived forms put their controls.</summary>
    Protected ReadOnly Property Content As Panel
        Get
            Return _content
        End Get
    End Property

    Protected ReadOnly Property Toolbar As ToolStrip
        Get
            Return _toolbar
        End Get
    End Property

    Protected Sub ShowStatus(message As String)
        _statusLabel.Text = message
    End Sub

    ''' <summary>Sets the permanent panels on the right of the status bar, for example the financial year and the server. Replaces earlier panels.</summary>
    Protected Sub SetStatusInfo(ParamArray panels As String())
        For Each oldPanel In _infoPanels
            _status.Items.Remove(oldPanel)
        Next
        _infoPanels.Clear()
        For Each panelText As String In panels
            Dim panel As New ToolStripStatusLabel(panelText) With {.BorderSides = ToolStripStatusLabelBorderSides.Left}
            _status.Items.Add(panel)
            _infoPanels.Add(panel)
        Next
    End Sub

    ''' <summary>Shows a summary error under the content. Cleared by ClearValidation.</summary>
    Protected Sub ShowError(message As String)
        _validationLabel.Text = message
        _validationLabel.BackColor = Theme.DangerSoft
        _validationLabel.Visible = True
        FitHeight()
    End Sub

    ''' <summary>Marks one field with the error icon and moves focus to it.</summary>
    Protected Sub SetFieldError(field As Control, message As String)
        _errors.SetError(field, message)
        ShowError(message)
        field.Focus()
    End Sub

    Protected Sub ClearValidation()
        _errors.Clear()
        _validationLabel.Text = String.Empty
        If _messageInline Then
            ' The line keeps its row, so the fields and buttons never move when an error appears or goes.
            _validationLabel.BackColor = Color.Transparent
        Else
            _validationLabel.Visible = False
        End If
        FitHeight()
    End Sub

    ''' <summary>Runs long work off the UI thread with a wait cursor, progress bar and the content disabled.</summary>
    Protected Async Function RunBusyAsync(work As Func(Of Task)) As Task
        If _busy Then
            Return
        End If
        _busy = True
        _content.Enabled = False
        _progress.Visible = True
        UseWaitCursor = True
        ShowStatus("Working...")
        Try
            Await work()
        Finally
            UseWaitCursor = False
            _progress.Visible = False
            _content.Enabled = True
            _busy = False
            ShowStatus("Ready")
        End Try
    End Function

    ''' <summary>A top-aligned single-column layout in Content for label-over-input fields.</summary>
    Protected Function CreateFormLayout() As TableLayoutPanel
        Dim layout As New TableLayoutPanel With {
            .ColumnCount = 1,
            .Dock = DockStyle.Top,
            .AutoSize = True
        }
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        _content.Controls.Add(layout)
        Return layout
    End Function

    Protected Shared Sub AddField(layout As TableLayoutPanel, caption As String, input As Control)
        Dim grid = TryCast(layout, FieldGrid)
        If grid IsNot Nothing Then
            grid.AddField(caption, input)
            Return
        End If
        layout.Controls.Add(New Label With {
            .Text = caption,
            .AutoSize = True,
            .Margin = New Padding(0, Theme.Space1, 0, 2)
        })
        input.Dock = DockStyle.Fill
        input.Margin = New Padding(0, 0, 0, Theme.Space1)
        layout.Controls.Add(input)
    End Sub

    ''' <summary>Like AddField, but a field that takes two columns in a FieldGrid (addresses, long names). In any other layout it is an ordinary field.</summary>
    Protected Shared Sub AddWideField(layout As TableLayoutPanel, caption As String, input As Control)
        Dim grid = TryCast(layout, FieldGrid)
        If grid IsNot Nothing Then
            grid.AddField(caption, input, 2)
        Else
            AddField(layout, caption, input)
        End If
    End Sub

    ''' <summary>
    ''' The standard screen: the editor on top, then a message line that keeps its own row, then the buttons, then the list filling the rest.
    ''' Add the primary button first (the buttons run right to left). editorPercent 0 sizes the editor to its content; a percentage gives it that share of the height.
    ''' </summary>
    Protected Function BuildStandardLayout(editor As Control, buttons As IEnumerable(Of Button), list As Control, Optional editorPercent As Single = 0.0F) As TableLayoutPanel
        Dim root = NewRoot(4)
        If editorPercent > 0.0F Then
            root.RowStyles.Add(New RowStyle(SizeType.Percent, editorPercent))
        Else
            root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        End If
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, If(editorPercent > 0.0F, 100.0F - editorPercent, 100.0F)))
        editor.Dock = DockStyle.Fill
        root.Controls.Add(editor, 0, 0)
        HostMessageLine(root, 1)
        Dim bar As New FlowLayoutPanel With {
            .FlowDirection = FlowDirection.RightToLeft,
            .Dock = DockStyle.Fill,
            .AutoSize = True,
            .WrapContents = True,
            .Margin = New Padding(0, 0, 0, Theme.Space1)
        }
        For Each actionButton In buttons
            actionButton.AutoSize = True
            bar.Controls.Add(actionButton)
        Next
        root.Controls.Add(bar, 0, 2)
        list.Dock = DockStyle.Fill
        root.Controls.Add(list, 0, 3)
        _content.Controls.Add(root)
        Return root
    End Function

    ''' <summary>The field area of a small dialog. The dialog sizes its height to its content, has no status bar, and keeps a message line above the buttons.</summary>
    Protected Function CreateDialogLayout() As TableLayoutPanel
        EnsureDialogRoot()
        Return _dialogFields
    End Function

    ''' <summary>The right-aligned button row of a small dialog. Add the primary button first.</summary>
    Protected Function CreateDialogButtonBar() As FlowLayoutPanel
        EnsureDialogRoot()
        Return _dialogButtons
    End Function

    Private Sub EnsureDialogRoot()
        If _dialogRoot IsNot Nothing Then
            Return
        End If
        _dialogRoot = NewRoot(3)
        For rowIndex As Integer = 1 To 3
            _dialogRoot.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next
        _dialogFields = New TableLayoutPanel With {.ColumnCount = 1, .Dock = DockStyle.Fill, .AutoSize = True}
        _dialogFields.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        _dialogButtons = New FlowLayoutPanel With {
            .FlowDirection = FlowDirection.RightToLeft,
            .Dock = DockStyle.Fill,
            .AutoSize = True,
            .WrapContents = True,
            .Margin = New Padding(0, 0, 0, Theme.Space1)
        }
        _dialogRoot.Controls.Add(_dialogFields, 0, 0)
        HostMessageLine(_dialogRoot, 1)
        _dialogRoot.Controls.Add(_dialogButtons, 0, 2)
        _content.Controls.Add(_dialogRoot)
        _fitRoot = _dialogRoot
        _status.Visible = False
    End Sub

    Private Shared Function NewRoot(rowCount As Integer) As TableLayoutPanel
        Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = rowCount, .Margin = New Padding(0)}
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        Return root
    End Function

    ''' <summary>Moves the error line from the bottom of the window into its own row of the layout, so showing it cannot cover anything.</summary>
    Private Sub HostMessageLine(root As TableLayoutPanel, row As Integer)
        Controls.Remove(_validationLabel)
        _messageInline = True
        _messageRoot = root
        _validationLabel.Dock = DockStyle.Fill
        _validationLabel.AutoSize = True
        _validationLabel.Visible = True
        _validationLabel.Text = String.Empty
        _validationLabel.BackColor = Color.Transparent
        _validationLabel.Margin = New Padding(0, Theme.Space1 \ 2, 0, Theme.Space1 \ 2)
        root.Controls.Add(_validationLabel, 0, row)
        AddHandler root.SizeChanged, AddressOf OnMessageRootSizeChanged
    End Sub

    Private Sub OnMessageRootSizeChanged(sender As Object, e As EventArgs)
        _validationLabel.MaximumSize = New Size(Math.Max(100, _messageRoot.ClientSize.Width), 0)
    End Sub

    ''' <summary>Sizes a dialog's height to its content, so no field or button can be cut off at any display scale.</summary>
    Private Sub FitHeight()
        If _fitRoot Is Nothing OrElse Not IsHandleCreated Then
            Return
        End If
        Dim availableWidth As Integer = Math.Max(100, ClientSize.Width - _content.Padding.Horizontal)
        _validationLabel.MaximumSize = New Size(availableWidth, 0)
        Dim wanted As Size = _fitRoot.GetPreferredSize(New Size(availableWidth, 0))
        Dim chrome As Integer = _header.Height + _content.Padding.Vertical
        If _toolbar.Visible Then chrome += _toolbar.Height
        If _status.Visible Then chrome += _status.Height
        Dim needed As Integer = chrome + wanted.Height
        If needed <> ClientSize.Height Then
            ClientSize = New Size(ClientSize.Width, needed)
        End If
    End Sub

    ''' <summary>A right-aligned button row at the bottom of Content. Add the primary button first.</summary>
    Protected Function CreateButtonBar() As FlowLayoutPanel
        Dim bar As New FlowLayoutPanel With {
            .Dock = DockStyle.Bottom,
            .FlowDirection = FlowDirection.RightToLeft,
            .AutoSize = True
        }
        _content.Controls.Add(bar)
        Return bar
    End Function

    Protected Overridable Sub OnSaveRequested()
    End Sub

    Protected Overridable Sub OnRefreshRequested()
    End Sub

    Protected Overridable Sub OnHelpKeyPressed()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Select Case keyData
            Case Keys.Control Or Keys.S
                OnSaveRequested()
                Return True
            Case Keys.F5
                OnRefreshRequested()
                Return True
            Case Keys.F1
                OnHelpKeyPressed()
                Return True
            Case Else
                Return MyBase.ProcessCmdKey(msg, keyData)
        End Select
    End Function

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        ThemeStyler.Apply(Me)
        If _messageInline Then
            ' One text line stays reserved for the message, so an error never moves the layout.
            _validationLabel.MinimumSize = New Size(0, FontHeight + _validationLabel.Padding.Vertical)
        End If
        FitHeight()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        FitHeight()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _errors.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    ''' <summary>Gives the form the application icon (ADR 0020). A form that cannot get it keeps the default icon: a missing icon must never stop a screen opening.</summary>
    Private Sub ApplyWindowIcon()
        Try
            Icon = AppWindowIcon.WindowIcon
        Catch ex As Exception
            Debug.WriteLine("The application icon could not be built: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildHeader(title As String, subtitle As String)
        _header.Tag = ThemeStyler.SkipTag
        _header.Dock = DockStyle.Top
        _header.Height = If(String.IsNullOrEmpty(subtitle), Theme.Space3 * 2 + Theme.Space1, Theme.Space3 * 3)
        _header.BackColor = Theme.Surface
        _header.Padding = New Padding(Theme.Space2, Theme.Space1, Theme.Space2, Theme.Space1)
        _titleLabel.Text = title
        _titleLabel.Font = Theme.TitleFont
        _titleLabel.ForeColor = Theme.Text
        _titleLabel.Dock = DockStyle.Fill
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft
        _subtitleLabel.Text = subtitle
        _subtitleLabel.Font = Theme.SmallFont
        _subtitleLabel.ForeColor = Theme.TextMuted
        _subtitleLabel.Dock = DockStyle.Bottom
        _subtitleLabel.Height = Theme.Space3
        _subtitleLabel.Visible = Not String.IsNullOrEmpty(subtitle)
        _header.Controls.Add(_titleLabel)
        _header.Controls.Add(_subtitleLabel)
        AddHandler _header.Paint, AddressOf OnHeaderPaint
    End Sub

    ''' <summary>Thin line under the light header.</summary>
    Private Sub OnHeaderPaint(sender As Object, e As PaintEventArgs)
        Using linePen As New Pen(Theme.BorderSoft)
            e.Graphics.DrawLine(linePen, 0, _header.Height - 1, _header.Width, _header.Height - 1)
        End Using
    End Sub

    Private Sub BuildToolbar()
        _toolbar.GripStyle = ToolStripGripStyle.Hidden
        _toolbar.Visible = False
        AddHandler _toolbar.ItemAdded, Sub(sender As Object, e As ToolStripItemEventArgs) _toolbar.Visible = True
    End Sub

    Private Sub BuildStatusBar()
        _statusLabel.Spring = True
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft
        _statusLabel.Text = "Ready"
        _progress.Style = ProgressBarStyle.Marquee
        _progress.Visible = False
        _status.Items.Add(_statusLabel)
        _status.Items.Add(_progress)
    End Sub

    Private Sub BuildValidationLabel()
        _validationLabel.Tag = ThemeStyler.SkipTag
        _validationLabel.Dock = DockStyle.Bottom
        _validationLabel.AutoSize = True
        _validationLabel.Padding = New Padding(Theme.Space2, Theme.Space1, Theme.Space2, Theme.Space1)
        _validationLabel.ForeColor = Theme.Danger
        _validationLabel.BackColor = Theme.DangerSoft
        _validationLabel.Visible = False
    End Sub
End Class
