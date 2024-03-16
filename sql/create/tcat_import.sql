/****** Object:  Table [dbo].[tcat_import]    Script Date: 1/1/2024 2:53:02 PM ******/
SET
    ANSI_NULLS ON
GO
SET
    QUOTED_IDENTIFIER ON
GO
    CREATE TABLE [dbo].[tcat_import](
		[catalog_id] [uniqueidentifier] NOT NULL,
        [id] [uniqueidentifier] NOT NULL,
        [state] [nvarchar](16) NOT NULL,
        [sourcePath] [varchar](1024) NOT NULL,
        [sourceServer] [varchar](1024) NOT NULL,
        [uploadDate] [datetimeoffset](7) NULL,
        [source] [nvarchar](16) NULL,
        CONSTRAINT [PK_tcat_migration] PRIMARY KEY CLUSTERED ([catalog_id] ASC, [id] ASC) WITH (
            PAD_INDEX = OFF,
            STATISTICS_NORECOMPUTE = OFF,
            IGNORE_DUP_KEY = OFF,
            ALLOW_ROW_LOCKS = ON,
            ALLOW_PAGE_LOCKS = ON,
            OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
        ) ON [PRIMARY]
    ) ON [PRIMARY]
GO