Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Label-over-input fields in as many columns as the width allows, up to MaxColumns. The inputs stretch to fill each column,
''' and the columns change when the window is resized, so a wide window shows more fields at once and nothing needs scrolling.
''' A field can take two columns (AddField with span 2); on a narrow window it falls back to one.
''' </summary>
Public NotInheritable Class FieldGrid
    Inherits TableLayoutPanel

    Private Const DesignDpi As Integer = 96
    Private Const DefaultMinColumnWidth As Integer = 230
    Private Const CaptionGap As Integer = 2

    Private NotInheritable Class FieldCell
        Public Sub New(host As Control, span As Integer)
            Me.Host = host
            Me.Span = span
        End Sub

        Public ReadOnly Property Host As Control

        Public ReadOnly Property Span As Integer
    End Class

    Private ReadOnly _cells As New List(Of FieldCell)()
    Private _maxColumns As Integer = 3
    Private _minColumnWidth As Integer = DefaultMinColumnWidth
    Private _columns As Integer
    Private _dirty As Boolean
    Private _relaying As Boolean

    Public Sub New()
        AutoSize = True
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        GrowStyle = TableLayoutPanelGrowStyle.AddRows
        ColumnCount = 1
        RowCount = 1
        Margin = New Padding(0)
    End Sub

    ''' <summary>The most columns used, however wide the window is.</summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property MaxColumns As Integer
        Get
            Return _maxColumns
        End Get
        Set(value As Integer)
            _maxColumns = Math.Max(1, value)
            _dirty = True
            Relayout()
        End Set
    End Property

    ''' <summary>The narrowest a column may get, in pixels at 96 dpi. A column count that would make columns narrower than this is not used.</summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property MinColumnWidth As Integer
        Get
            Return _minColumnWidth
        End Get
        Set(value As Integer)
            _minColumnWidth = Math.Max(60, value)
            _dirty = True
            Relayout()
        End Set
    End Property

    ''' <summary>Adds a field: the caption above the input. Fields fill the columns left to right, then down.</summary>
    Public Sub AddField(caption As String, input As Control, Optional span As Integer = 1)
        If input Is Nothing Then
            Throw New ArgumentNullException(NameOf(input))
        End If
        Dim cell As New TableLayoutPanel With {
            .ColumnCount = 1,
            .RowCount = 2,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Fill,
            .Margin = New Padding(0, 0, Theme.Space2, Theme.Space1)
        }
        cell.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        Dim captionLabel As New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(0, 0, 0, CaptionGap)}
        cell.Controls.Add(captionLabel)
        input.Dock = DockStyle.Fill
        input.Margin = New Padding(0)
        cell.Controls.Add(input)
        ' A long caption wraps inside its column instead of widening it.
        AddHandler cell.SizeChanged, Sub(sender As Object, e As EventArgs) captionLabel.MaximumSize = New Size(Math.Max(20, cell.ClientSize.Width), 0)
        _cells.Add(New FieldCell(cell, Math.Max(1, span)))
        cell.TabIndex = _cells.Count
        _dirty = True
        Relayout()
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)
        Relayout()
    End Sub

    Protected Overrides Sub OnVisibleChanged(e As EventArgs)
        MyBase.OnVisibleChanged(e)
        Relayout()
    End Sub

    Private Function ColumnsFor(width As Integer) As Integer
        Dim minWidth As Integer = Math.Max(1, CInt(_minColumnWidth * DeviceDpi / DesignDpi))
        Return Math.Max(1, Math.Min(_maxColumns, width \ minWidth))
    End Function

    Private Sub Relayout()
        If _relaying OrElse ClientSize.Width <= 0 Then
            Return
        End If
        Dim wanted As Integer = ColumnsFor(ClientSize.Width)
        If Not _dirty AndAlso wanted = _columns Then
            Return
        End If
        _relaying = True
        SuspendLayout()
        Try
            Controls.Clear()
            ColumnStyles.Clear()
            RowStyles.Clear()
            ColumnCount = wanted
            RowCount = 1
            For columnIndex As Integer = 1 To wanted
                ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / wanted))
            Next
            For Each fieldCell In _cells
                Controls.Add(fieldCell.Host)
                SetColumnSpan(fieldCell.Host, Math.Min(fieldCell.Span, wanted))
            Next
            _columns = wanted
            _dirty = False
        Finally
            ResumeLayout(True)
            _relaying = False
        End Try
    End Sub
End Class
