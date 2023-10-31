CREATE TABLE [dbo].[ListSurveyRespondentComments] (
    [ListSurveyRespondentCommentsId] INT           IDENTITY (1, 1) NOT NULL,
    [ListCompanyId]                  INT           NOT NULL,
    [RespondentKey]                  INT           NOT NULL,
    [Question]                       VARCHAR (MAX) NULL,
    [Response]                       VARCHAR (MAX) NULL,
    CONSTRAINT [PK_ListSurveyRespondentComments] PRIMARY KEY CLUSTERED ([ListSurveyRespondentCommentsId] ASC),
    CONSTRAINT [FK_ListSurveyRespondentComments_ListCompany] FOREIGN KEY ([ListCompanyId]) REFERENCES [dbo].[ListCompany] ([ListCompanyId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListSurveyRespondentDemographicsListSurveyRespondentComments_ListCompanyId]
    ON [dbo].[ListSurveyRespondentComments]([ListCompanyId] ASC);

