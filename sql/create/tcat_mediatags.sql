/****** Object:  Table [dbo].[tcat_mediatags]    Script Date: 2/19/2025 4:29:30 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_mediatags](
		[catalog_id] [uniqueidentifier] NOT NULL,
		[id] [uniqueidentifier] NOT NULL,
		[metatag] [uniqueidentifier] NOT NULL,
		[clock] [int] NOT NULL,
		[deleted] [bit] NOT NULL,
		[value] [varchar](1024) NULL,
		CONSTRAINT [PK_tcat_mediatags_new] PRIMARY KEY CLUSTERED (
			[catalog_id] ASC,
			[id] ASC,
			[metatag] ASC
		) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO