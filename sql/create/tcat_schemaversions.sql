/****** Object:  Table [dbo].[tcat_schemaversions]    Script Date: 2/19/2024 2:48:29 PM ******/
SET
	ANSI_NULLS ON
GO
SET
	QUOTED_IDENTIFIER ON
GO
	CREATE TABLE [dbo].[tcat_schemaversions](
		[catalog_id] [uniqueidentifier] NOT NULL,
		[metatag_schema_version] [int] NOT NULL,
		CONSTRAINT [PK_tcat_schemaversions] PRIMARY KEY CLUSTERED ([catalog_id] ASC) WITH (
			PAD_INDEX = OFF,
			STATISTICS_NORECOMPUTE = OFF,
			IGNORE_DUP_KEY = OFF,
			ALLOW_ROW_LOCKS = ON,
			ALLOW_PAGE_LOCKS = ON,
			OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
		) ON [PRIMARY]
	) ON [PRIMARY]
GO