namespace Atheon.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ScanStepAttribute : Attribute
{
    public string StepName { get; }
    public int StepOrder { get; }
    public bool ExecuteAfterErrors { get; }

    public ScanStepAttribute(string stepName, int stepOrder, bool executeAfterErrors = false)
    {
        StepName = stepName;
        StepOrder = stepOrder;
        ExecuteAfterErrors = executeAfterErrors;
    }

}
