''' <summary>The four id edits from the employee form.</summary>
Public NotInheritable Class IdentifierInput
    Public Property Uan As IdEdit = IdEdit.Unchanged
    Public Property EsiIp As IdEdit = IdEdit.Unchanged
    Public Property Pan As IdEdit = IdEdit.Unchanged
    Public Property Aadhaar As IdEdit = IdEdit.Unchanged

    Public ReadOnly Property HasChanges As Boolean
        Get
            Return Uan.Action <> IdAction.Unchanged OrElse EsiIp.Action <> IdAction.Unchanged OrElse
                   Pan.Action <> IdAction.Unchanged OrElse Aadhaar.Action <> IdAction.Unchanged
        End Get
    End Property

    ''' <summary>A new employee: every id typed on the draft is a replacement, blank ones stay empty.</summary>
    Public Shared Function FromDraft(draft As EmployeeDraft) As IdentifierInput
        If draft Is Nothing Then Throw New ArgumentNullException(NameOf(draft))
        Return New IdentifierInput With {
            .Uan = IdEdit.FromText(draft.Uan),
            .EsiIp = IdEdit.FromText(draft.EsiIp),
            .Pan = IdEdit.FromText(draft.Pan),
            .Aadhaar = IdEdit.FromText(draft.Aadhaar)}
    End Function
End Class
