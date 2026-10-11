/* 0103_mst_state.sql
   State master (36 states and union territories, GST state codes) and the factory's state. Brave lane 0100-0199. Idempotent.
   Needed for professional tax and ESI by state (Phase 5). Reference data, so no tenant filter. Read-only through mst.usp_State_List.
   Statutory rates themselves belong to the stat lane (0400), not here. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'mst.State', N'U') IS NULL
BEGIN
    CREATE TABLE mst.State
    (
        StateID   TINYINT       NOT NULL CONSTRAINT PK_State PRIMARY KEY,
        StateName NVARCHAR(60)  NOT NULL CONSTRAINT UQ_State_Name UNIQUE,
        IsUnionTerritory BIT    NOT NULL CONSTRAINT DF_State_IsUT DEFAULT 0
    );
END
GO
INSERT mst.State (StateID, StateName, IsUnionTerritory)
SELECT v.StateID, v.StateName, v.IsUT
FROM (VALUES
    (1, N'Jammu and Kashmir', 1), (2, N'Himachal Pradesh', 0), (3, N'Punjab', 0), (4, N'Chandigarh', 1),
    (5, N'Uttarakhand', 0), (6, N'Haryana', 0), (7, N'Delhi', 1), (8, N'Rajasthan', 0), (9, N'Uttar Pradesh', 0),
    (10, N'Bihar', 0), (11, N'Sikkim', 0), (12, N'Arunachal Pradesh', 0), (13, N'Nagaland', 0), (14, N'Manipur', 0),
    (15, N'Mizoram', 0), (16, N'Tripura', 0), (17, N'Meghalaya', 0), (18, N'Assam', 0), (19, N'West Bengal', 0),
    (20, N'Jharkhand', 0), (21, N'Odisha', 0), (22, N'Chhattisgarh', 0), (23, N'Madhya Pradesh', 0), (24, N'Gujarat', 0),
    (26, N'Dadra and Nagar Haveli and Daman and Diu', 1), (27, N'Maharashtra', 0), (29, N'Karnataka', 0), (30, N'Goa', 0),
    (31, N'Lakshadweep', 1), (32, N'Kerala', 0), (33, N'Tamil Nadu', 0), (34, N'Puducherry', 1),
    (35, N'Andaman and Nicobar Islands', 1), (36, N'Telangana', 0), (37, N'Andhra Pradesh', 0), (38, N'Ladakh', 1)
) AS v (StateID, StateName, IsUT)
WHERE NOT EXISTS (SELECT 1 FROM mst.State s WHERE s.StateID = v.StateID);
GO
IF COL_LENGTH(N'mst.Factory', N'StateID') IS NULL
    ALTER TABLE mst.Factory ADD StateID TINYINT NULL CONSTRAINT FK_Factory_State REFERENCES mst.State (StateID);
GO
CREATE OR ALTER PROCEDURE mst.usp_State_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StateID, StateName, IsUnionTerritory FROM mst.State ORDER BY StateName;
END
GO
