using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentHD.Logic
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal class TokenBucketRateLimiter
    {
        private readonly int _maxTokens;
        private int _tokens;
        private readonly object _lock = new object();
        private readonly TimeSpan _refillInterval;
        private readonly Timer _refillTimer;

        public TokenBucketRateLimiter(int maxTokens, TimeSpan refillInterval)
        {
            _maxTokens = maxTokens;
            _tokens = maxTokens;
            _refillInterval = refillInterval;

            _refillTimer = new Timer(RefillTokens, null, _refillInterval, _refillInterval);
        }

        private void RefillTokens(object state)
        {
            lock (_lock)
            {
                _tokens = _maxTokens; 
                Monitor.PulseAll(_lock); 
            }
        }

        public async Task WaitAsync()
        {
            lock (_lock)
            {
                while (_tokens <= 0)
                {
                    Monitor.Wait(_lock); 
                }
                _tokens--;
            }
        }
    }
}
