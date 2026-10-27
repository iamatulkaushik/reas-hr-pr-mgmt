''' <summary>One employee's position in one leave type. Every figure is in days. BalanceDays is the sum of all ledger rows.</summary>
Public NotInheritable Class LeaveBalanceRow
    Public Sub New(leaveTypeId As Integer, code As String, name As String, isEncashable As Boolean, maxEncashDays As Decimal?, accruedDays As Decimal,
                   carriedForwardDays As Decimal, takenDays As Decimal, encashedDays As Decimal, lapsedDays As Decimal, adjustedDays As Decimal, balanceDays As Decimal)
        Me.LeaveTypeId = leaveTypeId
        Me.Code = code
        Me.Name = name
        Me.IsEncashable = isEncashable
        Me.MaxEncashDays = maxEncashDays
        Me.AccruedDays = accruedDays
        Me.CarriedForwardDays = carriedForwardDays
        Me.TakenDays = takenDays
        Me.EncashedDays = encashedDays
        Me.LapsedDays = lapsedDays
        Me.AdjustedDays = adjustedDays
        Me.BalanceDays = balanceDays
    End Sub

    Public ReadOnly Property LeaveTypeId As Integer
    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property IsEncashable As Boolean
    Public ReadOnly Property MaxEncashDays As Decimal?
    ''' <summary>Opening plus accrual.</summary>
    Public ReadOnly Property AccruedDays As Decimal
    Public ReadOnly Property CarriedForwardDays As Decimal
    Public ReadOnly Property TakenDays As Decimal
    Public ReadOnly Property EncashedDays As Decimal
    Public ReadOnly Property LapsedDays As Decimal
    Public ReadOnly Property AdjustedDays As Decimal
    Public ReadOnly Property BalanceDays As Decimal
End Class
