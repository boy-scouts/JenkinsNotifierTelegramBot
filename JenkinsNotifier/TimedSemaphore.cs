using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JenkinsNotifier
{
    public class TimedSemaphore
    {
        private const int HitCheckDelayMs = 100;
        
        private readonly float _timeWindowSeconds;
        private readonly float _maxHitsPerTimeWindow;
        private readonly List<DateTime> _hits;
        private readonly DateTime _creationTime;
        private readonly SemaphoreSlim _semaphore;

        public TimedSemaphore(float timeWindowSeconds, float maxHitsPerTimeWindow)
        {
            _timeWindowSeconds = timeWindowSeconds;
            _maxHitsPerTimeWindow = maxHitsPerTimeWindow;
            _hits = new List<DateTime>();
            _creationTime = DateTime.Now;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        private DateTime TimeWindowStart
        {
            get
            {
                var start = DateTime.Now.Subtract(TimeSpan.FromSeconds(_timeWindowSeconds));
                return start < _creationTime ? _creationTime : start;
            }
        }
        
        private async Task<bool> CheckIfCanHitNow()
        {
            await _semaphore.WaitAsync();

            var timeWindowStart = TimeWindowStart;
            var result = _hits.Count(x => x > timeWindowStart) < _maxHitsPerTimeWindow;

            _semaphore.Release();

            return result;
        }
        
        public async Task Hit()
        {
            int delay = 0;
            while (true)
            {
                var canHit = await CheckIfCanHitNow();
                if (canHit) break;
                await Task.Delay(HitCheckDelayMs);
                delay += HitCheckDelayMs;

                if (delay % 1000 == 0)
                {
                    Logger.Log($"Delay is {delay}ms right now..");
                }
            }

            if (delay > 1000)
            {
                Logger.Log($"Delay released after {delay}ms");
            }
            
            var timeWindowStart = TimeWindowStart;
            _hits.RemoveAll(x => x < timeWindowStart);
            _hits.Add(DateTime.Now);
        }
    }
}