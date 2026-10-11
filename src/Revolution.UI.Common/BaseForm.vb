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
    Private ReadOnly _validationLabel As New Label()
    Private ReadOnly _content As New Panel()
    Private ReadOnly _errors As New ErrorProvider()
    Private _busy As Boolean

    Protected Sub New(title As String, Optional subtitle As String = "")
        AutoScaleMode = AutoScaleMode.Dpi
        AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        StartPosition = FormStartPosition.CenterParent
        Text = title
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

    ''' <summary>Shows a summary error under the content. Cleared by ClearValidation.</summary>
    Protected Sub ShowError(message As String)
        _validationLabel.Text = message
        _validationLabel.Visible = True
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
        _validationLabel.Visible = False
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
        layout.Controls.Add(New Label With {
            .Text = caption,
            .AutoSize = True,
            .Margin = New Padding(0, Theme.Space1, 0, 2)
        })
        input.Dock = DockStyle.Fill
        input.Margin = New Padding(0, 0, 0, Theme.Space1)
        layout.Controls.Add(input)
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
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _errors.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub BuildHeader(title As String, subtitle As String)
        _header.Tag = ThemeStyler.SkipTag
        _header.Dock = DockStyle.Top
        _header.Height = Theme.Space3 * 3
        _header.BackColor = Theme.Accent
        _header.Padding = New Padding(Theme.Space2, Theme.Space1, Theme.Space2, Theme.Space1)
        _titleLabel.Text = title
        _titleLabel.Font = Theme.TitleFont
        _titleLabel.ForeColor = Theme.AccentText
        _titleLabel.Dock = DockStyle.Fill
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft
        _subtitleLabel.Text = subtitle
        _subtitleLabel.Font = Theme.SmallFont
        _subtitleLabel.ForeColor = Theme.AccentText
        _subtitleLabel.Dock = DockStyle.Bottom
        _subtitleLabel.Height = Theme.Space3
        _subtitleLabel.Visible = Not String.IsNullOrEmpty(subtitle)
        _header.Controls.Add(_titleLabel)
        _header.Controls.Add(_subtitleLabel)
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
        _validationLabel.Visible = False
    End Sub
End Class
