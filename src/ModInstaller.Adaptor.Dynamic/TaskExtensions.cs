#if NET48
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FomodInstaller.Interface;

internal static class TaskExtensions
{
    public static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        using (var cts = new CancellationTokenSource())
        {
            var delayTask = Task.Delay(timeout, cts.Token);
            var completedTask = await Task.WhenAny(task, delayTask);
            
            if (completedTask == delayTask)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalMilliseconds}ms");
            }
            
            cts.Cancel();
            return await task;
        }
    }
}
#endif