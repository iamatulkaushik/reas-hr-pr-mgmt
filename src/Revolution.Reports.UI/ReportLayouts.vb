Imports System.IO
Imports ReportStudio.Core.Model

''' <summary>Finds and reads the layout files that are copied next to the app (reports/layouts, ADR 0028).</summary>
Friend NotInheritable Class ReportLayouts
    Private Const LayoutFolder As String = "Reports"

    Private Sub New()
    End Sub

    Public Shared Function Folder() As String
        Return Path.Combine(AppContext.BaseDirectory, LayoutFolder)
    End Function

    ''' <summary>The layout, or Nothing with a message fit to show to the user.</summary>
    Public Shared Function Load(layoutFile As String, ByRef problem As String) As ReportDefinition
        problem = String.Empty
        Try
            Return ReportDefinition.Load(Path.Combine(Folder(), layoutFile))
        Catch ex As IOException
            problem = "The layout file of this report could not be read."
        Catch ex As System.Text.Json.JsonException
            problem = "The layout file of this report is damaged."
        End Try
        Return Nothing
    End Function
End Class
