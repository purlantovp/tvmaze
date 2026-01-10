using MediatR;
using Microsoft.AspNetCore.Mvc;
using TvMaze.Api.Application.Features.ScrapeShows;
using TvMaze.Api.Models.Results;

namespace TvMaze.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ScraperController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScraperController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Triggers a scrape of the TVMaze API and stores the data
    /// </summary>
    /// <param name="startPage">The starting page number (default: 0)</param>
    /// <param name="pageCount">Number of pages to scrape (default: 10)</param>
    /// <returns>Information about the scraping operation</returns>
    /// <response code="200">Returns the number of shows scraped</response>
    /// <response code="400">If the parameters are invalid</response>
    [HttpPost("scrape")]
    [ProducesResponseType(typeof(ScrapeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScrapeResult>> ScrapeShows(
        [FromQuery] int startPage = 0,
        [FromQuery] int pageCount = 10)
    {
        var command = new ScrapeShowsCommand(startPage, pageCount);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
