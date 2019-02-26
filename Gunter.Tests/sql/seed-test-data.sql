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
		[_json] [nvarchar](max) NULL,
		[_misc] [nvarchar](max) NULL,
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

DECLARE @CRLF nvarchar(10) = char(13)+char(10);

--- Update settings
INSERT INTO [dbo].[Gunter_Test]([_id], [_text], [_json], [_misc], [_flag], [_count], [_distance], [_price], [_timestamp])
SELECT [_id], [_text], [_json], [_misc], [_flag], [_count], [_distance], [_price], [_timestamp]
FROM (
	VALUES		
		
		(1, REPLACE(N'Line-1{CRLF}Line-2', '{CRLF}', @CRLF), '{"Name":"John"}', 'Hallo!', 'true', 2, 5.5, 5.25, '2018-05-01'), -- ok
		(2, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL), -- null
		(3, '', 'Not-json', '', 'false', 3, 6.5, 6.25, '2018-05-02'),     -- invalid
		(4, REPLACE(N'Line-1{CRLF}Line-2', '{CRLF}', @CRLF),'{"Name":"Bill"}', '{"Greeting":"Hallo!"}', 'true', 2, 5.5, 5.25, '2018-05-01'), -- ok
		

		-- It's here so we don't have to think about the last comma.
		(1000, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL)
)sub ([_id], [_text], [_json], [_misc], [_flag], [_count], [_distance], [_price], [_timestamp]);

COMMIT;

SELECT [_id], [_text], [_json], [_misc], [_flag], [_count], [_distance], [_price], [_timestamp]
FROM [dbo].[Gunter_Test] 


