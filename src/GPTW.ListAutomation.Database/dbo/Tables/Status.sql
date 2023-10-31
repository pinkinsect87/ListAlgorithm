CREATE TABLE [dbo].[Status] (
    [StatusId]          INT           IDENTITY (1, 1) NOT NULL,
    [StatusName]        VARCHAR (50)  NOT NULL,
    [StatusDescription] VARCHAR (250) NULL,
    CONSTRAINT [PK_Status] PRIMARY KEY CLUSTERED ([StatusId] ASC)
);

