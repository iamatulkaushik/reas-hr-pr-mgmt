''' <summary>
''' Asset facts for a full and final settlement (ast.usp_AssetRecovery_Fnf*). Optional on FnfService: without it a settlement has no asset line and no asset block. Load takes the open recoveries, and those a settlement
''' already holds when fnfId is given. Reserve holds them for the settlement when their total equals the lines' total; Release puts them back.
''' </summary>
Public Interface IFnfAssetSource
    Function Load(employeeId As Integer, fnfId As Integer?) As FnfAssetFacts

    Sub Reserve(fnfId As Integer, employeeId As Integer, total As Decimal)

    Sub Release(fnfId As Integer)
End Interface
