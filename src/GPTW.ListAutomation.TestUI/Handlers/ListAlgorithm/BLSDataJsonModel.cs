namespace GPTW.ListAutomation.TestUI.Handlers;

public sealed class BLSDataJsonModel
{
    public BLSIndustryGenderVer2018[] BLSIndustryGenderVer2018 { get; set; }
    public BLSWorkforceMinorityVer2018[] BLSWorkforceMinorityVer2018 { get; set; }
}

public sealed class BLSIndustryGenderVer2018
{
    public string Industry { get; set; }
    public string BLS_pct_women { get; set; }
}

public sealed class BLSWorkforceMinorityVer2018
{
    public string Abbreviation { get; set; }
    public string State { get; set; }
    public string Ethnic_Minority { get; set; }
}
