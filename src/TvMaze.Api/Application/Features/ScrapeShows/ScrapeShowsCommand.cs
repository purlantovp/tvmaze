using MediatR;
using TvMaze.Api.Models.Results;

namespace TvMaze.Api.Application.Features.ScrapeShows;

public record ScrapeShowsCommand(
    int StartPage = 0,
    int PageCount = 10
) : IRequest<ScrapeResult>;
