CREATE TABLE [dbo].[ListAutomationResult] (
    [ListAutomationResultId] INT             IDENTITY (1, 1) NOT NULL,
    [ResultKey]              VARCHAR (5000)  NOT NULL,
    [ResultValue]            VARCHAR (MAX)   NULL,
    [ListCompanyId]          INT             NOT NULL,
    [CalculatedDate]         DATETIME        NULL,
    [Variation]              DECIMAL (18, 2) NULL,
    [CalculationNotes]       VARCHAR (5000)  NULL,
    [CalculationStatus]      NCHAR (10)      NULL,
    [IsCurrent]              BIT             NOT NULL,
    [InternalNotes]          VARCHAR (MAX)   NULL,
    CONSTRAINT [PK_ListAutomationResult] PRIMARY KEY CLUSTERED ([ListAutomationResultId] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListAutomationResult_ListCompanyId]
    ON [dbo].[ListAutomationResult]([ListCompanyId] ASC);

