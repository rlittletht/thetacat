USE [thetasoft]
GO
	/****** Object:  Table [dbo].[tcat_metatags]    Script Date: 1/1/2024 2:55:53 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_metatags](
		[id] [uniqueidentifier] NOT NULL,
		[parent] [uniqueidentifier] NULL,
		[name] [nvarchar](50) NOT NULL,
		[description] [nvarchar](255) NOT NULL,
		[standard] [nvarchar](50) NOT NULL,
		CONSTRAINT [PK_tcat_metatags] PRIMARY KEY CLUSTERED ([id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO