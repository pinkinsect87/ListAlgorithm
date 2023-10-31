CREATE TABLE [dbo].[ListCompanyCultureBrief] (
    [ListCompanyCultureBriefId] INT           IDENTITY (1, 1) NOT NULL,
    [VariableName]              VARCHAR (MAX) NOT NULL,
    [VariableValue]             VARCHAR (MAX) NOT NULL,
    [ListCompanyId]             INT           NOT NULL,
    CONSTRAINT [PK_ListCompanyCultureBrief] PRIMARY KEY CLUSTERED ([ListCompanyCultureBriefId] ASC),
    CONSTRAINT [FK_ListCompanyCultureBrief_ListCompany] FOREIGN KEY ([ListCompanyId]) REFERENCES [dbo].[ListCompany] ([ListCompanyId]),
    CONSTRAINT [FK_ListCompanyCultureBrief_ListCompanyCultureBrief] FOREIGN KEY ([ListCompanyCultureBriefId]) REFERENCES [dbo].[ListCompanyCultureBrief] ([ListCompanyCultureBriefId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListCompanyCultureBrief_ListCompanyId]
    ON [dbo].[ListCompanyCultureBrief]([ListCompanyId] ASC);

