using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);
    }

    /// <summary>
    /// Channel 기반 Lock-Free JobQueue
    /// </summary>
    public class JobQueue : IJobQueue
    {
        private readonly Channel<Action> _channel;
        private readonly ChannelWriter<Action> _writer;
        private readonly ChannelReader<Action> _reader;
        private readonly Task _processingTask;
        private bool _disposed = false;

        public JobQueue()
        {
            _channel = Channel.CreateUnbounded<Action>();
            _writer = _channel.Writer;
            _reader = _channel.Reader;

            // 백그라운드에서 작업 처리
            _processingTask = Task.Run(ProcessJobsAsync);
        }

        public void Push(Action job)
        {
            if (_disposed || job == null)
                return;

            if (!_writer.TryWrite(job))
            {
                Logger.LogWarning("Failed to enqueue job");
            }
        }

        private async Task ProcessJobsAsync()
        {
            await foreach (var job in _reader.ReadAllAsync())
            {
                try
                {
                    job.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error executing job: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 기존 호환성을 위한 Flush (Channel이 자동 처리하므로 빈 구현)
        /// </summary>
        public void Flush()
        {
            // Channel이 자동으로 처리하므로 빈 구현
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _writer.Complete();
            _processingTask?.Wait(5000);
            _processingTask?.Dispose();
        }
    }
}
