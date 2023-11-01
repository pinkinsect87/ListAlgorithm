
ALTER TABLE [dbo].[ListAlgorithmTemplate] WITH CHECK CHECK CONSTRAINT [FK_Template_TemplateType]
ALTER TABLE [dbo].[Countries] WITH CHECK CHECK CONSTRAINT [FK_Countries_Affiliates]
ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_Affiliates]
ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_AlgorithmProcessingStatus]
ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_UploadStatus]
ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_Template]
ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_SegmentId]
