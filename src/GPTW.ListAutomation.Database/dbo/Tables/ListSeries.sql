CREATE TABLE [dbo].[ListSeries] (
    [ListTemplateId] INT            IDENTITY (1, 1) NOT NULL,
    [EnglishName]    VARCHAR (5000) NULL,
    [LocalName]      VARCHAR (5000) NULL,
    [ListRequestId]  INT            NOT NULL,
    [CountryCode]    VARCHAR (50)   NOT NULL,
    CONSTRAINT [PK_ListTemplate] PRIMARY KEY CLUSTERED ([ListTemplateId] ASC),
    CONSTRAINT [FK_ListSeries_Countries] FOREIGN KEY ([CountryCode]) REFERENCES [dbo].[Countries] ([CountryCode]),
    CONSTRAINT [FK_ListTemplate_ListTemplate] FOREIGN KEY ([ListRequestId]) REFERENCES [dbo].[ListRequest] ([ListRequestId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListSeries_CountryCode]
    ON [dbo].[ListSeries]([CountryCode] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListSeries_ListRequestId]
    ON [dbo].[ListSeries]([ListRequestId] ASC);

