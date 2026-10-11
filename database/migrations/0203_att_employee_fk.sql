/* 0203_att_employee_fk.sql
   Adds the employee key promised in 0200: att.AttendanceDaily (CompanyID, EmployeeID) must be an employee of the same company.
   Composite, so a day row can never point at another company's employee. Needs mst.Employee (0103). Idempotent.
   Fails if attendance rows already exist for employees that are not in mst.Employee: clean those first. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AttendanceDaily_Employee')
    ALTER TABLE att.AttendanceDaily ADD CONSTRAINT FK_AttendanceDaily_Employee
        FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID);
GO
