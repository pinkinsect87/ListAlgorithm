CREATE TABLE [dbo].[Affiliates] (
    [AffiliateName] VARCHAR (5000) NOT NULL,
    [AffiliateId]   VARCHAR (10)   NOT NULL,
    CONSTRAINT [PK_Affiliates] PRIMARY KEY CLUSTERED ([AffiliateId] ASC)
);

