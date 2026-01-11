using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TvMaze.Api.Configuration;
using TvMaze.Api.Data;
using TvMaze.Api.Models.DTOs;
using TvMaze.Api.Services;

namespace TvMaze.Api.Application.Features.GetShows;

public class GetShowsQueryHandler : IRequestHandler<GetShowsQuery, PagedResult<ShowDto>>
{
    private readonly ITvMazeContext _context;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<GetShowsQueryHandler> _logger;

    public GetShowsQueryHandler(
        ITvMazeContext context,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<GetShowsQueryHandler> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    public async Task<PagedResult<ShowDto>> Handle(GetShowsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting shows - Page: {PageNumber}, Size: {PageSize}, OrderBy: {OrderBy}, Search: {SearchTerm}",
            request.PageNumber, request.PageSize, request.OrderBy, request.SearchTerm);

        var cacheKey = $"shows_page_{request.PageNumber}_{request.PageSize}_{request.OrderBy ?? "default"}_{request.SearchTerm ?? "none"}";
        var cacheDuration = TimeSpan.FromMinutes(_cacheSettings.ShowsListCacheDurationMinutes);

        var result = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var query = _context.Shows.Include(s => s.Cast).AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(s => s.Name.ToLower().Contains(searchTerm) ||
                                            s.Cast.Any(c => c.Name.ToLower().Contains(searchTerm)));
                }

                query = request.OrderBy?.ToLower() switch
                {
                    "name" => query.OrderBy(s => s.Name),
                    "name_desc" => query.OrderByDescending(s => s.Name),
                    _ => query.OrderBy(s => s.Id)
                };

                var totalCount = await query.CountAsync(cancellationToken);

                var shows = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => new ShowDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Cast = s.Cast.OrderBy(c => c.Birthday).Select(c => new CastMemberDto
                        {
                            Id = c.CastMemberId,
                            Name = c.Name,
                            Birthday = c.Birthday
                        }).ToList()
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} shows", shows.Count);

                return new PagedResult<ShowDto>
                {
                    Items = shows,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                };
            },
            cacheDuration);

        return result;
    }
}
