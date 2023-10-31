
CREATE PROCEDURE [dbo].[sp_GetListAutomationResultByListRequestId] 
	-- Add the parameters for the stored procedure here
	@ListRequestId varchar(10) 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @ColumnNames AS NVARCHAR(MAX)
	SELECT @ColumnNames = ISNULL(@ColumnNames + ',', '') + QUOTENAME([ResultKey]) FROM [dbo].[ListAutomationResult] GROUP BY [ResultKey]

	--SELECT @ColumnNames

	DECLARE @Query AS NVARCHAR(MAX)
	SET @Query = '
	SELECT * FROM (
		SELECT * FROM (
		  SELECT LC.[ListCompanyId], LAR.[ResultKey], LAR.[ResultValue] 
		  FROM [dbo].[ListAutomationResult] AS LAR with(nolock)
		  INNER JOIN [dbo].[ListCompany] AS LC with(nolock) ON LAR.[ListCompanyId] = LC.ListCompanyId
		  INNER JOIN [dbo].[ListRequest] AS LR with(nolock) ON LC.[ListRequestId] = LR.[ListRequestId] 
		  WHERE LR.[ListRequestId] = ' + @ListRequestId + '
		) AS tmp
		PIVOT(
		   MAX([ResultValue]) 
		   FOR [ResultKey] IN (' + @ColumnNames + ')
		) AS T
	) PVT ORDER BY [company_name] ASC'

	EXEC (@Query);

END
