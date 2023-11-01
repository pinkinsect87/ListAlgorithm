
ALTER TABLE [dbo].[ListAlgorithmTemplate] NOCHECK CONSTRAINT [FK_Template_TemplateType]

IF (EXISTS(SELECT * FROM [dbo].[TemplateType]))  
BEGIN  
	DELETE FROM [dbo].[TemplateType]
END

SET IDENTITY_INSERT [dbo].[TemplateType] ON 

INSERT [dbo].[TemplateType] ([TemplateTypeId], [TemplateTypeName], [TemplateTypeDescription], [CreatedDateTime], [ModifiedDateTime], [CreatedBy], [ModifiedBy]) 
	VALUES (1, N'Main Template', N'', CAST(N'2023-01-01' AS DateTime), null, null, null)

SET IDENTITY_INSERT [dbo].[TemplateType] OFF

ALTER TABLE [dbo].[ListAlgorithmTemplate] WITH CHECK CHECK CONSTRAINT [FK_Template_TemplateType]

GO
