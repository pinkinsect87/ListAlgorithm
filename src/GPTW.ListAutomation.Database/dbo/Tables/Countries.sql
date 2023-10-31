CREATE TABLE [dbo].[Countries] (
    [CountryCode] VARCHAR (50)  NOT NULL,
    [CountryName] VARCHAR (500) NOT NULL,
    [AffiliateId] VARCHAR (10)  NOT NULL,
    CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED ([CountryCode] ASC),
    CONSTRAINT [FK_Countries_Affiliates] FOREIGN KEY ([AffiliateId]) REFERENCES [dbo].[Affiliates] ([AffiliateId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_Countries_AffiliateId]
    ON [dbo].[Countries]([AffiliateId] ASC);

