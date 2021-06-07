using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RestQueue.API.Support
{
    public class BackgroundQueueWorker<T>
    {
        private readonly Func<T, CancellationToken, Task> _asyncMethod;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private Thread? _queueExecution;

        public BackgroundQueueWorker(Func<T, CancellationToken, Task> asyncMethod)
        {
            _asyncMethod = asyncMethod;
        }

        public void Enqueue(T data)
        {
            _queue.Enqueue(data);
            MaybeStartBackgroundJob();
        }

        /// <summary>
        /// Start an internal queue, if it hasn't started yet
        /// </summary>
        private void MaybeStartBackgroundJob()
        {
            lock (_queue)
            {
                if (_queueExecution != null && _queueExecution.IsAlive) return;
                _queueExecution = new Thread(BackgroundJob);
                _queueExecution.Start();
            }
        }

        /// <summary>
        /// While there are items on the queue, take action on them. Quit when no more items.
        /// </summary>
        private void BackgroundJob()
        {
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    var requestData = item;
                    // This way to call an async method from a synchronous method was found here:
                    // https://stackoverflow.com/questions/40324300/calling-async-methods-from-non-async-code
                    Task.Run(() => _asyncMethod(requestData, CancellationToken.None)).Wait();
                }
                catch (Exception)
                {
                    // Log this, but never fail.
                }
            }
        }
    }
}