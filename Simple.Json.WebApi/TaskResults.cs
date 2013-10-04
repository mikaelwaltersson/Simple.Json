using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.WebApi
{
    static class TaskResults
    {
        static readonly Task completed = Task.FromResult(default(AsyncVoid));


        public static Task<TResult> FromError<TResult>(Exception exception)
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>();

            taskCompletionSource.SetException(exception);

            return taskCompletionSource.Task;
        }

        public static Task FromError(Exception exception)
        {
            return FromError<AsyncVoid>(exception);
        }

        public static Task Completed()
        {
            return completed;
        }


        private struct AsyncVoid
        {
        }
    }
}
