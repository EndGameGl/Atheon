using Atheon.Attributes;
using System.Reflection;

namespace Atheon.Services.Scanners.Entities
{
    public abstract class EntityScannerBase<TInput, TContext>
        where TContext : class
        where TInput : class
    {
        private ScannerStepInfo<TInput, TContext>[] _steps;
        protected ILogger Logger { get; }

        protected EntityScannerBase(ILogger logger)
        {
            Logger = logger;
        }

        public async Task Scan(
            TInput input,
            TContext context,
            CancellationToken cancellationToken)
        {
            var shouldContinue = true;

            for (var i = 0; i < _steps.Length; i++)
            {
                var nextStep = _steps[i];

                try
                {
                    if (!shouldContinue && !nextStep.ExecuteAfterErrors)
                        continue;
                    var executionResult = await nextStep.Delegate(input, context, cancellationToken);
                    if (executionResult is false)
                    {
                        shouldContinue = false;
                    }
                }
                catch (OperationCanceledException operationCanceledException)
                {
                    Logger.LogError(
                        operationCanceledException,
                        "Step {ScannerStepName} execution was canceled",
                        nextStep.StepName);
                    return;
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to execute step {ScannerStepName}", nextStep.StepName);
                    shouldContinue = false;
                }
            }
        }

        protected void Initialize()
        {
            var currentScannerType = GetType();

            var methodsInfo = currentScannerType.GetMethods();

            var actualSteps = new Dictionary<MethodInfo, ScanStepAttribute>();

            foreach (var methodInfo in methodsInfo)
            {
                var scanStepAttribute = methodInfo.GetCustomAttribute<ScanStepAttribute>();

                if (scanStepAttribute is null)
                    continue;

                actualSteps.Add(methodInfo, scanStepAttribute);
            }

            var orderedSteps = actualSteps.OrderBy(x => x.Value.StepOrder);

            var steps = new List<ScannerStepInfo<TInput, TContext>>();
            foreach (var step in orderedSteps)
            {
                var delegateFunc =
                    (Func<TInput, TContext, CancellationToken, ValueTask<bool>>)Delegate.CreateDelegate(
                        typeof(Func<TInput, TContext, CancellationToken, ValueTask<bool>>),
                        this,
                        step.Key);

                steps.Add(new ScannerStepInfo<TInput, TContext>()
                {
                    Delegate = delegateFunc,
                    StepName = step.Value.StepName,
                    ExecuteAfterErrors = step.Value.ExecuteAfterErrors
                });
            }

            _steps = steps.ToArray();
        }
    }
}
