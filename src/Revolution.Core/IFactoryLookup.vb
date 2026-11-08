''' <summary>
''' The active locations (factories) of the current company, for a module that needs a location picker without referencing the Employees module
''' (Biometric device registration, change request 0024). Read only. The company comes from the session, never from a parameter.
''' </summary>
Public Interface IFactoryLookup
    Function ListActive() As IReadOnlyList(Of FactoryChoice)
End Interface
