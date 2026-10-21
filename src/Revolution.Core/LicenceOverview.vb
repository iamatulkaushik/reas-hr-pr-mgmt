Imports System.Globalization

''' <summary>One line of the Developer's licence list: an Associate's current licence and whether it checks out today (ADR 0018).</summary>
Public NotInheritable Class LicenceOverview
    Public Sub New(associateUserName As String, planCap As PlanTier, validFrom As DateOnly, validTo As DateOnly,
                   maxCompanies As Integer, maxActiveUsers As Integer, state As LicenceState?, problem As LicenceProblem, message As String)
        Me.AssociateUserName = associateUserName
        Me.PlanCap = planCap
        Me.ValidFrom = validFrom
        Me.ValidTo = validTo
        Me.MaxCompanies = maxCompanies
        Me.MaxActiveUsers = maxActiveUsers
        Me.State = state
        Me.Problem = problem
        Me.Message = If(message, String.Empty)
    End Sub

    Public ReadOnly Property AssociateUserName As String
    Public ReadOnly Property PlanCap As PlanTier
    Public ReadOnly Property ValidFrom As DateOnly
    Public ReadOnly Property ValidTo As DateOnly
    Public ReadOnly Property MaxCompanies As Integer
    Public ReadOnly Property MaxActiveUsers As Integer
    ''' <summary>Nothing when the licence did not verify (see Problem).</summary>
    Public ReadOnly Property State As LicenceState?
    Public ReadOnly Property Problem As LicenceProblem
    Public ReadOnly Property Message As String

    Public ReadOnly Property ValidToText As String
        Get
            Return ValidTo.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
        End Get
    End Property

    ''' <summary>Plain words for the list: the state, or why the licence is not valid.</summary>
    Public ReadOnly Property StatusText As String
        Get
            If Problem <> LicenceProblem.None OrElse Not State.HasValue Then
                Return "Not valid: " & Message
            End If
            If State.Value = LicenceState.Valid Then
                Return "Valid"
            End If
            If State.Value = LicenceState.Expiring Then
                Return "Ends soon"
            End If
            If State.Value = LicenceState.Grace Then
                Return "In grace period"
            End If
            If State.Value = LicenceState.NotYetValid Then
                Return "Not started yet"
            End If
            If State.Value = LicenceState.Revoked Then
                Return "Revoked (read-only): " & Message
            End If
            Return "Expired (read-only)"
        End Get
    End Property
End Class
