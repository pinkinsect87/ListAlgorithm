
ALTER TABLE [dbo].[ListRequest] NOCHECK CONSTRAINT [FK_ListRequest_SegmentId]

IF (EXISTS(SELECT * FROM [dbo].[Segment]))  
BEGIN  
	DELETE FROM [dbo].[Segment]
END

SET IDENTITY_INSERT [dbo].[Segment] ON 

INSERT [dbo].[Segment] ([SegmentId], [SegmentName]) VALUES (1, N'None')
INSERT [dbo].[Segment] ([SegmentId], [SegmentName]) VALUES (2, N'Small')
INSERT [dbo].[Segment] ([SegmentId], [SegmentName]) VALUES (3, N'Small & Medium (SMB)')
INSERT [dbo].[Segment] ([SegmentId], [SegmentName]) VALUES (4, N'Medium')
INSERT [dbo].[Segment] ([SegmentId], [SegmentName]) VALUES (5, N'Large')
INSERT [dbo].[Segment] ([SegmentId], [SegmentName]) VALUES (6, N'MNC')

SET IDENTITY_INSERT [dbo].[Segment] OFF

ALTER TABLE [dbo].[ListRequest] WITH CHECK CHECK CONSTRAINT [FK_ListRequest_SegmentId]

GO
