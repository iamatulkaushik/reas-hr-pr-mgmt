''' <summary>
''' Reads punches from one device brand (Phase 8, ADR 0013: the agent pulls, devices do not log in). One class per brand, in its own project, so the agent and the server need no vendor library.
''' Pull returns the punches stored on the device with a time at or after since (all of them when since is Nothing). It may return punches the database already has: they are ignored there.
''' It throws PunchSourceException when the device cannot be read. The device's comm key, when it has one, is kept on the agent computer and never in the database.
''' </summary>
Public Interface IPunchSource
    ''' <summary>The brand this reader handles, as written in the device row, compared without case. For example ESSL.</summary>
    ReadOnly Property Brand As String

    Function Pull(device As DeviceEndpoint, since As Date?) As IReadOnlyList(Of RawPunch)
End Interface
