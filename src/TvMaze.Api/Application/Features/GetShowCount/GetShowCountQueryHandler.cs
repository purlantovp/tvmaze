using MediatR;
using Microsoft.EntityFrameworkCore;
using TvMaze.Api.Data;

namespace TvMaze.Api.Application.Features.GetShowCount;

public class GetShowCountQueryHandler : IRequestHandler<GetShowCountQuery, int>
{
    private readonly ITvMazeContext _context;
    private readonly ILogger<GetShowCountQueryHandler> _logger;

    public GetShowCountQueryHandler(ITvMazeContext context, ILogger<GetShowCountQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> Handle(GetShowCountQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting total show count");

        var count = await _context.Shows.CountAsync(cancellationToken);

        _logger.LogInformation("Total shows in database: {Count}", count);

        return count;
    }
}
