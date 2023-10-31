
CREATE PROCEDURE [dbo].[sp_ProcessSurveyData] 
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
			SELECT [engagement_id], [client_id], [survey_ver_id]
			FROM [dbo].[TempORG] 
			WHERE [engagement_id] IS NOT NULL AND [client_id] IS NOT NULL AND [survey_ver_id] IS NOT NULL AND [respondent_key] IS NOT NULL
			GROUP BY [engagement_id], [client_id], [survey_ver_id] 
		)

		MERGE INTO [dbo].[ListCompany] AS Target
		USING ListCompanyData AS Source
		ON Target.[ClientId] = Source.[client_id] AND Target.[EngagementId] = Source.[engagement_id] AND Target.[SurveyVersionId] = Source.[survey_ver_id] 
		WHEN MATCHED THEN
			UPDATE SET Target.[SurveyDateTime] = @CreateDateTime
		WHEN NOT MATCHED by Target THEN
			INSERT([ClientId],[ClientName],[EngagementId],[SurveyVersionId],[CertificationDateTime],[IsCertified],[IsDisqualified],[ListSourceFileId],[ListRequestId],[SurveyDateTime])
			VALUES(Source.client_id, null, Source.[engagement_id], Source.[survey_ver_id], null, null, null, @ListSourceFileId, @ListRequestId, @CreateDateTime);
		
		-- respondent columns
		DECLARE @ColumnNames AS NVARCHAR(MAX)
		DECLARE @SelectColumnNames AS NVARCHAR(MAX)		
		SELECT @ColumnNames = string_agg( CAST('[' + replace([name], ']', ']]') + ']'as nvarchar(MAX)), ',') WITHIN GROUP (ORDER BY column_id)
		FROM sys.columns WHERE [object_id] = OBJECT_ID('[dbo].[TempORG]')
		AND [name] NOT IN ('engagement_id', 'client_id', 'survey_ver_id', 'respondent_key');
		
		SELECT @SelectColumnNames = string_agg( CAST('ISNULL([' + replace([name], ']', ']]') + '], '''') AS ' + '[' + replace([name], ']', ']]') + ']' as nvarchar(MAX)), ',') WITHIN GROUP (ORDER BY column_id)
		FROM sys.columns WHERE [object_id] = OBJECT_ID('[dbo].[TempORG]')
		AND [name] NOT IN ('engagement_id', 'client_id', 'survey_ver_id', 'respondent_key');
		
		---- merge ListSurveyRespondent
		DECLARE @Query AS NVARCHAR(MAX)		
		SET @Query = '
			;WITH ListSurveyRespondentData AS (
				SELECT * FROM (
					SELECT LC.[ListCompanyId], SD.[respondent_key], ' + @SelectColumnNames + '
					FROM [dbo].[TempORG] AS SD
					INNER JOIN [dbo].[ListCompany] AS LC ON SD.[engagement_id] = LC.[EngagementId] AND SD.[client_id] = LC.[ClientId] AND SD.[survey_ver_id] = LC.[SurveyVersionId]
					WHERE SD.[engagement_id] IS NOT NULL AND SD.[client_id] IS NOT NULL AND SD.[survey_ver_id] IS NOT NULL AND SD.[respondent_key] IS NOT NULL
				) AS tmp
				UNPIVOT
				(
				   [column_value] FOR [column_name] IN (' + @ColumnNames + ')
				) AS unpvt
			)
			
			MERGE INTO [dbo].[ListSurveyRespondent] AS Target
			USING ListSurveyRespondentData AS Source
			ON Target.[RespondentKey] = Source.[respondent_key] AND Target.[ListCompanyId] = Source.[ListCompanyId] AND Target.[StatementId] = Source.[column_name] 
			WHEN MATCHED THEN
				UPDATE SET Target.[Response] = Source.[column_value]
			WHEN NOT MATCHED by Target THEN
				INSERT([ListCompanyId], [RespondentKey], [StatementId], [Response])
				VALUES(Source.[ListCompanyId], Source.[respondent_key], Source.[column_name], Source.[column_value]);';
		
		EXEC (@Query);

	END

END
