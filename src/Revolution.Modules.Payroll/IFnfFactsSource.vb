''' <summary>Reads the database facts for one separation in one call (pay.usp_Fnf_Facts). Nothing when the separation is not found.</summary>
Public Interface IFnfFactsSource
    Function Load(separationId As Integer) As FnfSourceFacts
End Interface
