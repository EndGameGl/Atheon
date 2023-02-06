using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;

namespace Atheon.Services.Hosted
{
    public class MemoryCacheBackgroundCleaner : PeriodicBackgroundService
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheBackgroundCleaner(
            ILogger<MemoryCacheBackgroundCleaner> logger,
            IMemoryCache memoryCache) : base(logger)
        {
            _memoryCache = memoryCache;
        }

        protected override Task BeforeExecutionAsync(CancellationToken stoppingToken)
        {
            ChangeTimerSafe(TimeSpan.FromSeconds(60));
            return Task.CompletedTask;
        }

        protected override Task OnTimerExecuted(CancellationToken cancellationToken)
        {
            _memoryCache.CleanExpiredEntries();
            return Task.CompletedTask;
        }
    }
}
