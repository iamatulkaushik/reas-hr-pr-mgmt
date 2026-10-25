Imports Revolution.Core
Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeTdsRepository
    Implements ITdsRepository

    Public Existing As TdsDeclarationHeader
    Public Items As IReadOnlyList(Of TdsItem) = New List(Of TdsItem)()
    Public Rents As IReadOnlyList(Of TdsRentView) = New List(Of TdsRentView)()
    Public Prev As TdsPrevEmployer
    Public SaveFailure As String
    Public ReadOnly Calls As New List(Of String)()
    Public ReadOnly RentsWritten As New List(Of TdsRentWrite)()
    Public ItemsWritten As IReadOnlyList(Of TdsItem)
    Public LastStatus As TdsDeclarationStatus
    Public LastRegime As TdsRegime

    Public Function GetDeclaration(employeeId As Integer, financialYear As Integer) As TdsDeclarationHeader Implements ITdsRepository.GetDeclaration
        Return Existing
    End Function

    Public Function SaveDeclaration(employeeId As Integer, financialYear As Integer, regime As TdsRegime, userId As Integer?) As Integer Implements ITdsRepository.SaveDeclaration
        If SaveFailure IsNot Nothing Then Throw New PayrollRuleException(SaveFailure)
        Calls.Add("SaveDeclaration")
        LastRegime = regime
        Return If(Existing Is Nothing, 1, Existing.DeclarationId)
    End Function

    Public Sub SetStatus(declarationId As Integer, status As TdsDeclarationStatus, userId As Integer?) Implements ITdsRepository.SetStatus
        If SaveFailure IsNot Nothing Then Throw New PayrollRuleException(SaveFailure)
        Calls.Add("SetStatus")
        LastStatus = status
    End Sub

    Public Function ListItems(declarationId As Integer) As IReadOnlyList(Of TdsItem) Implements ITdsRepository.ListItems
        Return Items
    End Function

    Public Sub ReplaceItems(declarationId As Integer, newItems As IReadOnlyList(Of TdsItem), userId As Integer?) Implements ITdsRepository.ReplaceItems
        Calls.Add("ReplaceItems")
        ItemsWritten = newItems
    End Sub

    Public Function ListRents(declarationId As Integer) As IReadOnlyList(Of TdsRentView) Implements ITdsRepository.ListRents
        Return Rents
    End Function

    Public Sub ReplaceRents(declarationId As Integer, newRents As IReadOnlyList(Of TdsRentWrite), userId As Integer?) Implements ITdsRepository.ReplaceRents
        Calls.Add("ReplaceRents")
        RentsWritten.AddRange(newRents)
    End Sub

    Public Function GetPrevEmployer(declarationId As Integer) As TdsPrevEmployer Implements ITdsRepository.GetPrevEmployer
        Return Prev
    End Function

    Public Sub SavePrevEmployer(declarationId As Integer, newPrev As TdsPrevEmployer, userId As Integer?) Implements ITdsRepository.SavePrevEmployer
        Calls.Add("SavePrevEmployer")
    End Sub

    Public Sub ClearPrevEmployer(declarationId As Integer, userId As Integer?) Implements ITdsRepository.ClearPrevEmployer
        Calls.Add("ClearPrevEmployer")
    End Sub
End Class

Friend NotInheritable Class FakeTdsCrypto
    Implements IFieldCrypto

    Public ReadOnly Contexts As New List(Of String)()

    Public ReadOnly Property CurrentKeyVersion As Byte Implements IFieldCrypto.CurrentKeyVersion
        Get
            Return 3
        End Get
    End Property

    Public Function Encrypt(plain As String, context As String) As Byte() Implements IFieldCrypto.Encrypt
        Contexts.Add(context)
        Return System.Text.Encoding.UTF8.GetBytes("enc:" & plain)
    End Function

    Public Function Decrypt(cipher As Byte(), keyVersion As Byte, context As String) As String Implements IFieldCrypto.Decrypt
        Return System.Text.Encoding.UTF8.GetString(cipher).Substring(4)
    End Function

    Public Function BlindIndex(companyId As Integer, field As String, plain As String) As Byte() Implements IFieldCrypto.BlindIndex
        Return New Byte(31) {}
    End Function
End Class

Friend NotInheritable Class FakeTdsRates
    Implements IStatutoryRateService

    Public RuleFailure As String
    Public LimitFailure As String
    Public CityFailure As String
    Public LastAgeBand As TdsAgeBand
    Public LastAsOf As Date

    Public Function GetTdsRule(asOf As Date, regime As TdsRegime, ageBand As TdsAgeBand) As OperationResult(Of TdsRule) Implements IStatutoryRateService.GetTdsRule
        If RuleFailure IsNot Nothing Then Return OperationResult(Of TdsRule).Fail(RuleFailure)
        LastAgeBand = ageBand
        LastAsOf = asOf
        Return OperationResult(Of TdsRule).Ok(If(regime = TdsRegime.Old, TdsTestRules.OldRegime(), TdsTestRules.NewRegime()))
    End Function

    Public Function GetTdsDeductionLimits(asOf As Date, regime As TdsRegime) As OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)) Implements IStatutoryRateService.GetTdsDeductionLimits
        If LimitFailure IsNot Nothing Then Return OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)).Fail(LimitFailure)
        Return OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)).Ok(TdsTestRules.OldLimits())
    End Function

    Public Function GetHraMetroList(asOf As Date) As OperationResult(Of HraMetroList) Implements IStatutoryRateService.GetHraMetroList
        If CityFailure IsNot Nothing Then Return OperationResult(Of HraMetroList).Fail(CityFailure)
        Return OperationResult(Of HraMetroList).Ok(HraTestCities.From2026())
    End Function

    Public Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate) Implements IStatutoryRateService.GetEpfRate
        Return OperationResult(Of EpfRate).Fail("Not used in these tests.")
    End Function

    Public Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate) Implements IStatutoryRateService.GetEsiRate
        Return OperationResult(Of EsiRate).Fail("Not used in these tests.")
    End Function

    Public Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule) Implements IStatutoryRateService.GetPtRule
        Return OperationResult(Of PtRule).Fail("Not used in these tests.")
    End Function

    Public Function GetWageRule(asOf As Date) As OperationResult(Of WageRule) Implements IStatutoryRateService.GetWageRule
        Return OperationResult(Of WageRule).Fail("Not used in these tests.")
    End Function

    Public Function GetGratuityRule(asOf As Date) As OperationResult(Of GratuityRule) Implements IStatutoryRateService.GetGratuityRule
        Return OperationResult(Of GratuityRule).Fail("Not used in these tests.")
    End Function

    Public Function GetBonusRule(asOf As Date) As OperationResult(Of BonusRule) Implements IStatutoryRateService.GetBonusRule
        Return OperationResult(Of BonusRule).Fail("Not used in these tests.")
    End Function

    Public Function GetLabourWelfareRule(stateId As Integer, asOf As Date) As OperationResult(Of LabourWelfareRule) Implements IStatutoryRateService.GetLabourWelfareRule
        Return OperationResult(Of LabourWelfareRule).Fail("Not used in these tests.")
    End Function

    Public Function GetTdsRuleValue(valueKey As String, asOf As Date) As OperationResult(Of Decimal) Implements IStatutoryRateService.GetTdsRuleValue
        Return OperationResult(Of Decimal).Fail("Not used in these tests.")
    End Function

    Public Function GetTdsEmployerNps(asOf As Date, regime As TdsRegime) As OperationResult(Of Decimal) Implements IStatutoryRateService.GetTdsEmployerNps
        Return OperationResult(Of Decimal).Fail("Not used in these tests.")
    End Function

    Public Function GetTaxFormNo(purposeKey As String, financialYear As Integer) As OperationResult(Of String) Implements IStatutoryRateService.GetTaxFormNo
        Return OperationResult(Of String).Fail("Not used in these tests.")
    End Function

    Public Function GetTaxSectionRef(topicKey As String, financialYear As Integer) As OperationResult(Of String) Implements IStatutoryRateService.GetTaxSectionRef
        Return OperationResult(Of String).Fail("Not used in these tests.")
    End Function

    Public Function GetMinimumWages(stateId As Integer, asOf As Date) As OperationResult(Of IReadOnlyList(Of MinimumWageRate)) Implements IStatutoryRateService.GetMinimumWages
        Return OperationResult(Of IReadOnlyList(Of MinimumWageRate)).Fail("Not used in these tests.")
    End Function
End Class
