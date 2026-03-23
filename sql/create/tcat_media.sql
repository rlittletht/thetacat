/****** Object:  Table [dbo].[tcat_media]    Script Date: 2/19/2025 4:29:13 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_media](
		[catalog_id] [uniqueidentifier] NOT NULL,
		[id] [uniqueidentifier] NOT NULL,
		[virtualPath] [varchar](1024) NOT NULL,
		[mimeType] [nvarchar](32) NOT NULL,
		[state] [nvarchar](16) NOT NULL,
		[md5] [nvarchar](32) NOT NULL,
		CONSTRAINT [PK_tcat_media] PRIMARY KEY CLUSTERED ([catalog_id] ASC, [id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO