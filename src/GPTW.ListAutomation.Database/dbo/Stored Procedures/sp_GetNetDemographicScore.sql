
CREATE PROCEDURE [sp_GetNetDemographicScore]
	@Filter VARCHAR(1000) = '',
	@ClientId VARCHAR(10), 
	@EngagementId VARCHAR(10), 
	@ColumnName VARCHAR(100) = '',
	@ConfidenceAnswerOption VARCHAR(500) = '',
	@NonConfidenceAnswerOption VARCHAR(500) = ''
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    declare @dsql nvarchar(MAX) = '
	FROM (
		SELECT [ListCompanyResponseId], [ListCompanyId], [RespondentKey], [StatementId], [Response]
		FROM [dbo].[ListSurveyRespondent] WITH (NOLOCK)
		UNPIVOT
		(
			[Response] FOR [StatementId] IN (
				Response_1,
				Response_2,
				Response_3,
				Response_4,
				Response_5,
				Response_6,
				Response_7,
				Response_8,
				Response_9,
				Response_10,
				Response_11,
				Response_12,
				Response_13,
				Response_14,
				Response_15,
				Response_16,
				Response_17,
				Response_18,
				Response_19,
				Response_20,
				Response_21,
				Response_22,
				Response_23,
				Response_24,
				Response_25,
				Response_26,
				Response_27,
				Response_28,
				Response_29,
				Response_30,
				Response_31,
				Response_32,
				Response_33,
				Response_34,
				Response_35,
				Response_36,
				Response_37,
				Response_38,
				Response_39,
				Response_40,
				Response_41,
				Response_42,
				Response_43,
				Response_44,
				Response_45,
				Response_46,
				Response_47,
				Response_48,
				Response_49,
				Response_50,
				Response_51,
				Response_52,
				Response_53,
				Response_54,
				Response_55,
				Response_56,
				Response_57,
				Response_672,
				Response_12211,
				Response_12212,
				Response_12213,
				Response_12214,
				Response_12215)
		) AS unpvt
	) AS LS
	INNER JOIN [dbo].[ListCompany] AS LC with(nolock) ON LS.[ListCompanyId] = LC.[ListCompanyId]
	INNER JOIN [dbo].[ListSurveyRespondentDemographics] AS LSR with(nolock) 
		ON LS.[ListCompanyId] = LSR.[ListCompanyId] AND LS.[RespondentKey] = LSR.[RespondentKey]
	WHERE LC.[ClientId] = ' + @ClientId + ' 
	AND LC.[EngagementId] = ' + @EngagementId+ ' 
	AND LS.[Response] > 0' 
	
	IF @Filter <> '' BEGIN
		SET @dsql += '
		AND ' + @Filter
	END

	declare @sql nvarchar(MAX) = '
	;WITH T AS (
		SELECT COUNT(1) as count_total ' +
		@dsql + '
	),
	C AS (
		SELECT COUNT(1) as count_confidence ' +
		@dsql + '
		AND LSR.[' + @ColumnName + '] = ''' + @confidenceAnswerOption + '''
	),
	NC AS (
		SELECT COUNT(1) as count_nonConfidence ' +
		@dsql + ' 
		AND LSR.[' + @ColumnName + '] = ''' + @nonConfidenceAnswerOption + '''
	)

	SELECT CAST(CASE WHEN T.[count_total] > 0 THEN ((C.[count_confidence] - NC.[count_nonConfidence]) * 1.0) / T.[count_total] ELSE 0 END AS decimal(18,2)) AS NetDemographicScore
	FROM T, C, NC;'

	--PRINT @sql
	EXEC( @sql );

END
