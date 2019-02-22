--USE [TestDb]
--GO

BEGIN TRANSACTION

if (not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = 'dbo' and TABLE_NAME = 'Gunter_Test'))
begin
	
	SET ANSI_NULLS ON;
	SET QUOTED_IDENTIFIER ON;

	CREATE TABLE [dbo].[Gunter_Test]
	(
		[_id] [int] NOT NULL,
		[_text] [nvarchar](max) NULL,
		[_flag] [bit] NULL,
		[_count] [int] NULL,
		[_distance] [float] NULL,
		[_price] [decimal](18, 3) NULL,
		[_timestamp] [datetime2](7) NULL,
	 CONSTRAINT [PK_Gunter_Test] PRIMARY KEY CLUSTERED 
	 (
		[_id] ASC
	 )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

end

--- Clear settings.
DELETE FROM [dbo].[Gunter_Test];

--- Update settings
INSERT INTO [dbo].[Gunter_Test]([_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp])
SELECT [_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp]
FROM (
	VALUES		
		
		(1, 'Hallo!', 'true', 10, 10.5, 10.25, '2018-05-01'),
		(2, '{"Name":"John"}', 'false', 20, 10.5, 10.25, '2018-05-02'),
		

		-- It's here so we don't have to think about the last comma.
		(1000, NULL, NULL, NULL, NULL, NULL, NULL)
)sub ([_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp]);

COMMIT;

SELECT [_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp]
FROM [dbo].[Gunter_Test] 


