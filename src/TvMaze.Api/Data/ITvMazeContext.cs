using Microsoft.EntityFrameworkCore;
using TvMaze.Api.Models;

namespace TvMaze.Api.Data;

public interface ITvMazeContext
{
    DbSet<Show> Shows { get; set; }
    DbSet<CastMember> CastMembers { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
