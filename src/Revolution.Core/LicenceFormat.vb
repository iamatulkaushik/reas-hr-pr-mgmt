Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Text.Json

''' <summary>
''' The licence file: base64url(payload) + "." + base64url(signature) (ADR 0018). The payload is JSON; the signature is ECDSA P-256 over
''' the payload bytes exactly as carried in the file, so nothing has to be re-serialised before checking. Signing lives in the licence tool.
''' </summary>
Public NotInheritable Class LicenceFormat
    Private Const DateFormat As String = "yyyy-MM-dd"
    Private Const FormatVersion As Integer = 1

    Private Sub New()
    End Sub

    Public Shared Function PayloadBytes(grant As LicenceGrant) As Byte()
        If grant Is Nothing Then Throw New ArgumentNullException(NameOf(grant))
        Using stream As New MemoryStream()
            Using writer As New Utf8JsonWriter(stream)
                writer.WriteStartObject()
                writer.WriteNumber("v", FormatVersion)
                writer.WriteString("licenceId", grant.LicenceId)
                writer.WriteString("keyId", grant.KeyId)
                writer.WriteString("issuedBy", grant.IssuedBy)
                writer.WriteString("issuedOn", grant.IssuedOn.ToString(DateFormat, CultureInfo.InvariantCulture))
                writer.WriteString("installationId", grant.InstallationId)
                writer.WriteString("associate", grant.Associate)
                writer.WriteString("validFrom", grant.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture))
                writer.WriteString("validTo", grant.ValidTo.ToString(DateFormat, CultureInfo.InvariantCulture))
                writer.WriteString("planCap", grant.PlanCap.ToString())
                writer.WriteStartArray("modules")
                For Each licensed In grant.Modules
                    writer.WriteStringValue(licensed.ToString())
                Next
                writer.WriteEndArray()
                writer.WriteNumber("maxCompanies", grant.MaxCompanies)
                writer.WriteNumber("maxActiveUsers", grant.MaxActiveUsers)
                writer.WriteString("note", grant.Note)
                writer.WriteEndObject()
            End Using
            Return stream.ToArray()
        End Using
    End Function

    ''' <summary>Reads a payload. Call it on bytes whose signature has been checked. Throws FormatException when the content is not a licence.</summary>
    Public Shared Function ReadPayload(payload As Byte()) As LicenceGrant
        Try
            Using document = JsonDocument.Parse(Encoding.UTF8.GetString(payload))
                Dim root = document.RootElement
                Return New LicenceGrant(
                    root.GetProperty("licenceId").GetGuid(),
                    root.GetProperty("keyId").GetString(),
                    root.GetProperty("issuedBy").GetString(),
                    ReadDate(root, "issuedOn"),
                    root.GetProperty("installationId").GetGuid(),
                    root.GetProperty("associate").GetString(),
                    ReadDate(root, "validFrom"),
                    ReadDate(root, "validTo"),
                    ReadPlan(root.GetProperty("planCap").GetString()),
                    ReadModules(root.GetProperty("modules")),
                    root.GetProperty("maxCompanies").GetInt32(),
                    root.GetProperty("maxActiveUsers").GetInt32(),
                    root.GetProperty("note").GetString())
            End Using
        Catch ex As Exception When IsContentError(ex)
            Throw New FormatException("The licence content is not valid.", ex)
        End Try
    End Function

    Public Shared Function Encode(payload As Byte(), signature As Byte()) As String
        Return ToBase64Url(payload) & "." & ToBase64Url(signature)
    End Function

    Public Shared Function ToBase64Url(bytes As Byte()) As String
        Return Convert.ToBase64String(bytes).TrimEnd("="c).Replace("+"c, "-"c).Replace("/"c, "_"c)
    End Function

    ''' <summary>Throws FormatException when the text is not base64url.</summary>
    Public Shared Function FromBase64Url(text As String) As Byte()
        Dim padded = text.Replace("-"c, "+"c).Replace("_"c, "/"c)
        Select Case padded.Length Mod 4
            Case 2
                padded &= "=="
            Case 3
                padded &= "="
        End Select
        Return Convert.FromBase64String(padded)
    End Function

    Private Shared Function ReadDate(root As JsonElement, name As String) As DateOnly
        Return DateOnly.ParseExact(root.GetProperty(name).GetString(), DateFormat, CultureInfo.InvariantCulture)
    End Function

    Private Shared Function ReadPlan(text As String) As PlanTier
        Dim plan As PlanTier
        If Not System.Enum.TryParse(text, False, plan) OrElse Not System.Enum.IsDefined(GetType(PlanTier), plan) Then
            Throw New FormatException("Unknown plan.")
        End If
        Return plan
    End Function

    ''' <summary>Module names this app does not know are ignored, so a newer licence still opens on an older build.</summary>
    Private Shared Function ReadModules(element As JsonElement) As List(Of ModuleKey)
        Dim result As New List(Of ModuleKey)()
        For Each item In element.EnumerateArray()
            Dim licensed As ModuleKey
            If System.Enum.TryParse(item.GetString(), False, licensed) AndAlso System.Enum.IsDefined(GetType(ModuleKey), licensed) Then
                result.Add(licensed)
            End If
        Next
        Return result
    End Function

    Private Shared Function IsContentError(ex As Exception) As Boolean
        Return TypeOf ex Is JsonException OrElse TypeOf ex Is KeyNotFoundException OrElse TypeOf ex Is InvalidOperationException _
            OrElse TypeOf ex Is FormatException OrElse TypeOf ex Is ArgumentException
    End Function
End Class
