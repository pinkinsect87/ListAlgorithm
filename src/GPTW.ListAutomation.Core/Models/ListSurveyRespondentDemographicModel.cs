namespace GPTW.ListAutomation.Core.Models;

public sealed class ListSurveyRespondentDemographicModel
{
    public int ListCompanyDemographicsId { get; set; }
    public int ListCompanyId { get; set; }
    public int RespondentKey { get; set; }
    public string Gender { get; set; }
    public string Age { get; set; }
    public string CountryRegion { get; set; }
    public string JobLevel { get; set; }
    public string LgbtOrLgbtQ { get; set; }
    public string RaceEthniticity { get; set; }
    public string Responsibility { get; set; }
    public string Tenure { get; set; }
    public string WorkStatus { get; set; }
    public string WorkType { get; set; }
    public string WorkerType { get; set; }
    public string BirthYear { get; set; }
    public string Confidence { get; set; }
    public string Disabilities { get; set; }
    public string ManagerialLevel { get; set; }
    public string MeaningfulInnovationOpportunities { get; set; }
    public string PayType { get; set; }
    public string Zipcode { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime ModifiedDateTime { get; set; }
}
