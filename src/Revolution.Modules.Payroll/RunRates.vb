Imports Revolution.Core

''' <summary>The statutory rows a run reads once, for the run's rate date. Professional tax is one rule per state.</summary>
Public NotInheritable Class RunRates
    Public Sub New(epf As EpfRate, esi As EsiRate, wageRule As WageRule, ptRules As IReadOnlyDictionary(Of Integer, PtRule))
        If epf Is Nothing Then Throw New ArgumentNullException(NameOf(epf))
        If esi Is Nothing Then Throw New ArgumentNullException(NameOf(esi))
        If wageRule Is Nothing Then Throw New ArgumentNullException(NameOf(wageRule))
        If ptRules Is Nothing Then Throw New ArgumentNullException(NameOf(ptRules))
        Me.Epf = epf
        Me.Esi = esi
        Me.WageRule = wageRule
        Me.PtRules = ptRules
    End Sub

    Public ReadOnly Property Epf As EpfRate
    Public ReadOnly Property Esi As EsiRate
    Public ReadOnly Property WageRule As WageRule
    Public ReadOnly Property PtRules As IReadOnlyDictionary(Of Integer, PtRule)
End Class
