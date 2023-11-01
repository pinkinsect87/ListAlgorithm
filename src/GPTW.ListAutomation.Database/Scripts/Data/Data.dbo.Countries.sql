
IF (EXISTS(SELECT * FROM [dbo].[Countries]))  
BEGIN  
	DELETE FROM [dbo].[Countries]
END

INSERT [dbo].[Countries] ([CountryCode], [CountryName], [AffiliateId]) VALUES (N'US', N'US', 1)

GO
