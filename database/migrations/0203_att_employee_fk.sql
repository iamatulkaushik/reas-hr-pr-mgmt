/* 0203_att_employee_fk.sql
   Real foreign key from attendance to mst.Employee (0100). Composite on (CompanyID, EmployeeID), so a row can never point at another company's employee.
   Brave lane 0200-0299 (ADR 0008 addendum). Needs 0100 to be applied first; numeric order guarantees it. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AttendanceDaily_Employee')
    ALTER TABLE att.AttendanceDaily ADD CONSTRAINT FK_AttendanceDaily_Employee
        FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID);
GO
