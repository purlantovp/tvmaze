using MediatR;

namespace TvMaze.Api.Application.Features.GetShowCount;

public record GetShowCountQuery() : IRequest<int>;
