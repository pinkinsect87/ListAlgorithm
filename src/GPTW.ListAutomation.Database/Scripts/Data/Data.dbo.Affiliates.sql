
ALTER TABLE [dbo].[Countries] NOCHECK CONSTRAINT [FK_Countries_Affiliates]
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_Affiliates]

IF (EXISTS(SELECT * FROM [dbo].[Affiliates]))  
BEGIN  
	DELETE FROM [dbo].[Affiliates]
END

SET IDENTITY_INSERT [dbo].[Affiliates] ON 

INSERT [dbo].[Affiliates] ([AffiliateName], [AffiliateId]) VALUES (N'US', 1)

SET IDENTITY_INSERT [dbo].[Affiliates] OFF

ALTER TABLE [dbo].[Countries] WITH CHECK CHECK CONSTRAINT [FK_Countries_Affiliates]
ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_Affiliates]

GO
