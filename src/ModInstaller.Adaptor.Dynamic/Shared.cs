using System;
using System.Threading.Tasks;

namespace FomodInstaller.Interface;

internal struct Shared
{
    public const int TIMEOUT_MS = 30000;
    public const int TIMEOUT_RETRIES = 1;
    
    public static async Task<T> TimeoutRetry<T>(Func<Task<T>> cb, int tries = TIMEOUT_RETRIES)
    {
        try
        {
            return await cb().WaitAsync(TimeSpan.FromMilliseconds(TIMEOUT_MS));
        }
        catch (TimeoutException)
        {
            if (tries > 0)
            {
                return await TimeoutRetry(cb, tries - 1);
            } else
            {
                throw;
            }
        }
    }

    public static T TimeoutRetrySync<T>(Func<Task<T>> cb, int tries = TIMEOUT_RETRIES)
    {
        var task = cb();
        if (task.Wait(TimeSpan.FromMilliseconds(TIMEOUT_MS)))
        {
            return task.Result;
        }

        if (tries > 0)
        {
            return TimeoutRetrySync(cb, tries - 1);
        }

        throw new TimeoutException();
    }
}