/****** Object:  Table [dbo].[tcat_catalog]    Script Date: 2/19/2024 2:27:03 PM ******/
SET
    ANSI_NULLS ON
GO
SET
    QUOTED_IDENTIFIER ON
GO
    CREATE TABLE [dbo].[tcat_catalogs](
        [id] [uniqueidentifier] NOT NULL,
        [name] [nvarchar](50) NOT NULL,
        [description] [nvarchar](256) NOT NULL,
        CONSTRAINT [PK_tcat_catalogs] PRIMARY KEY CLUSTERED ([id] ASC) WITH (
            PAD_INDEX = OFF,
            STATISTICS_NORECOMPUTE = OFF,
            IGNORE_DUP_KEY = OFF,
            ALLOW_ROW_LOCKS = ON,
            ALLOW_PAGE_LOCKS = ON,
            OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
        ) ON [PRIMARY]
    ) ON [PRIMARY]
GO