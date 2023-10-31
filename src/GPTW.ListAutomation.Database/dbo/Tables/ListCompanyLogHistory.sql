CREATE TABLE [dbo].[ListCompanyLogHistory] (
    [ListCompanyLogId] INT           NOT NULL,
    [ListCompanyId]    INT           NOT NULL,
    [Description]      VARCHAR (MAX) NOT NULL,
    [CreatedDateTime]  DATETIME      NOT NULL,
    CONSTRAINT [PK_ListCompanyLogHistory] PRIMARY KEY CLUSTERED ([ListCompanyLogId] ASC),
    CONSTRAINT [FK_ListCompanyLogHistory_ListCompany] FOREIGN KEY ([ListCompanyId]) REFERENCES [dbo].[ListCompany] ([ListCompanyId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListCompanyLogHistory_ListCompanyId]
    ON [dbo].[ListCompanyLogHistory]([ListCompanyId] ASC);

