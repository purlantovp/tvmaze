using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TvMaze.Api.Data;
using TvMaze.Api.Models;
using TvMaze.Api.Models.Results;
using TvMaze.Api.Models.TvMazeDtos;

namespace TvMaze.Api.Application.Features.ScrapeShows;

public class ScrapeShowsCommandHandler : IRequestHandler<ScrapeShowsCommand, ScrapeResult>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TvMazeContext _context;
    private readonly ILogger<ScrapeShowsCommandHandler> _logger;

    public ScrapeShowsCommandHandler(
        IHttpClientFactory httpClientFactory,
        TvMazeContext context,
        ILogger<ScrapeShowsCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    public async Task<ScrapeResult> Handle(ScrapeShowsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting scrape from page {StartPage} for {PageCount} pages",
            request.StartPage, request.PageCount);

        var shows = new List<Show>();
        var httpClient = _httpClientFactory.CreateClient();

        // Fetch shows from TVMaze API
        for (int page = request.StartPage; page < request.StartPage + request.PageCount; page++)
        {
            try
            {
                var showsUrl = $"https://api.tvmaze.com/shows?page={page}";
                var showsResponse = await httpClient.GetStringAsync(showsUrl, cancellationToken);
                var tvMazeShows = JsonSerializer.Deserialize<List<TvMazeShow>>(showsResponse) ?? new List<TvMazeShow>();

                foreach (var tvShow in tvMazeShows)
                {
                    // Fetch cast for each show
                    var castUrl = $"https://api.tvmaze.com/shows/{tvShow.Id}/cast";
                    var castResponse = await httpClient.GetStringAsync(castUrl, cancellationToken);
                    var tvMazeCast = JsonSerializer.Deserialize<List<TvMazeCastMember>>(castResponse) ?? new List<TvMazeCastMember>();

                    var show = new Show
                    {
                        Id = tvShow.Id,
                        Name = tvShow.Name,
                        Cast = tvMazeCast.Select(c => new CastMember
                        {
                            CastMemberId = c.Person.Id,
                            Name = c.Person.Name,
                            Birthday = c.Person.Birthday,
                            ShowId = tvShow.Id
                        }).ToList()
                    };

                    shows.Add(show);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch page {Page}", page);
            }
        }

        int newShows = 0;
        int updatedShows = 0;

        // Save shows to database
        foreach (var show in shows)
        {
            var existingShow = await _context.Shows
                .Include(s => s.Cast)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == show.Id, cancellationToken);

            if (existingShow != null)
            {
                // Update existing show - attach and update
                var showToUpdate = await _context.Shows
                    .Include(s => s.Cast)
                    .FirstOrDefaultAsync(s => s.Id == show.Id, cancellationToken);

                if (showToUpdate != null)
                {
                    showToUpdate.Name = show.Name;

                    // Remove old cast members
                    _context.CastMembers.RemoveRange(showToUpdate.Cast);

                    // Add new cast members
                    foreach (var castMember in show.Cast)
                    {
                        showToUpdate.Cast.Add(castMember);
                    }

                    updatedShows++;
                }
            }
            else
            {
                // Add new show
                await _context.Shows.AddAsync(show, cancellationToken);
                newShows++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear(); // Clear tracking to avoid duplicate key errors
        }

        var result = new ScrapeResult
        {
            TotalScraped = shows.Count,
            NewShows = newShows,
            UpdatedShows = updatedShows,
            StartPage = request.StartPage,
            PageCount = request.PageCount
        };

        _logger.LogInformation(
            "Scrape completed: {TotalScraped} total, {NewShows} new, {UpdatedShows} updated",
            result.TotalScraped, newShows, updatedShows);

        return result;
    }
}
