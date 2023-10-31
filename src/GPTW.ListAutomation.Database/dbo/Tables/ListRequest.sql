CREATE TABLE [dbo].[ListRequest] (
    [ListRequestId]             INT            IDENTITY (1, 1) NOT NULL,
    [CountryCode]               VARCHAR (10)   NOT NULL,
    [TemplateId]                INT            NOT NULL,
    [PublicationYear]           INT            NOT NULL,
    [CreateDateTime]            DATETIME       NOT NULL,
    [ModifiedDateTime]          DATETIME       NOT NULL,
    [CreatedBy]                 VARCHAR (100)  NOT NULL,
    [ModifiedBy]                VARCHAR (100)  NOT NULL,
    [UploadStatusId]            INT            NOT NULL,
    [AlgorithProcessedStatusId] INT            NOT NULL,
    [ListName]                  VARCHAR (5000) NULL,
    [ListNameLocalLanguage]     VARCHAR (5000) NULL,
    [ListTypeId]                INT            NULL,
    [AffiliateId]               VARCHAR (10)   NULL,
    CONSTRAINT [PK_ListRequest] PRIMARY KEY CLUSTERED ([ListRequestId] ASC),
    CONSTRAINT [FK_ListRequest_Affiliates] FOREIGN KEY ([AffiliateId]) REFERENCES [dbo].[Affiliates] ([AffiliateId]),
    CONSTRAINT [FK_ListRequest_AlgorithmProcessingStatus] FOREIGN KEY ([AlgorithProcessedStatusId]) REFERENCES [dbo].[Status] ([StatusId]),
    CONSTRAINT [FK_ListRequest_Template] FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[ListAlgorithmTemplate] ([TemplateId]),
    CONSTRAINT [FK_ListRequest_UploadStatus] FOREIGN KEY ([UploadStatusId]) REFERENCES [dbo].[Status] ([StatusId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListRequest_AffiliateId]
    ON [dbo].[ListRequest]([AffiliateId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListRequest_AlgorithProcessedStatusId]
    ON [dbo].[ListRequest]([AlgorithProcessedStatusId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListRequest_TemplateId]
    ON [dbo].[ListRequest]([TemplateId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListRequest_UploadStatusId]
    ON [dbo].[ListRequest]([UploadStatusId] ASC);

