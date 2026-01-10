using MediatR;
using TvMaze.Api.Models.DTOs;

namespace TvMaze.Api.Application.Features.GetShowById;

public record GetShowByIdQuery(int ShowId) : IRequest<ShowDto?>;
