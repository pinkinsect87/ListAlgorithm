CREATE TABLE [dbo].[ListImportJobQueueLog] (
    [ListImportJobQueueId]    INT            NOT NULL,
    [ListImportJobQueueLogId] INT            IDENTITY (1, 1) NOT NULL,
    [LogImportLog]            VARCHAR (5000) NULL,
    [LogSeverity]             VARCHAR (10)   NULL,
    [EngagementId]            VARCHAR (100)  NOT NULL,
    [ClientId]                VARCHAR (100)  NOT NULL,
    [CreatedDate]             DATETIME       NOT NULL,
    [CreatedBy]               VARCHAR (200)  NOT NULL,
    [ModifiedBy]              VARCHAR (200)  NULL,
    [ModifedDate]             DATETIME       NULL,
    CONSTRAINT [PK_ListImportJobQueueLog] PRIMARY KEY CLUSTERED ([ListImportJobQueueLogId] ASC),
    CONSTRAINT [FK_ListImportJobQueueLog_ListImportJobQueue] FOREIGN KEY ([ListImportJobQueueId]) REFERENCES [dbo].[ListImportJobQueue] ([ListImportJobQueueId]),
    CONSTRAINT [FK_ListImportJobQueueLog_ListImportJobQueue1] FOREIGN KEY ([ListImportJobQueueId]) REFERENCES [dbo].[ListImportJobQueue] ([ListImportJobQueueId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListImportJobQueueLog_ListImportJobQueueId]
    ON [dbo].[ListImportJobQueueLog]([ListImportJobQueueId] ASC);

