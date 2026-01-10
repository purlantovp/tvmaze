using MediatR;
using Microsoft.AspNetCore.Mvc;
using TvMaze.Api.Application.Features.GetShowById;
using TvMaze.Api.Application.Features.GetShowCount;
using TvMaze.Api.Application.Features.GetShows;
using TvMaze.Api.Models.DTOs;

namespace TvMaze.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShowsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShowsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a paginated list of TV shows with their cast members
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1)</param>
    /// <param name="pageSize">The number of items per page (default: 10, max: 100)</param>
    /// <param name="orderBy">Sort order: 'name' for ascending, 'name_desc' for descending (default: by ID)</param>
    /// <param name="searchTerm">Optional search term to filter by show name or cast member name</param>
    /// <returns>A paginated list of shows with cast information</returns>
    /// <response code="200">Returns the paginated list of shows</response>
    /// <response code="400">If the page number or page size is invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ShowDto>>> GetShows(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? orderBy = null,
        [FromQuery] string? searchTerm = null)
    {
        var query = new GetShowsQuery(pageNumber, pageSize, orderBy, searchTerm);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific TV show by ID with its cast members
    /// </summary>
    /// <param name="id">The show ID</param>
    /// <returns>The show with cast information</returns>
    /// <response code="200">Returns the show</response>
    /// <response code="404">If the show is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShowDto>> GetShowById(int id)
    {
        var query = new GetShowByIdQuery(id);
        var showDto = await _mediator.Send(query);

        if (showDto == null)
        {
            return NotFound($"Show with ID {id} not found");
        }

        return Ok(showDto);
    }

    /// <summary>
    /// Gets the total count of shows in the database
    /// </summary>
    /// <returns>The total number of shows</returns>
    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetShowCount()
    {
        var query = new GetShowCountQuery();
        var count = await _mediator.Send(query);
        return Ok(count);
    }
}
