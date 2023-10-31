CREATE TABLE [dbo].[TemplateType] (
    [TemplateTypeId]          INT            IDENTITY (1, 1) NOT NULL,
    [TemplateTypeName]        VARCHAR (500)  NULL,
    [TemplateTypeDescription] VARCHAR (5000) NULL,
    [CreatedDateTime]         DATETIME       NOT NULL,
    [ModifiedDateTime]        DATETIME       NULL,
    [CreatedBy]               VARCHAR (100)  NULL,
    [ModifiedBy]              VARCHAR (100)  NULL,
    CONSTRAINT [PK_TemplateType] PRIMARY KEY CLUSTERED ([TemplateTypeId] ASC)
);

