Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>A small dialog that asks for the month a monthly attendance screen works on. It starts on the month before the current one (ADR 0026). Closing it keeps that month.</summary>
Friend NotInheritable Class MonthlyPeriodPicker
    Private Sub New()
    End Sub

    ''' <summary>The first day of the chosen month.</summary>
    Public Shared Function Pick(defaultPeriod As Date) As Date
        Dim start As New Date(defaultPeriod.Year, defaultPeriod.Month, 1)
        Using dialog As New Form()
            dialog.Text = "Choose the month"
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog
            dialog.StartPosition = FormStartPosition.CenterScreen
            dialog.MaximizeBox = False
            dialog.MinimizeBox = False
            dialog.ClientSize = New Size(300, 110)
            Dim month As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Left = 16, .Top = 24, .Width = 150}
            For m As Integer = 1 To 12
                month.Items.Add(CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m))
            Next
            month.SelectedIndex = start.Month - 1
            Dim year As New NumericUpDown() With {.Left = 176, .Top = 24, .Width = 80, .Minimum = 2000, .Maximum = Date.Today.Year, .Value = Math.Min(start.Year, Date.Today.Year)}
            Dim ok As New Button() With {.Text = "Open", .Left = 176, .Top = 66, .Width = 80, .DialogResult = DialogResult.OK}
            dialog.Controls.AddRange(New Control() {month, year, ok})
            dialog.AcceptButton = ok
            If dialog.ShowDialog() <> DialogResult.OK Then Return start
            Return New Date(CInt(year.Value), month.SelectedIndex + 1, 1)
        End Using
    End Function
End Class
