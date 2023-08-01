namespace Atheon.Extensions;

public static class TaskExtensions
{
    public static async Task ExecuteInTimeOrThrow(
        this Task task,
        int millisecondsTimeout)
    {
        var delayTask = Task.Delay(millisecondsTimeout);
        var completedTask = await Task.WhenAny(task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException($"Failed to complete task in {millisecondsTimeout} ms");
        }
    }
}
