--USE [TestDb]
--GO

BEGIN TRANSACTION

if (not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = 'dbo' and TABLE_NAME = 'Gunter_Example'))
begin
	
	SET ANSI_NULLS ON;
	SET QUOTED_IDENTIFIER ON;

	CREATE TABLE [dbo].[Gunter_Example]
	(
		[_id] [int] NOT NULL,
		[_text] [nvarchar](max) NULL,
		[_flag] [bit] NULL,
		[_count] [int] NULL,
		[_distance] [float] NULL,
		[_price] [decimal](18, 3) NULL,
		[_timestamp] [datetime2](7) NULL,
	 CONSTRAINT [PK_Gunter_Example] PRIMARY KEY CLUSTERED 
	 (
		[_id] ASC
	 )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

end

--- Clear settings.
DELETE FROM [dbo].[Gunter_Example];

--- Update settings
INSERT INTO [dbo].[Gunter_Example]([_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp])
SELECT [_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp]
FROM (
	VALUES		
		
		(1, 'blub', 'true', 123, 1.2345, 1.2345, '2018-05-01'),
		(2, '{"Color":"Blue"}', 'true', 123, 1.2345, 1.2345, '2018-05-01'),
		

		-- It's here so we don't have to think about the last comma.
		(1000, NULL, NULL, NULL, NULL, NULL, NULL)
)sub ([_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp]);

COMMIT;

SELECT [_id], [_text], [_flag], [_count], [_distance], [_price], [_timestamp]
FROM [dbo].[Gunter_Example] 


