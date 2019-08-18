--USE [TestDb]
--GO

-- drop table dbo.Gunter_Test

BEGIN TRANSACTION

if (not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = 'dbo' and TABLE_NAME = 'Gunter_Test'))
begin
	
	SET ANSI_NULLS ON;
	SET QUOTED_IDENTIFIER ON;

	CREATE TABLE [dbo].[Gunter_Test]
	(
		[_id] [int] NOT NULL,
		[_string_text] [nvarchar](max) NULL,
		[_string_json] [nvarchar](max) NULL,
		[_string_misc] [nvarchar](max) NULL,
		[_bit] [bit] NULL,
		[_int] [int] NULL,
		[_float] [float] NULL,
		[_decimal] [decimal](18, 3) NULL,
		[_datetime2] [datetime2](7) NULL,
	 CONSTRAINT [PK_Gunter_Test] PRIMARY KEY CLUSTERED 
	 (
		[_id] ASC
	 )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

end

-- clear old data
truncate table [dbo].[Gunter_Test];

DECLARE @CRLF nvarchar(10) = char(13)+char(10);

-- insert new data
insert into [dbo].[Gunter_Test]([_id])
select [_id]
from (values (1),(2),(3),(4),(7)) v ([_id]);

-- INSERT INTO [dbo].[Gunter_Test]([_id], [_text], [_json], [_misc], [_flag], [_count], [_elapsed], [_price], [_timestamp])
-- SELECT [_id], [_text], [_json], [_misc], [_flag], [_count], [_elapsed], [_price], [_timestamp]
-- FROM (
-- 	VALUES		
-- 		
--      -- ok
-- 		(1, REPLACE(N'Line-1{CRLF}Line-2', '{CRLF}', @CRLF), '{"Name":"John"}', 'Hallo!', 'true', 2, 5.5, 5.25, '2018-05-01'), 
-- 		(2, REPLACE(N'Line-1{CRLF}Line-2', '{CRLF}', @CRLF), '{"Name":"Bill"}', '{"Greeting":"Hallo!"}', 'true', 2, 5.5, 5.25, '2018-05-01'), 
-- 		 -- invalid (where possible)
-- 		(3, '', 'foo', '', 'false', 3, 6.5, 6.25, '2018-05-02'), 
-- 		
--      -- null
-- 		(7, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL) 
-- 		
-- )sub ([_id], [_text], [_json], [_misc], [_flag], [_count], [_elapsed], [_price], [_timestamp]);

-- ok
update [dbo].[Gunter_Test]
set
    [_string_text] = REPLACE(N'Line-1{CRLF}Line-2', '{CRLF}', @CRLF),
    [_string_json] = '{"Name":"John"}',
    [_string_misc] = 'Hallo!',
    [_bit] = 'true',
    [_int] = 1,
    [_float] = 1.1,
    [_decimal] = 1.1,
    [_datetime2] = '2018-05-01 01:01:01'
WHERE [_id] = 1;

-- ok
update [dbo].[Gunter_Test]
set
  [_string_text] = REPLACE(N'Line-1{CRLF}Line-2', '{CRLF}', @CRLF),
  [_string_json] = '[{"Name":"Bill"}]',
  [_string_misc] = '{"Greeting":"Hallo!"}',
  [_bit] = 'true',
  [_int] = 2,
  [_float] = 2.2,
  [_decimal] = 2.2,
  [_datetime2] = '2018-05-02 02:02:02'
WHERE [_id] = 2;

-- empty
update [dbo].[Gunter_Test]
set
  [_string_text] = '',
  [_string_json] = '',
  [_string_misc] = '',
  [_bit] = ''
WHERE [_id] = 3;

-- invalid where possible
update [dbo].[Gunter_Test]
set
  [_string_json] = 'not-json'
WHERE [_id] = 4;


COMMIT;

-- display current data
SELECT * --[_id], [_text], [_json], [_misc], [_flag], [_count], [_elapsed], [_price], [_timestamp]
FROM [dbo].[Gunter_Test] 


