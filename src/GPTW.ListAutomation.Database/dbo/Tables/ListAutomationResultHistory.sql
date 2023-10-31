CREATE TABLE [dbo].[ListAutomationResultHistory] (
    [ListAutomationResultHistoryId] INT             NOT NULL,
    [ListAutomationResultId]        INT             NOT NULL,
    [ResultKey]                     VARCHAR (5000)  NOT NULL,
    [ResultValue]                   DECIMAL (18, 2) NOT NULL,
    [ListAutomationJobQueueId]      INT             NOT NULL,
    [CalculationDate]               DATETIME        NULL,
    [Varition]                      DECIMAL (18, 2) NULL,
    [Exceptions]                    VARCHAR (5000)  NULL
);

