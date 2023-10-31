
CREATE PROCEDURE [sp_GetListCompanyProduceRank]
	@ListCompanyId INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	;WITH SurveyData AS (
		SELECT [ListCompanyResponseId], [ListCompanyId], [StatementId], [Response]
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
	),
	T AS (
		SELECT [ListCompanyId], COUNT(ListCompanyId) AS count_total 
		FROM SurveyData
		WHERE [Response] > 0
		GROUP BY [ListCompanyId]
	),
	C45 AS (
		SELECT [ListCompanyId], COUNT(1) AS count_45 
		FROM SurveyData 
		WHERE [Response] IN (4, 5) 
		GROUP BY [ListCompanyId]
	)

	SELECT [ProduceRank] FROM (
		SELECT ListCompanyId, ROW_NUMBER() OVER(ORDER BY TrustIndexScore DESC) AS ProduceRank 
		FROM (
			SELECT T.[ListCompanyId], CAST(CASE WHEN T.[count_total] > 0 THEN C45.[count_45] * 1.0 / T.[count_total] ELSE 0 END AS decimal(18,2)) AS TrustIndexScore
			FROM T 
			INNER JOIN C45 ON T.[ListCompanyId] = C45.[ListCompanyId]
		) tmp
	) tt WHERE [ListCompanyId] = @ListCompanyId

    
END
