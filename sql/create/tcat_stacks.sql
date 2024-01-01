USE [thetasoft]
GO
	/****** Object:  Table [dbo].[tcat_stacks]    Script Date: 1/1/2024 2:56:43 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_stacks](
		[id] [uniqueidentifier] NOT NULL,
		[stackType] [nvarchar](50) NOT NULL,
		[description] [varchar](256) NOT NULL,
		CONSTRAINT [PK_tcat_stacks_1] PRIMARY KEY CLUSTERED ([id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO