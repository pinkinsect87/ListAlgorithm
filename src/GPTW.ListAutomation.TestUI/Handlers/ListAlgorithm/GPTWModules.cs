using GPTW.ListAutomation.Core.Services.Data;
using GPTW.ListAutomation.TestUI.Infrastructure;

namespace GPTW.ListAutomation.TestUI.Handlers;

public enum QuestionType { TIStatement = 1, TIDemographic = 2 }

public class GPTWModules
{
    private readonly IListSurveyRespondentDemographicService _listSurveyRespondentDemographicService;
    private readonly IListSurveyRespondentService _listSurveyRespondentService;

    public GPTWModules(
        IListSurveyRespondentDemographicService listSurveyRespondentDemographicService,
        IListSurveyRespondentService listSurveyRespondentService)
    {
        _listSurveyRespondentDemographicService = listSurveyRespondentDemographicService;
        _listSurveyRespondentService = listSurveyRespondentService;
    }

    public async Task<double> GetDemographicNetPenalty(
        string respondentFilter,
        string demogHeader,
        string confidenceAnswerOption,
        string nonConfidenceAnswerOption,
        string demogHeaderToSkip,
        Dictionary<string, string> demogHeaderAnswerOptionToSkip,
        Dictionary<string, double> demogWeightingList,
        int cid,
        int eid)
    {
        // need to consider the case where we ignore NAs and a demog like Full Time / Part Time doesn't have any responses
        // and the weights need to change proportionally

        // intention is for penalty weights to be a total of 3
        // if some demographic penalties don't apply we will scale the other penalty weights to be out of 3

        // could call with standard demographics in demogweightinglist:
        // Gender // 1
        // Mgmt // 1
        // Age // 0.33
        // Work Status // 0.33
        // Tenure // 0.33

        // or could call with any list of demographics / weights (special USA)
        // Race/Eth // 1
        // Pay type // 0.33
        // LGBT // 0.33
        // Disability // 0.33

        //demogHeaderAnswerOptionToSkip // {'Gender': 'Other, 'LGBT': 'Prefer not Answer', 'Disability': 'Prefer not Answer'}

        double totalPenalty = 0;
        foreach (var localDemog in demogWeightingList)
        {
            //object localDemogHeader = localDemog.Key;
            var demogWeight = localDemog.Value;
            if (demogHeaderAnswerOptionToSkip.ContainsKey(localDemog.Key))
            {
                // recode As No Response 
            }
            var localConsistency = await GetDemographicCategoryConsistencyNet(demogHeader, confidenceAnswerOption, nonConfidenceAnswerOption, localDemog.Key, cid, eid);
            totalPenalty += (localConsistency * demogWeight);
        }
        return totalPenalty;
    }

    public async Task<double> GetNetDemographicScore(
        string respondentFilter,
        string demogHeader,
        string confidenceAnswerOption,
        string nonConfidenceAnswerOption,
        int cid,
        int eid)
    {
        // take into account overall respondent filter
        // ( Count(Confidence) - Count(NonConfidence) ) / Count (actual responses)

        // respondentFilter = ALL or Blacks or Women or...
        // demogHeader = Confidence
        // confidence = "Great deal"
        // nonconfidence = "Very little or none"

        // get demogHeader = "Confidence"
        //var greatDealConfidence = get count ListSurveyRespondentDemographics.Confidence == "Great deal"
        //var nonConfidence = get count ListSurveyRespondentDemographics.Confidence == "Very little or none"

        //var returnValue = (greatDealConfidence - nonConfidence)/(count of ListSurveyRespondent for ClientId)

        var matchedColumnName = GetMatchedDemographicsColumnName(demogHeader);

        respondentFilter = respondentFilter.ToUpper() == "ALL_RESPONDENTS" ? "" : respondentFilter;

        return await _listSurveyRespondentDemographicService.GetNetDemographicScore(
                            respondentFilter,
                            matchedColumnName,
                            confidenceAnswerOption,
                            nonConfidenceAnswerOption,
                            cid,
                            eid);
    }

    public async Task<double> GetDemographicTIPenalty(
        List<int> stmts,
        string respondentFilter,
        string demogHeaderToSkip,
        Dictionary<string, string> demogHeaderAnswerOptionToSkip,
        Dictionary<string, double> demogWeightingList,
        int cid,
        int eid)
    {
        // need to consider the case where we ignore NAs and a demog like Full Time / Part Time doesn't have any responses
        // and the weights need to change proportionally

        // intention is for penalty weights to be a total of 3
        // if some demographic penalties don't apply we will scale the other penalty weights to be out of 3

        // could call with standard demographics in demogweightinglist:
        // Gender // 1
        // Mgmt // 1
        // Age // 0.33
        // Work Status // 0.33
        // Tenure // 0.33

        // or could call with any list of demographics / weights (special USA)
        // Race/Eth // 1
        // Pay type // 0.33
        // LGBT // 0.33
        // Disability // 0.33

        //demogHeaderAnswerOptionToSkip // {'Gender': 'Other, 'LGBT': 'Prefer not Answer', 'Disability': 'Prefer not Answer'}

        double totalPenalty = 0;
        foreach (var localDemog in demogWeightingList)
        {
            var demogHeader = localDemog.Key;
            var demogWeight = localDemog.Value;
            if (demogHeaderAnswerOptionToSkip.ContainsKey(localDemog.Key))
            {
                // recode As No Response 
            }
            var localConsistency = await GetDemographicCategoryConsistency(stmts, demogHeader, cid, eid);
            totalPenalty += (localConsistency * demogWeight);
        }
        return totalPenalty;
    }

    private async Task<double> GetDemographicCategoryConsistencyNet(
        string demogHeaderForNetHighLow,
        string highDemog,
        string lowDemog,
        string demogHeader,
        int cid,
        int eid)
    {
        var localAnsOptions = await GetExistingAnswerOptionsForCompany(demogHeader, cid, eid);

        double aggregateCount = 0;
        int numCounted = 0;
        foreach (var localOption in localAnsOptions)
        {
            var localMoeGap = await GetMoeAdjustedGapNet("[Age] = 'Under 25'", "[Age] != 'Under 25' AND [Age] != 'No Response'", "Confidence", "A lot", "not many", 5, cid, eid);

            aggregateCount += localMoeGap;
            numCounted += 1;
        }
        double avgConsistency = numCounted == 0 ? 0 : aggregateCount / numCounted;
        return avgConsistency;
    }

    private async Task<double> GetMoeAdjustedGapNet(
        string groupOneFilter,
        string groupTwoFilter,
        string demogHeaderNet,
        string demogAnsOptionHigh,
        string demogAnswerOptionLow,
        int minRespondentsInGroup,
        int cid,
        int eid)
    {
        // ti scores and respondent counts

        var matchedColumnName = GetMatchedDemographicsColumnName(demogHeaderNet);

        double groupOneNetScore = await GetNetDemographicScore(groupOneFilter, demogHeaderNet, demogAnsOptionHigh, demogAnswerOptionLow, cid, eid);
        double groupTwoNetScore = await GetNetDemographicScore(groupTwoFilter, demogHeaderNet, demogAnsOptionHigh, demogAnswerOptionLow, cid, eid);
        double groupOneResponseCount = await GetActualNumberOfResponses(QuestionType.TIDemographic, demogHeaderNet, groupOneFilter + $" AND [{matchedColumnName}] != 'No Response'", cid, eid);
        double groupTwoResponseCount = await GetActualNumberOfResponses(QuestionType.TIDemographic, demogHeaderNet, groupTwoFilter + $" AND [{matchedColumnName}] != 'No Response'", cid, eid);

        List<int> syntheticResponses = new List<int>() { 1, 1, -1, -1 };

        // adjusted moe gap calculation - effectively adding in 2 positive & 2 negatives (or 4 neutrals)
        double n1_adj = groupOneResponseCount + 4;
        double p1_adj = groupOneNetScore * groupOneResponseCount / n1_adj;
        double n2_adj = groupTwoResponseCount + 4;
        double p2_adj = groupTwoNetScore * groupTwoResponseCount / n2_adj;

        double groupOneVariance = GetNetDemographicVariance(groupOneFilter, demogHeaderNet, demogAnsOptionHigh, demogAnswerOptionLow, cid, eid, syntheticResponses, p1_adj, n1_adj);
        double groupTwoVariance = GetNetDemographicVariance(groupOneFilter, demogHeaderNet, demogAnsOptionHigh, demogAnswerOptionLow, cid, eid, syntheticResponses, p2_adj, n2_adj);

        double adj_gap = Math.Abs(p1_adj - p2_adj);
        double rawMoe = 1.96 * Math.Sqrt((groupOneVariance / n1_adj) + (groupTwoVariance / n2_adj));
        double moeAdjGap = Math.Max(adj_gap - rawMoe, 0);

        // need at least 2 negative or 4 neutral (or 1 negative, 2 neutral) in the less happy group 
        if ((groupOneNetScore < groupTwoNetScore) & (groupOneResponseCount * (1 - groupOneNetScore) < 4))
        {
            moeAdjGap = 0;
        }
        else if ((groupOneNetScore > groupTwoNetScore) & (groupTwoResponseCount * (1 - groupTwoNetScore) < 4))
        {
            moeAdjGap = 0;
        }
        return moeAdjGap;
    }

    private double GetNetDemographicVariance(
        string respondentFilter,
        string demogHeader,
        string confidenceAnswerOption,
        string nonConfidenceAnswerOption,
        int cid,
        int eid,
        List<int> augmentedResponses,
        double adj_mean,
        double adj_count)
    {
        //adj_count();

        // take into account overall respondent filter

        // respondentFilter = ALL or Blacks or Women or...
        // demogHeader = Confidence
        // confidence = "Great deal"
        // nonconfidence = "Very little or none"
        // confidenceAnswerOption are counted as 1s
        // nonConfidenceAnswerOption are counted as -1s
        // other responses are counted as 0s
        // the augmented responses are added to stabilize the MoE for small sample sizes. This is typically 2 successes & 2 failures ({1, 1, -1, -1})
        // adj_mean is the average of all the responses after adjusting using the augmented responses [1, 1, -1, -1]
        // adj_count is the number of responses including the augmented responses, which should be the actual count + 4
        // calculate the variance (standard deviation squared) for the sample of all actual responses in the demo group and 4 augmented synthetic responses

        //variance = sum((x-adj_mean)^2) / (adj_count - 1) where x is each of the responses including the augmented responses

        ///'Example''''
        //the actual responses are ({0, -1, 1, 1, 0, 1}) and augmented responses are ({1, 1, -1, -1})
        //adj_count = 10 and adj_mean = .2
        //variance = ((0-0.2)^2+(-1-0.2)^2+(1-0.2)^2+(0-0.2)^2+^2+(1-0.2)^2+(1-0.2)^2+(1-0.2)^2+(-1-0.2)^2+(-1-0.2)^2) / (10-1)

        // TODO: How to calculate actual responses

        var a1 = Math.Pow(0 - adj_mean, 2);
        var a2 = Math.Pow(-1 - adj_mean, 2);
        var a3 = Math.Pow(1 - adj_mean, 2);
        var a4 = Math.Pow(1 - adj_mean, 2);
        var a5 = Math.Pow(0 - adj_mean, 2);
        var a6 = Math.Pow(1 - adj_mean, 2);

        var b1 = Math.Pow(1 - adj_mean, 2);
        var b2 = Math.Pow(1 - adj_mean, 2);
        var b3 = Math.Pow(-1 - adj_mean, 2);
        var b4 = Math.Pow(-1 - adj_mean, 2);

        var variance = (a1 + a2 + a3 + a4 + a5 + a6 + b1 + b2 + b3 + b4) / (adj_count - 1);
        return variance;
    }

    public async Task<double> GetDemographicCategoryConsistency(List<int> stmts, string demogHeader, int cid, int eid)
    {
        var localAnsOptions = await GetExistingAnswerOptionsForCompany(demogHeader, cid, eid);

        double aggregateCount = 0;
        int numCounted = 0;
        foreach (var localOption in localAnsOptions)
        {
            var localConsistency = await GetDemographicGroupConsistency(stmts, demogHeader, localOption, cid, eid);
            aggregateCount += localConsistency;
            numCounted += 1;
        }
        double avgConsistency = numCounted == 0 ? 0 : aggregateCount / numCounted;
        return avgConsistency;
    }

    private async Task<double> GetDemographicGroupConsistency(List<int> stmts, string demogHeader, string demogAnsOption, int cid, int eid)
    {
        // header = Age // AnsOption = 'Under 25'

        double aggregateCount = 0;
        int numCounted = 0;
        foreach (var localStatement in stmts)
        {
            var localMoeGap = await GetMoeAdjustedGapTrustIndex("[Age] = 'Under 25'", "[Age] != 'Under 25' AND [Age] != 'No Response'", localStatement, 5, cid, eid);
            aggregateCount += localMoeGap;
            numCounted += 1;
        }
        var avgConsistency = numCounted == 0 ? 0 : aggregateCount / numCounted;
        return avgConsistency;
    }

    private async Task<double> GetMoeAdjustedGapTrustIndex(
        string groupOneFilter,
        string groupTwoFilter,
        int stmtId,
        int minRespondentsInGroup,
        int cid,
        int eid)
    {
        // stmt to list
        List<int> whichStmtId = new List<int>();
        whichStmtId.Add(stmtId);

        // ti scores and respondent counts
        double groupOneTrustIndexScore = await GetTrustIndexScore(whichStmtId, groupOneFilter, cid, eid);
        double groupTwoTrustIndexScore = await GetTrustIndexScore(whichStmtId, groupTwoFilter, cid, eid);
        double groupOneResponseCount = await GetActualNumberOfResponses(QuestionType.TIStatement, stmtId.ToString(), groupOneFilter, cid, eid);
        double groupTwoResponseCount = await GetActualNumberOfResponses(QuestionType.TIStatement, stmtId.ToString(), groupTwoFilter, cid, eid);

        // adjusted moe gap calculation - effectively adding in 2 positive & 2 negatives (or 4 neutrals)
        double n1_adj = groupOneResponseCount + 4;
        double p1_adj = (groupOneTrustIndexScore * groupOneResponseCount + 2) / n1_adj;
        double n2_adj = groupTwoResponseCount + 4;
        double p2_adj = (groupTwoTrustIndexScore * groupTwoResponseCount + 2) / n2_adj;
        double adj_gap = Math.Abs(p1_adj - p2_adj);
        double var_1 = (p1_adj) * (1 - p1_adj);
        double var_2 = (p2_adj) * (1 - p2_adj);
        double rawMoe = 1.96 * Math.Sqrt(var_1 / n1_adj + var_2 / n2_adj);
        double moeAdjGap = Math.Max(adj_gap - rawMoe, 0);

        // zero if only one negative response in group with lower score
        if ((groupOneTrustIndexScore < groupTwoTrustIndexScore) && (groupOneResponseCount * (1 - groupOneTrustIndexScore) < 2))
        {
            moeAdjGap = 0;
        }
        else if ((groupOneTrustIndexScore > groupTwoTrustIndexScore) && (groupTwoResponseCount * (1 - groupTwoTrustIndexScore) < 2))
        {
            moeAdjGap = 0;
        }
        return moeAdjGap;
    }

    public async Task<double> GetTrustIndexScore(List<int> stmts, string respondentFilter, int cid, int eid)
    {
        // GetTrustIndexScore(43, "all_respondents", 2, 34)
        // GetTrustIndexScore([43,44,26,38], "[Gender] = 'Female'", 3, 49)

        respondentFilter = respondentFilter.ToUpper() == "ALL_RESPONDENTS" ? "" : respondentFilter;

        return await _listSurveyRespondentService.GetTrustIndexScore(respondentFilter, cid, eid, stmts.ToArray());
    }

    private async Task<double> GetActualNumberOfResponses(
        QuestionType questionType,
        string questionIdentifier,
        string respondentFilter,
        int cid,
        int eid)
    {
        // GetActualNumberOfResponses("TIStatement", "32") = 382
        // GetActualNumberOfResponses("TIDemographic", "Innovation Opportunitites") = 378 EXCLUDE NON-RESPONSES

        switch (questionType)
        {
            case QuestionType.TIStatement:
                // select count(*) from ListSurveyRespondent where StatementId = questionIdentifier
                int statementId = int.Parse(questionIdentifier);
                return await _listSurveyRespondentService.GetNumberOfRespondents(respondentFilter, cid, eid, statementId);
            case QuestionType.TIDemographic:
                var matchedColumnName = GetMatchedDemographicsColumnName(questionIdentifier);
                // select count(*) from ListSurveyRespondentDemographics where respondentFilter <> 'NULL' or respondentFilter <> 'No Response'
                // Example select count(*) from ListSurveyRespondentDemographics where Gender <> 'NULL' Or Gender != 'No Response'
                return await _listSurveyRespondentService.GetNumberOfRespondents(respondentFilter, cid, eid);
            default:
                throw new Exception($"Unimplemented QuestionType {questionType}");
        }
    }

    public async Task<int> GetNumberOfRespondents(string respondentFilter, int cid, int eid)
    {
        // pulls the number of people in a specific demographic filter
        //Select ValListCompanyId from ListCOmpany where clientid = cid and engagementid = eid
        // select count(*) from ListSurveyRespondentDemographics where respondentFilter and listcompanyid = ValListCompanyId
        // For example: Select count(*) from ListSurveyRespondentDemographics where (Age = '25 to 35' or Age = '35 to 45') and listcompanyid = 3

        respondentFilter = respondentFilter.ToUpper() == "ALL_RESPONDENTS" ? "" : respondentFilter;

        return await _listSurveyRespondentService.GetNumberOfRespondents(respondentFilter, cid, eid);
    }

    private async Task<IEnumerable<string>> GetExistingAnswerOptionsForCompany(string demogHeader, int cid, int eid)
    {
        // returns ACTUAL answer options for a company based on real data
        // Age = 25 to 35, 35 to 45 but other ones didn't have any responses for this company

        //    find Select Distinct [demogHeader] from ListSurveyRespondentDemographics 
        //    add result into list and return 
        //Select ValListCompanyId from ListCOmpany where clientid = cid and engagementid = eid
        // select Distinct [demogHeader] from ListSurveyRespondentDemographics where listcompanyid = ValListCompanyId

        var matchedColumnName = GetMatchedDemographicsColumnName(demogHeader);

        return await _listSurveyRespondentDemographicService.GetExistingAnswerOptionsForCompany(matchedColumnName, cid, eid);
    }

    public double GetCultureAuditScore(int cid, int eid)
    {
        // returns Culture audit on scale from 0 to 100
        // cid & eid are country specific, possible future version using global ids

        //This will be something we'll be allowing to be imported in excel spreadsheet
        //for now set this to 100
        return 100;
    }

    public async Task<int> ProduceRank(int listCompanyId)
    {
        // can call as many times as you want
        // runs across companies
        // OverallGlobalScore_TI_CA

        return await _listSurveyRespondentService.GetProduceRank(listCompanyId);
    }

    private string GetMatchedDemographicsColumnName(string demogHeader)
    {
        var matchedColumns = CultureSurveyConstants.DemographicsColumns.Where(o => o.Key.Equals(demogHeader, StringComparison.OrdinalIgnoreCase));

        if (!matchedColumns.Any())
        {
            throw new Exception($"{demogHeader} be matched to a table column of ListSurveyRespondentDemographics");
        }

        return matchedColumns.First().Value;
    }
}
