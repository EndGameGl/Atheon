namespace Atheon.Services.Scanners.Entities;

public class ScannerStepInfo<TInput, TContext>
{
    public Func<TInput, TContext, CancellationToken, ValueTask<bool>> Delegate { get; init; }
    public string StepName { get; init; }
    public bool ExecuteAfterErrors { get; init; }

}
