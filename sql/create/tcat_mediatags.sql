USE [thetasoft]
GO
	/****** Object:  Table [dbo].[tcat_mediatags]    Script Date: 1/1/2024 2:55:41 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_mediatags](
		[id] [uniqueidentifier] NOT NULL,
		[metatag] [uniqueidentifier] NOT NULL,
		[value] [varchar](1024) NULL,
		CONSTRAINT [PK_tcat_mediatags] PRIMARY KEY CLUSTERED ([id] ASC, [metatag] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO