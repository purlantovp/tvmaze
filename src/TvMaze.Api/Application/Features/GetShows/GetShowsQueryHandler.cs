using MediatR;
using Microsoft.EntityFrameworkCore;
using TvMaze.Api.Data;
using TvMaze.Api.Models.DTOs;

namespace TvMaze.Api.Application.Features.GetShows;

public class GetShowsQueryHandler : IRequestHandler<GetShowsQuery, PagedResult<ShowDto>>
{
    private readonly ITvMazeContext _context;
    private readonly ILogger<GetShowsQueryHandler> _logger;

    public GetShowsQueryHandler(ITvMazeContext context, ILogger<GetShowsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<ShowDto>> Handle(GetShowsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting shows - Page: {PageNumber}, Size: {PageSize}, OrderBy: {OrderBy}, Search: {SearchTerm}",
            request.PageNumber, request.PageSize, request.OrderBy, request.SearchTerm);

        var query = _context.Shows.Include(s => s.Cast).AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(s => s.Name.Contains(request.SearchTerm) ||
                                    s.Cast.Any(c => c.Name.Contains(request.SearchTerm)));
        }

        // Apply ordering
        query = request.OrderBy?.ToLower() switch
        {
            "name" => query.OrderBy(s => s.Name),
            "name_desc" => query.OrderByDescending(s => s.Name),
            _ => query.OrderBy(s => s.Id)
        };

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
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
    }
}
