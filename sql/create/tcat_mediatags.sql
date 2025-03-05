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

/****** Object:  Index [idx_tcat_mediatags_new_clock]    Script Date: 3/4/2025 11:07:49 AM ******/
CREATE NONCLUSTERED INDEX [idx_tcat_mediatags_new_clock] ON [dbo].[tcat_mediatags]
(
	[catalog_id] ASC,
	[clock] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
