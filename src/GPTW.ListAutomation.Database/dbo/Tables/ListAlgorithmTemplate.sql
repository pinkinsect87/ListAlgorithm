CREATE TABLE [dbo].[ListAlgorithmTemplate] (
    [TemplateId]       INT           IDENTITY (1, 1) NOT NULL,
    [TemplateName]     VARCHAR (200) NULL,
    [TemplateTypeId]   INT           NOT NULL,
    [TemplateVersion]  VARCHAR (10)  NOT NULL,
    [ManifestFileInfo] VARCHAR (500) NULL,
    [ManifestFileXml]  XML           NULL,
    CONSTRAINT [PK_Template] PRIMARY KEY CLUSTERED ([TemplateId] ASC),
    CONSTRAINT [FK_Template_TemplateType] FOREIGN KEY ([TemplateTypeId]) REFERENCES [dbo].[TemplateType] ([TemplateTypeId])
);


GO
CREATE NONCLUSTERED INDEX [IX_FK_ListAlgorithm_TemplateTypeId]
    ON [dbo].[ListAlgorithmTemplate]([TemplateTypeId] ASC);

