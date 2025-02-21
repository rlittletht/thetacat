/****** Object:  Table [dbo].[tcat_workgroups]    Script Date: 2/19/2025 4:43:42 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_workgroups](
		[catalog_id] [uniqueidentifier] NOT NULL,
		[id] [uniqueidentifier] NOT NULL,
		[name] [nvarchar](128) NOT NULL,
		[serverPath] [nvarchar](256) NOT NULL,
		[cacheRoot] [nvarchar](1024) NOT NULL,
		[deletedMediaClock] [int] NOT NULL,
		CONSTRAINT [PK_tcat_workgroups] PRIMARY KEY CLUSTERED ([catalog_id] ASC, [id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO