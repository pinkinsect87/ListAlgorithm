
CREATE PROCEDURE [dbo].[sp_ProcessDemographicsData] 
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
			FROM [dbo].[TempDemographics] 
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
		
		-- merge ListSurveyRespondentDemographics
		;WITH RespondentDemographicsData AS (
			SELECT LC.[ListCompanyId], SD.[respondent_key], SD.[Gender], SD.[Age], SD.[Country/Region], SD.[Job Level], SD.[LGBT/LGBTQ+], 
				SD.[Race/ Ethnicity], SD.[Responsibility], SD.[Tenure], SD.[Work Status], SD.[Work Type], SD.[Worker Type], SD.[Birth Year], 
				SD.[Confidence], SD.[Disabilities], SD.[Managerial Level], SD.[Meaningful Innovation Opportunities], SD.[Pay Type], SD.[Zip Code]
			FROM [dbo].[TempDemographics] AS SD
			INNER JOIN [dbo].[ListCompany] AS LC ON SD.[engagement_id] = LC.[EngagementId] AND SD.[client_id] = LC.[ClientId] AND SD.[survey_ver_id] = LC.[SurveyVersionId]
			WHERE SD.[engagement_id] IS NOT NULL AND SD.[client_id] IS NOT NULL AND SD.[survey_ver_id] IS NOT NULL AND SD.[respondent_key] IS NOT NULL
		)

		MERGE INTO [dbo].[ListSurveyRespondentDemographics] AS Target
		USING RespondentDemographicsData AS Source
		ON Target.[ListCompanyId] = Source.[ListCompanyId] AND Target.[RespondentKey] = Source.[respondent_key] 
		WHEN MATCHED THEN
			UPDATE SET Target.[Gender] = Source.[Gender],
				Target.[Age] = Source.[Age],
				Target.[CountryRegion] = Source.[Country/Region],
				Target.[JobLevel] = Source.[Job Level],
				Target.[LgbtOrLgbtQ] = Source.[LGBT/LGBTQ+],
				Target.[RaceEthniticity] = Source.[Race/ Ethnicity],
				Target.[Responsibility] = Source.[Responsibility],
				Target.[Tenure] = Source.[Tenure],
				Target.[WorkStatus] = Source.[Work Status],
				Target.[WorkType] = Source.[Work Type],
				Target.[WorkerType] = Source.[Worker Type],
				Target.[BirthYear] = Source.[Birth Year],
				Target.[Confidence] = Source.[Confidence],
				Target.[Disabilities] = Source.[Disabilities],
				Target.[ManagerialLevel] = Source.[Managerial Level],
				Target.[MeaningfulInnovationOpportunities] = Source.[Meaningful Innovation Opportunities],
				Target.[PayType] = Source.[Pay Type],
				Target.[Zipcode] = Source.[Zip Code],
				Target.[ModifiedDateTime] = @CreateDateTime
		WHEN NOT MATCHED by Target THEN
			INSERT([ListCompanyId],[RespondentKey],[Gender],[Age],[CountryRegion],[JobLevel],[LgbtOrLgbtQ],[RaceEthniticity],
				[Responsibility],[Tenure],[WorkStatus],[WorkType],[WorkerType],[BirthYear],[Confidence],[Disabilities],[PayType],
				[ManagerialLevel],[MeaningfulInnovationOpportunities],[Zipcode],[CreatedDateTime],[ModifiedDateTime])
			VALUES(Source.[ListCompanyId], Source.[respondent_key], Source.[Gender], Source.[Age], Source.[Country/Region], Source.[Job Level], 
				Source.[LGBT/LGBTQ+], Source.[Race/ Ethnicity], Source.[Responsibility], Source.[Tenure], Source.[Work Status], Source.[Work Type], 
				Source.[Worker Type], Source.[Birth Year], Source.[Confidence], Source.[Disabilities], Source.[Pay Type], Source.[Managerial Level], 
				Source.[Meaningful Innovation Opportunities], Source.[Zip Code], @CreateDateTime, @CreateDateTime); 

	END

END
