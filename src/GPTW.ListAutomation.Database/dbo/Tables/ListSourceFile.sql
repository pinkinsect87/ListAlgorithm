CREATE TABLE [dbo].[ListSourceFile] (
    [ListSourceFileId] INT              IDENTITY (1, 1) NOT NULL,
    [ListRequestId]    INT              NOT NULL,
    [FileType]         VARCHAR (50)     NOT NULL,
    [StorageAccountId] UNIQUEIDENTIFIER NULL,
    [UploadedDateTime] DATETIME         NULL,
    [ModifiedDateTime] DATETIME         NULL,
    [UploadedBy]       VARCHAR (100)    NULL,
    [ModifiedBy]       VARCHAR (100)    NULL,
    CONSTRAINT [PK_ListSourceFile] PRIMARY KEY CLUSTERED ([ListSourceFileId] ASC),
    CONSTRAINT [FK_ListSourceFile_ListRequest] FOREIGN KEY ([ListRequestId]) REFERENCES [dbo].[ListRequest] ([ListRequestId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListSourceFile_ListRequestId]
    ON [dbo].[ListSourceFile]([ListRequestId] ASC);

