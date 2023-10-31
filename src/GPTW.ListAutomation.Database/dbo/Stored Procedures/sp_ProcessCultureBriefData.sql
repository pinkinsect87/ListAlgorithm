
CREATE PROCEDURE [dbo].[sp_ProcessCultureBriefData] 
	-- Add the parameters for the stored procedure here
	@ListSourceFileId INT 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @ListRequestId INT
	SELECT @ListRequestId = [ListRequestId] FROM [dbo].[ListSourceFile] WHERE [ListSourceFileId] = @ListSourceFileId

	IF @ListRequestId IS NOT NULL BEGIN

		DECLARE @CreateDateTime DATETIME
		SET @CreateDateTime = GETDATE()

		-- merge ListCompany
		;WITH ListCompanyData AS (
			SELECT [ClientId], [ECRClientName], [EngagementId], [SurveyVersionId]
			FROM [dbo].[TempCultureBrief] 
			WHERE [ClientId] IS NOT NULL AND [EngagementId] IS NOT NULL AND [SurveyVersionId] IS NOT NULL
			GROUP BY [ClientId], [ECRClientName], [EngagementId], [SurveyVersionId]
		)

		MERGE INTO [dbo].[ListCompany] AS Target
		USING ListCompanyData AS Source
		ON Target.[ClientId] = Source.[ClientId] AND Target.[EngagementId] = Source.[EngagementId] AND Target.[SurveyVersionId] = Source.[SurveyVersionId] 
		WHEN MATCHED THEN
			UPDATE SET Target.[ClientName] = Source.[ECRClientName], Target.[SurveyDateTime] = @CreateDateTime
		WHEN NOT MATCHED by Target THEN
			INSERT([ClientId],[ClientName],[EngagementId],[SurveyVersionId],[CertificationDateTime],[IsCertified],[IsDisqualified],[ListSourceFileId],[ListRequestId],[SurveyDateTime])
			VALUES(Source.[ClientId], Source.[ECRClientName], Source.[EngagementId], Source.[SurveyVersionId], null, null, null, @ListSourceFileId, @ListRequestId, @CreateDateTime);

		-- respondent columns
		DECLARE @ColumnNames AS NVARCHAR(MAX)
		DECLARE @SelectColumnNames AS NVARCHAR(MAX)		
		SELECT @ColumnNames = string_agg( CAST('[' + replace([name], ']', ']]') + ']'as nvarchar(MAX)), ',') WITHIN GROUP (ORDER BY column_id)
		FROM sys.columns WHERE [object_id] = OBJECT_ID('[dbo].[TempCultureBrief]')
		AND ([name] like ('ca_%') or [name] like ('ca1_%') or [name] like ('ca2_%') or [name] like ('cb_%') or [name] like ('gr_%') or [name] like ('sr_%') )
		
		SELECT @SelectColumnNames = string_agg( CAST('ISNULL([' + replace([name], ']', ']]') + '], '''') AS ' + '[' + replace([name], ']', ']]') + ']' as nvarchar(MAX)), ',') WITHIN GROUP (ORDER BY column_id)
		FROM sys.columns WHERE [object_id] = OBJECT_ID('[dbo].[TempCultureBrief]')
		AND ([name] like ('ca_%') or [name] like ('ca1_%') or [name] like ('ca2_%') or [name] like ('cb_%') or [name] like ('gr_%') or [name] like ('sr_%') )
		
		-- merge ListCompanyCultureBrief
		DECLARE @Query AS NVARCHAR(MAX)
		SET @Query = '
			;WITH ListCompanyCultureBriefData AS (
				SELECT * FROM (
					SELECT LC.[ListCompanyId], ' + @SelectColumnNames + '
					FROM [dbo].[TempCultureBrief] AS SD
					INNER JOIN [dbo].[ListCompany] AS LC ON SD.[EngagementId] = LC.[EngagementId] AND SD.[ClientId] = LC.[ClientId] AND SD.[SurveyVersionId] = LC.[SurveyVersionId]
					WHERE SD.[ClientId] IS NOT NULL AND SD.[EngagementId] IS NOT NULL AND SD.[SurveyVersionId] IS NOT NULL
				) AS tmp
				UNPIVOT
				(
				   [column_value] FOR [column_name] IN (' + @ColumnNames + ')
				) AS unpvt
			)

			MERGE INTO [dbo].[ListCompanyCultureBrief] AS Target
			USING ListCompanyCultureBriefData AS Source
			ON Target.[ListCompanyId] = Source.[ListCompanyId] AND Target.[VariableName] = Source.[column_name] 
			WHEN MATCHED THEN
				UPDATE SET Target.[VariableValue] = Source.[column_value]
			WHEN NOT MATCHED by Target THEN
				INSERT([ListCompanyId], [VariableName], [VariableValue])
				VALUES(Source.[ListCompanyId], Source.[column_name], Source.[column_value]);';
	
		EXEC (@Query);
		--print (@Query)

	END

END
