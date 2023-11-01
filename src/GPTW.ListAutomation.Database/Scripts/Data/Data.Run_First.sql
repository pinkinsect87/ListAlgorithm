
ALTER TABLE [dbo].[ListAlgorithmTemplate] NOCHECK CONSTRAINT [FK_Template_TemplateType]
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_Template]
ALTER TABLE [dbo].[Countries] NOCHECK CONSTRAINT [FK_Countries_Affiliates]
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_Affiliates]
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_AlgorithmProcessingStatus]
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_UploadStatus]
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_SegmentId]
