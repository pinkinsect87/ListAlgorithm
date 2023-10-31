CREATE TABLE [dbo].[ListAutomationJobQueue] (
    [ListAutomationJobQueueId] INT            IDENTITY (1, 1) NOT NULL,
    [ListRequestId]            INT            NOT NULL,
    [CreateDateTime]           DATETIME       NOT NULL,
    [ProcessedDateTime]        DATETIME       NULL,
    [ListAutomationResultId]   INT            NULL,
    [StatusId]                 INT            NOT NULL,
    [Reason]                   VARCHAR (5000) NULL,
    CONSTRAINT [PK_ListAutomationJobQueue] PRIMARY KEY CLUSTERED ([ListAutomationJobQueueId] ASC),
    CONSTRAINT [FK_ListAutomationJobQueue_Status] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[Status] ([StatusId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListAutomationJobQueue_StatusId]
    ON [dbo].[ListAutomationJobQueue]([StatusId] ASC);

