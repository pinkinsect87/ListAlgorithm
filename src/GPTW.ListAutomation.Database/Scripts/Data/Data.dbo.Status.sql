
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_AlgorithmProcessingStatus]
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_UploadStatus]

IF (EXISTS(SELECT * FROM [dbo].[Status]))  
BEGIN  
	DELETE FROM [dbo].[Status]
END

SET IDENTITY_INSERT [dbo].[Status] ON 

INSERT [dbo].[Status] ([StatusId], [StatusName], [StatusDescription]) VALUES (1, N'Success', N'')

SET IDENTITY_INSERT [dbo].[Status] OFF

ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_AlgorithmProcessingStatus]
ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_UploadStatus]

GO
