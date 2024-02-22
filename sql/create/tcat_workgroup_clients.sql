/****** Object:  Table [dbo].[tcat_workgroup_clients]    Script Date: 1/1/2024 3:01:26 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_workgroup_clients](
		[catalog_id] [uniqueidentifier] NOT NULL,
		[id] [uniqueidentifier] NOT NULL,
		[name] [varchar](128) NOT NULL,
		[authID] [uniqueidentifier] NULL,
		CONSTRAINT [PK_tcat_workgroup_clients] PRIMARY KEY CLUSTERED ([id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO