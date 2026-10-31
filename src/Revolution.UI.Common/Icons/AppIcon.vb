''' <summary>
''' Every icon the app can show (ADR 0020). Only Sigma adds members; Brave asks through a change request.
''' Members may share one glyph on purpose (Close and Cancel, Backup and Database). Call sites say what they mean.
''' A screen or button with no member here shows text only.
''' </summary>
Public Enum AppIcon
    ' General
    Add
    Edit
    Delete
    Save
    Cancel
    Close
    Search
    Refresh
    Print
    Download
    Upload
    Import
    Export
    Home
    Settings
    Employee
    Calendar
    Mail
    Accept
    ' Shell
    Back
    Forward
    Help
    Info
    Warning
    [Error]
    Busy
    Filter
    Copy
    Menu
    Dashboard
    Expand
    Collapse
    ' Security, roles and plans
    Login
    Logout
    User
    Password
    Key
    Lock
    Unlock
    Shield
    RoleDeveloper
    RoleOwner
    RoleAssociate
    RoleOperator
    PlanBadge
    PlanLocked
    Company
    CompanySwitch
    DualApproval
    History
    ' Masters and employees
    Department
    Designation
    Grade
    IdCard
    Bank
    Address
    Location
    Masters
    Phone
    ' Attendance
    Attendance
    Clock
    LossOfPay
    Leave
    Holiday
    Present
    Absent
    Regularize
    ' Payroll
    Payroll
    Rupee
    SalaryStructure
    Epf
    Esi
    Payslip
    PayslipRun
    Calculator
    Report
    ' Tax
    Declaration
    ' Closing
    BankFile
    [Exit]
    ' Biometric agent
    Biometric
    Device
    Sync
    AgentOnline
    AgentOffline
    ' Assets
    Asset
    Receipt
    ' Hardening
    Database
    Backup
    Restore
    Preview
    Update
    About
    Pdf
    ' Brand
    AppLogo
End Enum
