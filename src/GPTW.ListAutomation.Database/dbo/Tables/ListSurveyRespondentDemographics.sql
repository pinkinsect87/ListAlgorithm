CREATE TABLE [dbo].[ListSurveyRespondentDemographics] (
    [ListCompanyDemographicsId]         INT           IDENTITY (1, 1) NOT NULL,
    [ListCompanyId]                     INT           NOT NULL,
    [RespondentKey]                     INT           NOT NULL,
    [Gender]                            VARCHAR (500) NULL,
    [Age]                               VARCHAR (500) NULL,
    [CountryRegion]                     VARCHAR (500) NULL,
    [JobLevel]                          VARCHAR (500) NULL,
    [LgbtOrLgbtQ]                       VARCHAR (500) NULL,
    [RaceEthniticity]                   VARCHAR (500) NULL,
    [Responsibility]                    VARCHAR (500) NULL,
    [Tenure]                            VARCHAR (500) NULL,
    [WorkStatus]                        VARCHAR (500) NULL,
    [WorkType]                          VARCHAR (500) NULL,
    [WorkerType]                        VARCHAR (500) NULL,
    [BirthYear]                         VARCHAR (500) NULL,
    [Confidence]                        VARCHAR (500) NULL,
    [Disabilities]                      VARCHAR (500) NULL,
    [ManagerialLevel]                   VARCHAR (500) NULL,
    [MeaningfulInnovationOpportunities] VARCHAR (500) NULL,
    [PayType]                           VARCHAR (500) NULL,
    [Zipcode]                           VARCHAR (500) NULL,
    [CreatedDateTime]                   DATETIME      NOT NULL,
    [ModifiedDateTime]                  DATETIME      NOT NULL,
    CONSTRAINT [PK_ListCompanyDemographic] PRIMARY KEY CLUSTERED ([ListCompanyDemographicsId] ASC),
    CONSTRAINT [FK_ListCompanyDemographic_ListCompany] FOREIGN KEY ([ListCompanyId]) REFERENCES [dbo].[ListCompany] ([ListCompanyId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListSurveyRespondentDemographics_ListCompanyId]
    ON [dbo].[ListSurveyRespondentDemographics]([ListCompanyId] ASC);

