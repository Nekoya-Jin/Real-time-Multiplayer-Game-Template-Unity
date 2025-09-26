using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;

namespace Server
{
    public struct JobTimerElem : IComparable<JobTimerElem>
    {
        public int execTick; // 실행 시간
        public Action action;
        public string jobName; // 디버깅용 작업 이름

        public JobTimerElem(Action action, int execTick, string jobName = "Unknown")
        {
            this.action = action;
            this.execTick = execTick;
            this.jobName = jobName;
        }

        public int CompareTo(JobTimerElem other)
        {
            return other.execTick - execTick;
        }
    }

    public class JobTimer
    {
        // ConcurrentQueue 사용으로 lock 제거
        private readonly ConcurrentQueue<JobTimerElem> _jobQueue =
            new ConcurrentQueue<JobTimerElem>();
        private readonly Timer _timer;

        public static JobTimer Instance { get; } = new JobTimer();

        private JobTimer()
        {
            // 10ms마다 작업 처리
            _timer = new Timer(ProcessJobs, null, 10, 10);
        }

        /// <summary>
        /// 작업을 스케줄에 추가 (Thread-Safe)
        /// </summary>
        /// <param name="action">실행할 작업</param>
        /// <param name="tickAfter">지연 시간 (밀리초)</param>
        /// <param name="jobName">디버깅용 작업 이름</param>
        public void Push(Action action, int tickAfter = 0, string jobName = "Unknown")
        {
            if (action == null)
            {
                Logger.LogWarning("Null action passed to JobTimer.Push");
                return;
            }

            var job = new JobTimerElem(action, Environment.TickCount + tickAfter, jobName);
            _jobQueue.Enqueue(job);

            Logger.LogDebug($"Job '{jobName}' scheduled to execute in {tickAfter}ms");
        }

        private void ProcessJobs(object state)
        {
            var executedJobs = 0;
            var now = Environment.TickCount;
            var readyJobs = new List<JobTimerElem>();

            // 실행 준비된 작업들 수집
            while (_jobQueue.TryDequeue(out var job))
            {
                if (job.execTick <= now)
                {
                    readyJobs.Add(job);
                }
                else
                {
                    // 아직 실행 시간이 안 된 작업은 다시 큐에 넣기
                    _jobQueue.Enqueue(job);
                    break;
                }
            }

            // 수집된 작업들 실행
            foreach (var job in readyJobs)
            {
                try
                {
                    job.action.Invoke();
                    executedJobs++;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error executing job '{job.jobName}': {ex.Message}");
                }
            }

            if (executedJobs > 0)
            {
                Logger.LogDebug($"Executed {executedJobs} scheduled jobs");
            }
        }

        /// <summary>
        /// 수동으로 작업 처리 (기존 호환성용)
        /// </summary>
        public void Flush()
        {
            // Timer가 자동으로 처리하므로 빈 구현
            // 기존 코드와의 호환성을 위해 유지
        }

        /// <summary>
        /// 대기 중인 작업 수 반환 (Thread-Safe)
        /// </summary>
        public int PendingJobCount => _jobQueue.Count;

        /// <summary>
        /// 타이머 정리
        /// </summary>
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
