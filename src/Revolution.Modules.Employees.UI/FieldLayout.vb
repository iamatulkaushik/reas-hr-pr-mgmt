Imports System.Windows.Forms

''' <summary>Small helpers for the employee panels: the same stacked label-over-input layout as BaseForm, and combo box binding.</summary>
Friend NotInheritable Class FieldLayout
    Private Sub New()
    End Sub

    Public Shared Function Create() As TableLayoutPanel
        Dim layout As New TableLayoutPanel With {.ColumnCount = 1, .Dock = DockStyle.Top, .AutoSize = True}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        Return layout
    End Function

    Public Shared Sub Add(layout As TableLayoutPanel, caption As String, input As Control)
        layout.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(0, Theme.Space1, 0, 2)})
        input.Dock = DockStyle.Fill
        input.Margin = New Padding(0, 0, 0, Theme.Space1)
        layout.Controls.Add(input)
    End Sub

    Public Shared Sub Bind(Of TKey)(combo As ComboBox, pairs As IEnumerable(Of KeyValuePair(Of TKey, String)))
        combo.DropDownStyle = ComboBoxStyle.DropDownList
        combo.DataSource = pairs.ToList()
        combo.DisplayMember = "Value"
        combo.ValueMember = "Key"
    End Sub

    ''' <summary>The pairs with an empty choice first (key 0), for optional fields.</summary>
    Public Shared Function WithBlank(pairs As IEnumerable(Of KeyValuePair(Of Integer, String))) As IEnumerable(Of KeyValuePair(Of Integer, String))
        Dim list As New List(Of KeyValuePair(Of Integer, String)) From {New KeyValuePair(Of Integer, String)(0, String.Empty)}
        list.AddRange(pairs)
        Return list
    End Function

    Public Shared Function IntValue(combo As ComboBox) As Integer
        Return If(combo.SelectedValue Is Nothing, 0, Convert.ToInt32(combo.SelectedValue))
    End Function

    ''' <summary>Nothing when the empty choice (0) is selected.</summary>
    Public Shared Function OptionalValue(combo As ComboBox) As Integer?
        Dim value As Integer = IntValue(combo)
        Return If(value = 0, Nothing, CType(value, Integer?))
    End Function

    Public Shared Sub SelectValue(combo As ComboBox, value As Integer?)
        combo.SelectedValue = If(value.HasValue, value.Value, 0)
    End Sub
End Class
