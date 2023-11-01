
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_Template]

IF (EXISTS(SELECT * FROM [dbo].[ListAlgorithmTemplate]))  
BEGIN  
	DELETE FROM [dbo].[ListAlgorithmTemplate]
END

SET IDENTITY_INSERT [dbo].[ListAlgorithmTemplate] ON 

INSERT [dbo].[ListAlgorithmTemplate] ([TemplateId], [TemplateName], [TemplateTypeId], [TemplateVersion], [ManifestFileInfo], [ManifestFileXml]) VALUES (1, N'BLSIndustryGenderVer2018', 1, N'1.0', N'', N'BLS_data.json')
INSERT [dbo].[ListAlgorithmTemplate] ([TemplateId], [TemplateName], [TemplateTypeId], [TemplateVersion], [ManifestFileInfo], [ManifestFileXml]) VALUES (2, N'Top100List', 1, N'1.0', N'', N'list_manifest.json')
INSERT [dbo].[ListAlgorithmTemplate] ([TemplateId], [TemplateName], [TemplateTypeId], [TemplateVersion], [ManifestFileInfo], [ManifestFileXml]) VALUES (3, N'US_100SupplementList', 1, N'1.0', N'', N'US_supplement.json')

SET IDENTITY_INSERT [dbo].[ListAlgorithmTemplate] OFF

ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_Template]

GO
