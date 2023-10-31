CREATE TABLE [dbo].[ListAutomationLog] (
    [ListAutomationLogId]      INT            IDENTITY (1, 1) NOT NULL,
    [ListAutomationJobQueueId] INT            NOT NULL,
    [LogInfo]                  VARCHAR (5000) NULL,
    [CreatedDate]              DATETIME       NULL,
    [Severity]                 VARCHAR (50)   NULL,
    CONSTRAINT [PK_ListAutomationLog] PRIMARY KEY CLUSTERED ([ListAutomationLogId] ASC),
    CONSTRAINT [FK_ListAutomationLog_ListAutomationLog] FOREIGN KEY ([ListAutomationJobQueueId]) REFERENCES [dbo].[ListAutomationJobQueue] ([ListAutomationJobQueueId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListAutomationLog_ListAutomationJobQueueId]
    ON [dbo].[ListAutomationLog]([ListAutomationJobQueueId] ASC);

