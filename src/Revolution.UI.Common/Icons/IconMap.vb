''' <summary>The only place that knows which codepoint of the icon font draws which AppIcon.</summary>
Friend Module IconMap
    Private ReadOnly Codes As New Dictionary(Of AppIcon, Integer) From {
        {AppIcon.Add, &H2B}, {AppIcon.Edit, &HFF}, {AppIcon.Delete, &H102}, {AppIcon.Save, &HF5},
        {AppIcon.Cancel, &H101}, {AppIcon.Close, &H101}, {AppIcon.Search, &HF6}, {AppIcon.Refresh, &H828},
        {AppIcon.Print, &H103}, {AppIcon.Download, &H106}, {AppIcon.Upload, &H809}, {AppIcon.Import, &H1D4},
        {AppIcon.Export, &H118}, {AppIcon.Home, &HE7}, {AppIcon.Settings, &H172}, {AppIcon.Employee, &H21B},
        {AppIcon.Calendar, &H23C}, {AppIcon.Mail, &HF2}, {AppIcon.Accept, &H100}, {AppIcon.Back, &H2190},
        {AppIcon.Forward, &H2192}, {AppIcon.Help, &HA7}, {AppIcon.Info, &HE8}, {AppIcon.Warning, &HA4},
        {AppIcon.Error, &H21}, {AppIcon.Busy, &H478}, {AppIcon.Filter, &H187}, {AppIcon.Copy, &H1FC},
        {AppIcon.Menu, &H188}, {AppIcon.Dashboard, &H406}, {AppIcon.Expand, &H2197}, {AppIcon.Collapse, &H2196},
        {AppIcon.Login, &HFD}, {AppIcon.Logout, &HFE}, {AppIcon.User, &H805}, {AppIcon.Password, &H185},
        {AppIcon.Key, &H185}, {AppIcon.Lock, &HF9}, {AppIcon.Unlock, &HEB}, {AppIcon.Shield, &H175},
        {AppIcon.RoleDeveloper, &H1DB}, {AppIcon.RoleOwner, &H1DD}, {AppIcon.RoleAssociate, &H1DC}, {AppIcon.RoleOperator, &H1DE},
        {AppIcon.PlanBadge, &H1E0}, {AppIcon.PlanLocked, &H1E1}, {AppIcon.Company, &H116}, {AppIcon.CompanySwitch, &H215},
        {AppIcon.DualApproval, &H108}, {AppIcon.History, &H7F0}, {AppIcon.Department, &HED}, {AppIcon.Designation, &H21D},
        {AppIcon.Grade, &H222}, {AppIcon.IdCard, &H114}, {AppIcon.Bank, &H1D2}, {AppIcon.Address, &H196},
        {AppIcon.Location, &H196}, {AppIcon.Masters, &H17B}, {AppIcon.Phone, &H124}, {AppIcon.Attendance, &H193},
        {AppIcon.Clock, &H7F0}, {AppIcon.LossOfPay, &H139}, {AppIcon.Leave, &H823}, {AppIcon.Holiday, &H240},
        {AppIcon.Present, &H220}, {AppIcon.Absent, &H21F}, {AppIcon.Regularize, &H23D}, {AppIcon.Payroll, &H822},
        {AppIcon.Rupee, &H20B9}, {AppIcon.SalaryStructure, &H233}, {AppIcon.Epf, &H10C}, {AppIcon.Esi, &H10D},
        {AppIcon.Payslip, &H1B5}, {AppIcon.PayslipRun, &H252}, {AppIcon.Calculator, &H816}, {AppIcon.Report, &H112},
        {AppIcon.Declaration, &H41B}, {AppIcon.BankFile, &H1D2}, {AppIcon.Exit, &HFE}, {AppIcon.Biometric, &H12C},
        {AppIcon.Device, &H14A}, {AppIcon.Sync, &H170}, {AppIcon.AgentOnline, &H1C1}, {AppIcon.AgentOffline, &H1C2},
        {AppIcon.Asset, &H1C4}, {AppIcon.Receipt, &H1B5}, {AppIcon.Database, &HF0}, {AppIcon.Backup, &HF0},
        {AppIcon.Restore, &HF0}, {AppIcon.Preview, &HF0}, {AppIcon.Update, &H106}, {AppIcon.About, &HE8},
        {AppIcon.Pdf, &H1CE}, {AppIcon.AppLogo, &HE003}
    }

    ''' <summary>The glyph for an icon. Throws if the icon has no mapping, so a new enum member cannot ship without one.</summary>
    Friend Function GlyphFor(icon As AppIcon) As String
        Dim code As Integer
        If Not Codes.TryGetValue(icon, code) Then
            Throw New InvalidOperationException($"No glyph is mapped for {icon}.")
        End If
        Return ChrW(code)
    End Function
End Module
