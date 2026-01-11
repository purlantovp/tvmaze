using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TvMaze.Api.Configuration;
using TvMaze.Api.Data;
using TvMaze.Api.Models.DTOs;
using TvMaze.Api.Services;

namespace TvMaze.Api.Application.Features.GetShowById;

public class GetShowByIdQueryHandler : IRequestHandler<GetShowByIdQuery, ShowDto?>
{
    private readonly ITvMazeContext _context;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<GetShowByIdQueryHandler> _logger;

    public GetShowByIdQueryHandler(
        ITvMazeContext context,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<GetShowByIdQueryHandler> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    public async Task<ShowDto?> Handle(GetShowByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting show by ID: {ShowId}", request.ShowId);

        var cacheKey = $"show_{request.ShowId}";
        var cacheDuration = TimeSpan.FromMinutes(_cacheSettings.ShowByIdCacheDurationMinutes);

        var showDto = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var show = await _context.Shows
                    .Include(s => s.Cast)
                    .FirstOrDefaultAsync(s => s.Id == request.ShowId, cancellationToken);

                if (show == null)
                {
                    _logger.LogWarning("Show with ID {ShowId} not found", request.ShowId);
                    return null;
                }

                var dto = new ShowDto
                {
                    Id = show.Id,
                    Name = show.Name,
                    Cast = show.Cast.OrderBy(c => c.Birthday).Select(c => new CastMemberDto
                    {
                        Id = c.CastMemberId,
                        Name = c.Name,
                        Birthday = c.Birthday
                    }).ToList()
                };

                _logger.LogInformation("Retrieved show: {ShowName} with {CastCount} cast members", show.Name, dto.Cast.Count);

                return dto;
            },
            cacheDuration);

        return showDto;
    }
}
