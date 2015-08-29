using System;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Sandbox.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();
            var firstTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            if (firstTask == task)
            {
                cts.Cancel();
                return await task;
            }
            
            throw new TimeoutException();
        }

        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();
            var firstTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            if (firstTask == task)
            {
                cts.Cancel();
                return;
            }

            throw new TimeoutException();
        }
    }
}
