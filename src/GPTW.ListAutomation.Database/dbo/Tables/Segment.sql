CREATE TABLE [dbo].[Segment] (
    [SegmentId] INT             IDENTITY (1, 1) NOT NULL,
    [SegmentName]               VARCHAR (200) NULL,
    CONSTRAINT [PK_Segment] PRIMARY KEY CLUSTERED ([SegmentId] ASC)
);


GO