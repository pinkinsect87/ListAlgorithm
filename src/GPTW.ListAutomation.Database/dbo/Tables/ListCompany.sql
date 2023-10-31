CREATE TABLE [dbo].[ListCompany] (
    [ListCompanyId]         INT           IDENTITY (1, 1) NOT NULL,
    [ClientId]              INT           NULL,
    [ClientName]            VARCHAR (MAX) NULL,
    [EngagementId]          INT           NULL,
    [SurveyVersionId]       VARCHAR (50)  NULL,
    [CertificationDateTime] DATETIME      NULL,
    [IsCertified]           BIT           NULL,
    [IsDisqualified]        BIT           NULL,
    [ListSourceFileId]      INT           NOT NULL,
    [ListRequestId]         INT           NOT NULL,
    [SurveyDateTime]        DATETIME      NULL,
    CONSTRAINT [PK_ListCompany_1] PRIMARY KEY CLUSTERED ([ListCompanyId] ASC),
    CONSTRAINT [FK_ListCompany_ListRequest] FOREIGN KEY ([ListRequestId]) REFERENCES [dbo].[ListRequest] ([ListRequestId]),
    CONSTRAINT [FK_ListCompany_ListSourceFile] FOREIGN KEY ([ListSourceFileId]) REFERENCES [dbo].[ListSourceFile] ([ListSourceFileId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListCompany_ListRequestId]
    ON [dbo].[ListCompany]([ListRequestId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListCompany_ListSourceFileId]
    ON [dbo].[ListCompany]([ListSourceFileId] ASC);


GO
CREATE NONCLUSTERED INDEX [ndx_ListCompany_eid_cid_svid]
    ON [dbo].[ListCompany]([EngagementId] ASC, [ClientId] ASC, [SurveyVersionId] ASC);

