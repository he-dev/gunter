--USE [TestDb]
--GO

BEGIN TRANSACTION

if (not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = 'dbo' and TABLE_NAME = 'Gunter_Test'))
begin
	
	SET ANSI_NULLS ON;
	SET QUOTED_IDENTIFIER ON;

	CREATE TABLE [dbo].[Gunter_Test](
		[_id] [int] NOT NULL,
		[_nvarchar] [nvarchar](max) NULL,
		[_float] [float] NULL,
		[_int] [int] NULL,
		[_datetime] [datetime2](7) NULL,
		[_decimal] [decimal](18, 0) NULL,
		[_bit] [bit] NULL,
	 CONSTRAINT [PK_Gunter_Test] PRIMARY KEY CLUSTERED 
	 (
		[_id] ASC
	 )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

end

--- Clear settings.
DELETE FROM [dbo].[Gunter_Test];

--- Update settings
INSERT INTO [dbo].[Gunter_Test]([_id], [_nvarchar], [_datetime], [_float], [_int], [_decimal], [_bit])
SELECT [_id], [_nvarchar], [_datetime], [_float], [_int], [_decimal], [_bit]
FROM (
	VALUES		
		
		(1, 'foo', '2018-05-01', 3.1, 7, 5.5, 'true'),
		

		-- It's here so we don't have to think about the last comma.
		(1000, null, null, null, null, null, null)
)sub ([_id], [_nvarchar], [_datetime], [_float], [_int], [_decimal], [_bit]);

COMMIT;

SELECT [_id], [_nvarchar], [_datetime], [_float], [_int], [_decimal], [_bit]
FROM [dbo].[Gunter_Test] 


