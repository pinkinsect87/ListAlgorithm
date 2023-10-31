
CREATE PROCEDURE [dbo].[sp_ProcessORGData] 
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
			GROUP BY [engagement_id], [client_id], [survey_ver_id] 
		)

		MERGE INTO [dbo].[ListCompany] AS Target
		USING ListCompanyData AS Source
		ON Target.[EngagementId] = Source.[engagement_id] AND Target.[ClientId] = Source.[client_id] AND Target.[SurveyVersionId] = Source.[survey_ver_id] 
		WHEN MATCHED THEN
			UPDATE SET Target.[SurveyDateTime] = @CreateDateTime
		WHEN NOT MATCHED by Target THEN
			INSERT([ClientId],[ClientName],[EngagementId],[SurveyVersionId],[CertificationDateTime],[IsCertified],[IsDisqualified],[ListSourceFileId],[ListRequestId],[SurveyDateTime])
			VALUES(Source.client_id, null, Source.[engagement_id], Source.[survey_ver_id], null, null, null, @ListSourceFileId, @ListRequestId, @CreateDateTime);
		
		DECLARE @PageSize INT
		DECLARE @From INT
		DECLARE @To INT
		
		SET @PageSize = 1000000
		SET @From = 1
		SET @To = @PageSize

		WHILE EXISTS(SELECT 1 FROM [dbo].[TempORG] WHERE [Id] > @From)
		BEGIN
			-- merge ListSurveyRespondent
			WITH ListSurveyRespondentData AS (
				SELECT LC.[ListCompanyId], SD.[respondent_key], SD.[statement_id], SD.[response] 
				FROM [dbo].[TempORG] AS SD
				INNER JOIN [dbo].[ListCompany] AS LC 
				ON SD.[engagement_id] = LC.[EngagementId] AND SD.[client_id] = LC.[ClientId] AND SD.[survey_ver_id] = LC.[SurveyVersionId] 
				WHERE SD.[Id] BETWEEN @From AND @To
			)
			
			MERGE INTO [dbo].[ListSurveyRespondent] AS Target
			USING ListSurveyRespondentData AS Source
			ON Target.[RespondentKey] = Source.[respondent_key] AND Target.[ListCompanyId] = Source.[ListCompanyId] AND Target.[StatementId] = Source.[statement_id] 
			WHEN MATCHED THEN
				UPDATE SET Target.[Response] = Source.[response]
			WHEN NOT MATCHED by Target THEN
				INSERT([ListCompanyId], [RespondentKey], [StatementId], [Response])
				VALUES(Source.[ListCompanyId], Source.[respondent_key], Source.[statement_id], Source.[response]);

			SET @From = @From + @PageSize
			SET @To = @To + @PageSize

		END

	END

END
