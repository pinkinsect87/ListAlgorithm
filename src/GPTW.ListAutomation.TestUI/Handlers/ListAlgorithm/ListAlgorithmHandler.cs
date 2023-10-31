using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.Core.Services.Data;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using GPTW.ListAutomation.TestUI.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace GPTW.ListAutomation.TestUI.Handlers;

public interface IListAlgorithmHandler
{
    Task Process(ListCompanyModel listCompany, string filePath, string countryRegionId);
}

public sealed class ListAlgorithmHandler : IListAlgorithmHandler
{
    private readonly GPTWModules _gptwModules;

    private readonly IListCompanyCultureBriefService _listCompanyCultureBriefService;
    private readonly IListAutomationResultService _listAutomationResultService;
    private readonly IBlsdataService _blsDataService;
    private readonly ILogger<IListAlgorithmHandler> _logger;

    public ListAlgorithmHandler(
        GPTWModules gptwModules,
        IListCompanyCultureBriefService listCompanyCultureBriefService,
        IListAutomationResultService listAutomationResultService,
        IBlsdataService blsDataService,
        ILogger<IListAlgorithmHandler> logger)
    {
        _gptwModules = gptwModules;
        _listCompanyCultureBriefService = listCompanyCultureBriefService;
        _listAutomationResultService = listAutomationResultService;
        _blsDataService = blsDataService;
        _logger = logger;
    }

    public async Task Process(ListCompanyModel listCompany, string filePath, string countryRegionId)
    {
        var listAutomationResults = new List<ListAutomationResultModel>();
        var algorithmDynamicVariableItems = new List<AlgorithmVariableItem>();
        var cultureBriefVariables = _listCompanyCultureBriefService.GetCultureBriefVariablesByListCompanyId(listCompany.ListCompanyId);

        #region Process algorithm file

        var contents = File.ReadAllText(filePath);

        if (contents.StartsWith("{\r\n  [\r\n"))
        {
            contents = "{\r\n  \"data\": [\r\n" + contents.Substring("{\r\n  [\r\n".Length + 1);
        }
        else if (contents.StartsWith("{\r\n [\r\n"))
        {
            contents = "{\r\n  \"data\": [\r\n" + contents.Substring("{\r\n [\r\n".Length + 1);
        }

        if (filePath.EndsWith("BLS_data.json", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessBLSData(contents);
        }
        else
        {
            try
            {
                var blsDataVariables = _blsDataService.GetTable().ToList();

                var json = JObject.Parse(contents);

                if (json.HasValues)
                {
                    JProperty property = json.First as JProperty;

                    if (property.HasValues && property.Value.Type == JTokenType.Array)
                    {
                        JArray jArray = (JArray)property.Value;

                        foreach (var item in jArray)
                        {
                            var module = item["module"] as JValue;
                            var module_parameters = item["module_parameters"] as JValue;
                            var output_variable = item["output_variable"] as JValue;

                            if (module != null
                                && output_variable != null
                                && Enum.TryParse<ModuleType>((string)module.Value, true, out var moduleType))
                            {
                                var module_if_condition = item["module_if_condition"] as JValue;

                                var algorithmVariableItem = new AlgorithmVariableItem
                                {
                                    ModuleType = moduleType,
                                    OutputVariable = (string)output_variable.Value
                                };

                                var sModuleIfCondition = module_if_condition == null ? "" : (string)module_if_condition.Value;
                                var sModuleParameters = module_parameters == null ? "" : (string)module_parameters.Value;

                                if (module_if_condition == null)
                                {
                                    algorithmVariableItem.ModuleIfCondition = "";
                                    algorithmVariableItem.ModuleParameters = sModuleParameters.ToSafeVariable();
                                    algorithmVariableItem.Expression = sModuleParameters;
                                }
                                else
                                {
                                    algorithmVariableItem.ModuleIfCondition = sModuleIfCondition.ToSafeVariable();
                                    algorithmVariableItem.ModuleParameters = sModuleParameters;
                                    algorithmVariableItem.Expression = sModuleIfCondition;
                                }

                                var resultKey = algorithmVariableItem.OutputVariable.ToSafeVariable();

                                switch (moduleType)
                                {
                                    #region StaticValue

                                    case ModuleType.StaticValue:
                                        listAutomationResults.Add(new ListAutomationResultModel
                                        {
                                            ListCompanyId = listCompany.ListCompanyId,
                                            ResultKey = algorithmVariableItem.OutputVariable.ToSafeVariable(),
                                            ResultValue = algorithmVariableItem.ModuleParameters
                                        });
                                        break;

                                    #endregion

                                    #region GetDemographicNetPenalty

                                    case ModuleType.GetDemographicNetPenalty:
                                        if (item["module_parameters"].Type == JTokenType.Object)
                                        {
                                            var respondent_filter = item["module_parameters"]["respondent_filter"].ToString();
                                            var demogHeader = item["module_parameters"]["demogHeader"].ToString();
                                            var confidenceAnswerOption = item["module_parameters"]["confidenceAnswerOption"].ToString();
                                            var nonConfidenceAnswerOption = item["module_parameters"]["nonConfidenceAnswerOption"].ToString();
                                            var demogHeaderToSkip = item["module_parameters"]["demogHeaderToSkip"].ToString();
                                            var demogHeaderAnswerOptionToSkip = item["module_parameters"]["demogHeaderAnswerOptionToSkip"].ToString();
                                            var demogWeightingList = item["module_parameters"]["demogWeightingList"].ToString();

                                            Dictionary<string, string> demogHeaderAnswerOptionToSkipValues = new Dictionary<string, string>();
                                            Dictionary<string, double> demogWeightingListValues = new Dictionary<string, double>();

                                            if (demogHeaderAnswerOptionToSkip.IsPresent())
                                            {
                                                var values = demogHeaderAnswerOptionToSkip.Split(',');
                                                foreach (string value in values)
                                                {
                                                    var arr = value.Split('=');
                                                    if (arr.Length == 2)
                                                    {
                                                        demogHeaderAnswerOptionToSkipValues.Add(arr[0].Trim(), arr[1].Trim());
                                                    }
                                                }
                                            }

                                            if (demogWeightingList.IsPresent())
                                            {
                                                var values = demogWeightingList.Split(',');
                                                foreach (string value in values)
                                                {
                                                    var arr = value.Split('=');
                                                    if (arr.Length == 2)
                                                    {
                                                        demogWeightingListValues.Add(arr[0].Trim(), double.Parse(arr[1].Trim()));
                                                    }
                                                }
                                            }

                                            using (_logger.MeasureOperation($"GetDemographicNetPenalty - {resultKey}"))
                                            {
                                                var demographicNetPenalty = await _gptwModules.GetDemographicNetPenalty(
                                                                respondent_filter,
                                                                demogHeader,
                                                                confidenceAnswerOption,
                                                                nonConfidenceAnswerOption,
                                                                demogHeaderToSkip,
                                                                demogHeaderAnswerOptionToSkipValues,
                                                                demogWeightingListValues,
                                                                listCompany.ClientId,
                                                                listCompany.EngagementId);

                                                listAutomationResults.Add(new ListAutomationResultModel
                                                {
                                                    ListCompanyId = listCompany.ListCompanyId,
                                                    ResultKey = resultKey,
                                                    ResultValue = demographicNetPenalty.ToString()
                                                });
                                            }
                                        }
                                        break;

                                    #endregion

                                    #region GetDemographicTIPenalty

                                    case ModuleType.GetDemographicTIPenalty:
                                        if (item["module_parameters"].Type == JTokenType.Object)
                                        {
                                            var stmts = item["module_parameters"]["stmts"].ToString();
                                            var respondent_filter = item["module_parameters"]["respondent_filter"].ToString();
                                            var demogHeaderToSkip = item["module_parameters"]["header_to_skip"].ToString();
                                            var header_ans_to_skip = item["module_parameters"]["header_ans_to_skip"].ToString();
                                            var weightings = item["module_parameters"]["weightings"].ToString();

                                            Dictionary<string, string> demogHeaderAnswerOptionToSkipValues = new Dictionary<string, string>();
                                            Dictionary<string, double> demogWeightingListValues = new Dictionary<string, double>();

                                            if (header_ans_to_skip.IsPresent())
                                            {
                                                var values = header_ans_to_skip.Split(',');
                                                foreach (string value in values)
                                                {
                                                    var arr = value.Split('=');
                                                    if (arr.Length == 2)
                                                    {
                                                        demogHeaderAnswerOptionToSkipValues.Add(arr[0].Trim(), arr[1].Trim());
                                                    }
                                                }
                                            }

                                            if (weightings.IsPresent())
                                            {
                                                var values = weightings.Split(',');
                                                foreach (string value in values)
                                                {
                                                    var arr = value.Split('=');
                                                    if (arr.Length == 2)
                                                    {
                                                        demogWeightingListValues.Add(arr[0].Trim(), double.Parse(arr[1].Trim()));
                                                    }
                                                }
                                            }

                                            List<int> statementIds = new List<int>();

                                            if (stmts.ToUpper() == "ALL_60_STMTS")
                                            {
                                                statementIds = CultureSurveyConstants.RespondentColumns.ToList();
                                            }
                                            else
                                            {
                                                statementIds = Array.ConvertAll(stmts.Split(','), int.Parse).ToList();
                                            }

                                            using (_logger.MeasureOperation($"GetDemographicTIPenalty - {resultKey}"))
                                            {
                                                var demographicTIPenalty = await _gptwModules.GetDemographicTIPenalty(
                                                            statementIds,
                                                            respondent_filter,
                                                            demogHeaderToSkip,
                                                            demogHeaderAnswerOptionToSkipValues,
                                                            demogWeightingListValues,
                                                            listCompany.ClientId,
                                                            listCompany.EngagementId);

                                                listAutomationResults.Add(new ListAutomationResultModel
                                                {
                                                    ListCompanyId = listCompany.ListCompanyId,
                                                    ResultKey = resultKey,
                                                    ResultValue = demographicTIPenalty.ToString()
                                                });
                                            }
                                        }
                                        break;

                                    #endregion

                                    #region GetNetDemographicScore

                                    case ModuleType.GetNetDemographicScore:
                                        if (item["module_parameters"].Type == JTokenType.Object)
                                        {
                                            var respondent_filter = item["module_parameters"]["respondent_filter"].ToString();
                                            var demogHeader = item["module_parameters"]["demogHeader"].ToString();
                                            var confidenceAnswerOption = item["module_parameters"]["confidenceAnswerOption"].ToString();
                                            var nonConfidenceAnswerOption = item["module_parameters"]["nonConfidenceAnswerOption"].ToString();

                                            using (_logger.MeasureOperation($"GetNetDemographicScore - {resultKey}"))
                                            {
                                                var demographicScore = await _gptwModules.GetNetDemographicScore(
                                                            respondent_filter,
                                                            demogHeader,
                                                            confidenceAnswerOption,
                                                            nonConfidenceAnswerOption,
                                                            listCompany.ClientId,
                                                            listCompany.EngagementId);

                                                listAutomationResults.Add(new ListAutomationResultModel
                                                {
                                                    ListCompanyId = listCompany.ListCompanyId,
                                                    ResultKey = resultKey,
                                                    ResultValue = demographicScore.ToString()
                                                });
                                            }
                                        }
                                        break;

                                    #endregion

                                    #region GetTrustIndexScore

                                    case ModuleType.GetTrustIndexScore:
                                        if (item["module_parameters"].Type == JTokenType.Object)
                                        {
                                            var stmts = item["module_parameters"]["stmts"].ToString();
                                            var respondent_filter = item["module_parameters"]["respondent_filter"].ToString();

                                            List<int> statementIds = new List<int>();
                                            if (stmts.ToUpper() != "ALL_CORE_STATEMENTS")
                                            {
                                                statementIds = Array.ConvertAll(stmts.Split(','), int.Parse).ToList();
                                            }

                                            using (_logger.MeasureOperation($"GetTrustIndexScore - {resultKey}"))
                                            {
                                                var trustIndexScore = await _gptwModules.GetTrustIndexScore(
                                                            statementIds,
                                                            respondent_filter,
                                                            listCompany.ClientId,
                                                            listCompany.EngagementId);

                                                listAutomationResults.Add(new ListAutomationResultModel
                                                {
                                                    ListCompanyId = listCompany.ListCompanyId,
                                                    ResultKey = resultKey,
                                                    ResultValue = trustIndexScore.ToString()
                                                });
                                            }
                                        }
                                        break;

                                    #endregion

                                    #region GetCultureAuditScore

                                    case ModuleType.GetCultureAuditScore:
                                        listAutomationResults.Add(new ListAutomationResultModel
                                        {
                                            ListCompanyId = listCompany.ListCompanyId,
                                            ResultKey = algorithmVariableItem.OutputVariable.ToSafeVariable(),
                                            ResultValue = _gptwModules.GetCultureAuditScore(listCompany.ClientId, listCompany.EngagementId).ToString()
                                        });
                                        break;

                                    #endregion

                                    #region GetNumberOfRespondents

                                    case ModuleType.GetNumberOfRespondents:
                                        if (item["module_parameters"].Type == JTokenType.Object)
                                        {
                                            var respondent_filter = item["module_parameters"]["respondent_filter"].ToString();

                                            using (_logger.MeasureOperation($"GetNumberOfRespondents - {resultKey}"))
                                            {
                                                var numberOfRespondents = await _gptwModules.GetNumberOfRespondents(respondent_filter, listCompany.ClientId, listCompany.EngagementId);

                                                listAutomationResults.Add(new ListAutomationResultModel
                                                {
                                                    ListCompanyId = listCompany.ListCompanyId,
                                                    ResultKey = resultKey,
                                                    ResultValue = numberOfRespondents.ToString()
                                                });
                                            }
                                        }
                                        break;

                                    #endregion

                                    #region ProduceRank

                                    case ModuleType.ProduceRank:
                                        using (_logger.MeasureOperation($"ProduceRank - {resultKey}"))
                                        {
                                            var produceRank = await _gptwModules.ProduceRank(listCompany.ListCompanyId);
                                            listAutomationResults.Add(new ListAutomationResultModel
                                            {
                                                ListCompanyId = listCompany.ListCompanyId,
                                                ResultKey = resultKey,
                                                ResultValue = produceRank.ToString()
                                            });
                                        }
                                        break;

                                    #endregion

                                    #region GetCompanyData

                                    case ModuleType.GetCompanyData:
                                        var resultModel = new ListAutomationResultModel
                                        {
                                            ListCompanyId = listCompany.ListCompanyId,
                                            ResultKey = algorithmVariableItem.OutputVariable.ToSafeVariable()
                                        };

                                        if (algorithmVariableItem.ModuleParameters == "engagement_id")
                                        {
                                            resultModel.ResultValue = listCompany.EngagementId.ToString();
                                        }
                                        else if (algorithmVariableItem.ModuleParameters == "client_id")
                                        {
                                            resultModel.ResultValue = listCompany.ClientId.ToString();
                                        }
                                        else if (algorithmVariableItem.ModuleParameters == "survey_ver_id")
                                        {
                                            resultModel.ResultValue = listCompany.SurveyVersionId.ToString();
                                        }
                                        else if (algorithmVariableItem.ModuleParameters == "countryregion_id")
                                        {
                                            resultModel.ResultValue = countryRegionId;
                                        }
                                        else if (algorithmVariableItem.ModuleParameters == "ECRClientName")
                                        {
                                            resultModel.ResultValue = listCompany.ClientName;
                                        }
                                        listAutomationResults.Add(resultModel);
                                        break;

                                    #endregion

                                    #region GetCultureBriefVariable

                                    case ModuleType.GetCultureBriefVariable:
                                        var variableName = algorithmVariableItem.ModuleParameters
                                                                .Replace("countryregion_id", countryRegionId)
                                                                .Replace("CB$ca1", "ca1");

                                        var cultureBriefVariable = cultureBriefVariables
                                            .FirstOrDefault(o => o.VariableName.Equals(variableName, StringComparison.OrdinalIgnoreCase));

                                        if (cultureBriefVariable == null)
                                        {
                                            throw new Exception($"Unable to find VariableName - {algorithmVariableItem.ModuleParameters} from ListCompanyCultureBrief table");
                                        }

                                        listAutomationResults.Add(new ListAutomationResultModel
                                        {
                                            ListCompanyId = listCompany.ListCompanyId,
                                            ResultKey = algorithmVariableItem.OutputVariable.ToSafeVariable(),
                                            ResultValue = cultureBriefVariable.VariableValue
                                        });
                                        break;

                                    #endregion

                                    #region GetBLSData

                                    case ModuleType.GetBLSData:
                                        var reg = new Regex("(?:\\[)(.*?)(?(1)\\])");
                                        var match = reg.Match(algorithmVariableItem.Expression);

                                        if (match != null && match.Value.IsPresent())
                                        {
                                            var parameter = match.Value.Replace("[", "").Replace("]", "");

                                            if (parameter != null)
                                            {
                                                var variable = listAutomationResults.FirstOrDefault(o => $"${o.ResultKey}".Equals(parameter, StringComparison.OrdinalIgnoreCase));

                                                if (variable != null)
                                                {
                                                    var expression = algorithmVariableItem.Expression.Replace(parameter, variable.ResultValue);

                                                    var bls = blsDataVariables.FirstOrDefault(o => o.BlsdataKey.Equals(expression, StringComparison.OrdinalIgnoreCase));

                                                    if (bls != null)
                                                    {
                                                        listAutomationResults.Add(new ListAutomationResultModel
                                                        {
                                                            ListCompanyId = listCompany.ListCompanyId,
                                                            ResultKey = algorithmVariableItem.OutputVariable.ToSafeVariable(),
                                                            ResultValue = bls.BlsdataValue
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                    #endregion

                                    #region DynamicMath

                                    case ModuleType.DynamicMath:
                                        algorithmDynamicVariableItems.Add(algorithmVariableItem);
                                        break;

                                    #endregion

                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }

                #region Process DynamicMath variables

                if (algorithmDynamicVariableItems.Any())
                {
                    using (_logger.MeasureOperation("Process DynamicMath variables"))
                    {
                        // Not dependent on others
                        var items = algorithmDynamicVariableItems.Where(o => !algorithmDynamicVariableItems.Any(i => i.Expression.Contains(o.OutputVariable, StringComparison.OrdinalIgnoreCase))).ToList();

                        foreach (var item in items)
                        {
                            item.SortIndex = 100;

                            SortItems(item, algorithmDynamicVariableItems);
                        }

                        items = algorithmDynamicVariableItems.OrderBy(o => o.SortIndex).ToList();

                        foreach (var item in items)
                        {
                            if (listAutomationResults.Any(o => o.ResultKey == item.OutputVariable.ToSafeVariable()))
                            {
                                continue;
                            }

                            var variableItems = algorithmDynamicVariableItems
                                .Where(o => o.OutputVariable == item.OutputVariable)
                                .OrderByDescending(o => o.ModuleIfCondition)
                                .ToList();

                            var expressionCompiler = new ExpressionCompiler(_logger, variableItems);
                            if (expressionCompiler.HasError)
                            {
                                throw new Exception($"Invalid Expression - {item.Expression}");
                            }

                            object? value = null;
                            var parameters = expressionCompiler.GetParameters();

                            if (parameters.Any())
                            {
                                var p = new object[parameters.Count];

                                for (var i = 0; i < parameters.Count; i++)
                                {
                                    var para = parameters[i];
                                    var listAutomationResult = listAutomationResults.FirstOrDefault(o => o.ResultKey.Equals(para, StringComparison.OrdinalIgnoreCase));
                                    if (listAutomationResult == null)
                                    {
                                        throw new Exception($"The variable {para} can't be found for Expression - {item.Expression}");
                                    }

                                    p[i] = listAutomationResult.ResultValue.IsPresent() ? double.Parse(listAutomationResult.ResultValue) : 0;
                                }

                                value = expressionCompiler.Compute(p);
                            }
                            else
                            {
                                value = expressionCompiler.Compute(null);
                            }

                            listAutomationResults.Add(new ListAutomationResultModel
                            {
                                ListCompanyId = listCompany.ListCompanyId,
                                ResultKey = item.OutputVariable.ToSafeVariable(),
                                ResultValue = value == null ? null : value.ToString()
                            });
                        }
                    }
                }

                #endregion

                using (_logger.MeasureOperation("Save ListAutomationResult"))
                {
                    _listAutomationResultService.ClearListAutomationResultByListCompanyId(listCompany.ListCompanyId);

                    _listAutomationResultService.BulkInsert(listAutomationResults.Select(la => new ListAutomationResult
                    {
                        ListCompanyId = la.ListCompanyId,
                        ResultKey = la.ResultKey,
                        ResultValue = la.ResultValue,
                        CalculatedDate = DateTime.Now
                    }));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Fail to process algorithm file - " + ex.Message);
            }
        }

        #endregion
    }

    private async Task ProcessBLSData(string jsonBLSData)
    {
        var blsData = new List<Blsdata>();
        var blsJsonData = JsonConvert.DeserializeObject<BLSDataJsonModel>(jsonBLSData);

        if (blsJsonData != null && blsJsonData.BLSIndustryGenderVer2018 != null && blsJsonData.BLSIndustryGenderVer2018.Any())
        {
            foreach (var item in blsJsonData.BLSIndustryGenderVer2018)
            {
                blsData.Add(new Blsdata
                {
                    BlsdataKey = $"BLSIndustryGenderVer2018[{item.Industry}]",
                    BlsdataValue = item.BLS_pct_women
                });
            }
        }

        if (blsJsonData != null && blsJsonData.BLSWorkforceMinorityVer2018 != null && blsJsonData.BLSWorkforceMinorityVer2018.Any())
        {
            foreach (var item in blsJsonData.BLSWorkforceMinorityVer2018)
            {
                var key = item.State == null ? item.Abbreviation : item.State;
                blsData.Add(new Blsdata
                {
                    BlsdataKey = $"BLSWorkforceMinorityVer2018[{key}]",
                    BlsdataValue = item.Ethnic_Minority
                });
            }
        }

        if (blsData.Any())
            await _blsDataService.BatchSave(blsData);
    }

    private void SortItems(AlgorithmVariableItem parent, List<AlgorithmVariableItem> items)
    {
        var children = items.Where(o => parent.Expression.Contains(o.OutputVariable, StringComparison.OrdinalIgnoreCase)).ToList();

        if (children.Any())
        {
            foreach (var item in children)
            {
                item.SortIndex = parent.SortIndex - 1;

                SortItems(item, items);
            }
        }
    }
}
