CREATE TABLE [dbo].[Statement] (
    [StatementId]  INT           NOT NULL,
    [Statement]    VARCHAR (MAX) NOT NULL,
    [StmtCoreId]   INT           NOT NULL,
    [CreatedDate]  DATETIME      NOT NULL,
    [ModifiedDate] DATETIME      NULL,
    [ModifiedBy]   VARCHAR (500) NULL,
    [CreatedBy]    VARCHAR (500) NULL
);

