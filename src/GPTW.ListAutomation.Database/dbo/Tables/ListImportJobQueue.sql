CREATE TABLE [dbo].[ListImportJobQueue] (
    [ListImportJobQueueId] INT      IDENTITY (1, 1) NOT NULL,
    [ListRequestId]        INT      NOT NULL,
    [CreatedDate]          DATETIME NOT NULL,
    [ProcessedDate]        DATETIME NOT NULL,
    [StatusId]             INT      NOT NULL,
    CONSTRAINT [PK_ListImportJobQueue] PRIMARY KEY CLUSTERED ([ListImportJobQueueId] ASC),
    CONSTRAINT [FK_ListImportJobQueue_ListRequest] FOREIGN KEY ([ListRequestId]) REFERENCES [dbo].[ListRequest] ([ListRequestId]),
    CONSTRAINT [FK_ListImportJobQueue_Status] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[Status] ([StatusId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListImportJobQueue_ListRequestId]
    ON [dbo].[ListImportJobQueue]([ListRequestId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListImportJobQueue_StatusId]
    ON [dbo].[ListImportJobQueue]([StatusId] ASC);

