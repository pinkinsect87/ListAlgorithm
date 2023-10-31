namespace GPTW.ListAutomation.Core.Models
{
    public sealed class ListAutomationResultModel
    {
        public int ListAutomationResultId { get; set; }
        public string ResultKey { get; set; }
        public string? ResultValue { get; set; }
        public int ListCompanyId { get; set; }
        //public DateTime? CalculatedDate { get; set; }
        //public decimal? Variation { get; set; }
        //public string? CalculationNotes { get; set; }
        //public string? CalculationStatus { get; set; }
        //public bool IsCurrent { get; set; }
        //public string? InternalNotes { get; set; }
    }
}
