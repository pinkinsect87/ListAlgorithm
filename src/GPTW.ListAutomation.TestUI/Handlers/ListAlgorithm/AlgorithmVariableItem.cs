namespace GPTW.ListAutomation.TestUI.Handlers;

public sealed class AlgorithmVariableItem
{
    public ModuleType ModuleType { get; set; }

    public string ModuleIfCondition { get; set; }

    public string ModuleParameters { get; set; }

    public string Expression { get; set; }

    public string OutputVariable { get; set; }

    public int SortIndex { get; set; } = 1;
}
