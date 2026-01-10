using MediatR;
using Microsoft.EntityFrameworkCore;
using TvMaze.Api.Data;
using TvMaze.Api.Models.DTOs;

namespace TvMaze.Api.Application.Features.GetShowById;

public class GetShowByIdQueryHandler : IRequestHandler<GetShowByIdQuery, ShowDto?>
{
    private readonly ITvMazeContext _context;
    private readonly ILogger<GetShowByIdQueryHandler> _logger;

    public GetShowByIdQueryHandler(ITvMazeContext context, ILogger<GetShowByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ShowDto?> Handle(GetShowByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting show by ID: {ShowId}", request.ShowId);

        var show = await _context.Shows
            .Include(s => s.Cast)
            .FirstOrDefaultAsync(s => s.Id == request.ShowId, cancellationToken);

        if (show == null)
        {
            _logger.LogWarning("Show with ID {ShowId} not found", request.ShowId);
            return null;
        }

        var showDto = new ShowDto
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

        _logger.LogInformation("Retrieved show: {ShowName} with {CastCount} cast members", show.Name, showDto.Cast.Count);

        return showDto;
    }
}
