CREATE TABLE [dbo].[ListCompanyOtherScore] (
    [ListCompanyAdditionalScoreId] INT            IDENTITY (1, 1) NOT NULL,
    [ListCompanyId]                INT            NOT NULL,
    [EvaluationTitle]              VARCHAR (5000) NOT NULL,
    [Score]                        INT            NULL,
    CONSTRAINT [PK_ListCompanyOtherScore] PRIMARY KEY CLUSTERED ([ListCompanyAdditionalScoreId] ASC)
);

