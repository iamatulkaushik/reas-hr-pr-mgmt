/* 0010_cfg_setting.sql
   Installation-wide settings (key and value). Same for every company. Idempotent. Never edit after merge.
   The app can read settings. Changing one is a setup-tool action, so the app login is denied the write procedure (like 0007).
   Fixed rules are NOT settings: dates are dd-MM-yyyy and the financial year is 1 April to 31 March (CLAUDE.md). */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'cfg.Setting', N'U') IS NULL
BEGIN
    CREATE TABLE cfg.Setting
    (
        SettingKey   NVARCHAR(100) NOT NULL CONSTRAINT PK_Setting PRIMARY KEY,
        SettingValue NVARCHAR(500) NOT NULL,
        Description  NVARCHAR(300) NULL,
        UpdatedUtc   DATETIME2(0)  NOT NULL CONSTRAINT DF_Setting_UpdatedUtc DEFAULT SYSUTCDATETIME()
    );
END
GO
INSERT cfg.Setting (SettingKey, SettingValue, Description)
SELECT v.SettingKey, v.SettingValue, v.Description
FROM (VALUES
    (N'RateVerificationRequired', N'1', N'1 = payroll refuses statutory rates that are not marked Verified in stat.* (CA sign-off gate).')
) v (SettingKey, SettingValue, Description)
WHERE NOT EXISTS (SELECT 1 FROM cfg.Setting s WHERE s.SettingKey = v.SettingKey);
GO
CREATE OR ALTER PROCEDURE cfg.usp_Setting_Get @SettingKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SettingValue FROM cfg.Setting WHERE SettingKey = @SettingKey;
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Setting_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SettingKey, SettingValue, Description FROM cfg.Setting ORDER BY SettingKey;
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Setting_Set @SettingKey NVARCHAR(100), @SettingValue NVARCHAR(500), @Description NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE cfg.Setting
    SET SettingValue = @SettingValue, Description = COALESCE(@Description, Description), UpdatedUtc = SYSUTCDATETIME()
    WHERE SettingKey = @SettingKey;
    IF @@ROWCOUNT = 0
        INSERT cfg.Setting (SettingKey, SettingValue, Description) VALUES (@SettingKey, @SettingValue, @Description);
END
GO
DENY EXECUTE ON OBJECT::cfg.usp_Setting_Set TO hrms_app;
GO
