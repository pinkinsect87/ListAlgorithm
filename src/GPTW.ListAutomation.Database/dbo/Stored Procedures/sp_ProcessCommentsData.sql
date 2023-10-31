
CREATE PROCEDURE [dbo].[sp_ProcessCommentsData] 
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
			FROM [dbo].[TempComments] 
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
		
		-- merge ListSurveyRespondentComments
		;WITH RespondentCommentsData AS (
			SELECT LC.[ListCompanyId], SD.[respondent_key], SD.[question], SD.[response]
			FROM [dbo].[TempComments] AS SD
			INNER JOIN [dbo].[ListCompany] AS LC ON SD.[engagement_id] = LC.[EngagementId] AND SD.[client_id] = LC.[ClientId] AND SD.[survey_ver_id] = LC.[SurveyVersionId]
			WHERE SD.[engagement_id] IS NOT NULL AND SD.[client_id] IS NOT NULL AND SD.[survey_ver_id] IS NOT NULL AND SD.[respondent_key] IS NOT NULL
		)

		MERGE INTO [dbo].[ListSurveyRespondentComments] AS Target
		USING RespondentCommentsData AS Source
		ON Target.[ListCompanyId] = Source.[ListCompanyId] 
			AND Target.[RespondentKey] = Source.[respondent_key] 
			AND Target.[Question] = Source.[question] 
		WHEN MATCHED THEN
			UPDATE SET Target.[Response] = Source.[response]
		WHEN NOT MATCHED by Target THEN
			INSERT([ListCompanyId],[RespondentKey],[Question],[Response])
			VALUES(Source.[ListCompanyId], Source.[respondent_key], Source.[question], Source.[response]);
		
	END

END
