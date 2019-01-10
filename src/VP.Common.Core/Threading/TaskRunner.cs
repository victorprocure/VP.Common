using System;
using System.Threading;
using System.Threading.Tasks;

namespace VP.Common.Core.Threading
{
    public static class TaskRunner
    {
        public const bool DefaultConfigureAwaitValue = false;
        
        public static async Task<TResult> Run<TResult>(Func<Task<TResult>> func, bool configureAwait = DefaultConfigureAwaitValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = Task.Run(func, cancellationToken);

            if (!configureAwait)
            {
                return await task.ConfigureAwait(false);
            }

            return await task;
        }

        public static async Task Run(Func<Task> func, bool configureAwait = DefaultConfigureAwaitValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = Task.Run(func, cancellationToken);

            if (!configureAwait)
            {
                await task.ConfigureAwait(false);
            }

            await task;
        }

        public static async Task<TResult> Run<TResult>(Func<TResult> func, bool configureAwait = DefaultConfigureAwaitValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = Task.Run(func, cancellationToken);

            if (!configureAwait)
            {
                return await task.ConfigureAwait(false);
            }

            return await task;
        }

        public static async Task Run(Action action, bool configureAwait = DefaultConfigureAwaitValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = Task.Run(action, cancellationToken);

            if (!configureAwait)
            {
                await task.ConfigureAwait(false);
            }
            else
            {
                await task;
            }
        }
    }
}