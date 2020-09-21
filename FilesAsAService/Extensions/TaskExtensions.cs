using System;
using System.Threading.Tasks;

namespace FilesAsAService.Extensions
{
    /// <summary>
    /// Source: https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types
    /// </summary>
    public static class TaskExtensions
    {
        public static IAsyncResult AsApm<T>(this Task<T> task,
            AsyncCallback callback,
            object? state)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var tcs = new TaskCompletionSource<T>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                if (callback != null)
                    callback(tcs.Task);
            }, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}