/****** Object:  Table [dbo].[tcat_stackmedia]    Script Date: 1/1/2024 2:56:34 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_stackmedia](
		[catalog_id] [uniqueidentifier] NOT NULL,
		[id] [uniqueidentifier] NOT NULL,
		[media_id] [uniqueidentifier] NOT NULL,
		[orderHint] [int] NOT NULL,
		CONSTRAINT [PK_tcat_stacks] PRIMARY KEY CLUSTERED ([catalog_id] ASC, [id] ASC, [media_id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO