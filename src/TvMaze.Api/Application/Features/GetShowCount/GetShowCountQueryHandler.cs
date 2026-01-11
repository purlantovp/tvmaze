using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TvMaze.Api.Configuration;
using TvMaze.Api.Data;
using TvMaze.Api.Services;

namespace TvMaze.Api.Application.Features.GetShowCount;

public class GetShowCountQueryHandler : IRequestHandler<GetShowCountQuery, int>
{
    private readonly ITvMazeContext _context;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<GetShowCountQueryHandler> _logger;

    public GetShowCountQueryHandler(
        ITvMazeContext context,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<GetShowCountQueryHandler> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    public async Task<int> Handle(GetShowCountQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting total show count");

        var cacheKey = "shows_count";
        var cacheDuration = TimeSpan.FromMinutes(_cacheSettings.ShowCountCacheDurationMinutes);

        var count = await _cacheService.GetOrCreateValueAsync(
            cacheKey,
            async () =>
            {
                var totalCount = await _context.Shows.CountAsync(cancellationToken);
                _logger.LogInformation("Total shows in database: {Count}", totalCount);
                return totalCount;
            },
            cacheDuration);

        return count;
    }
}
