using MediatR;
using TvMaze.Api.Models.DTOs;

namespace TvMaze.Api.Application.Features.GetShows;

public record GetShowsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? OrderBy = null,
    string? SearchTerm = null
) : IRequest<PagedResult<ShowDto>>;
