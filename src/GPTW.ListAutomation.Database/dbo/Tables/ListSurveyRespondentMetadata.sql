CREATE TABLE [dbo].[ListSurveyRespondentMetadata] (
    [ListCompanyMetadataId] INT            NOT NULL,
    [ListCompanyResponseId] INT            NOT NULL,
    [MetadataKey]           VARCHAR (500)  NOT NULL,
    [MetadataValue]         VARCHAR (5000) NULL
);

