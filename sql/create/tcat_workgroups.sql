USE [thetasoft]
GO
	/****** Object:  Table [dbo].[tcat_workgroups]    Script Date: 1/1/2024 3:05:37 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_workgroups](
		[id] [uniqueidentifier] NOT NULL,
		[name] [nvarchar](128) NOT NULL,
		[serverPath] [nvarchar](256) NOT NULL,
		[cacheRoot] [nvarchar](1024) NOT NULL,
		CONSTRAINT [PK_tcat_workgroups] PRIMARY KEY CLUSTERED ([id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO