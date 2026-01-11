using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TvMaze.Api.Application.Features.ScrapeShows;
using TvMaze.Api.Data;
using TvMaze.Api.Models;
using TvMaze.Api.Models.TvMazeDtos;

namespace TvMaze.Api.Tests.Application.Features.ScrapeShows;

public class ScrapeShowsCommandHandlerTests : IDisposable
{
    private readonly TvMazeContext _context;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ScrapeShowsCommandHandler>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public ScrapeShowsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TvMazeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TvMazeContext(options);
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ScrapeShowsCommandHandler>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_WhenNewShowsAreScraped_ShouldAddThemToDatabase()
    {
        // Arrange
        var shows = new List<TvMazeShow>
        {
            new() { Id = 1, Name = "Breaking Bad" },
            new() { Id = 2, Name = "Game of Thrones" }
        };

        var cast1 = new List<TvMazeCastMember>
        {
            new() { Person = new TvMazePerson { Id = 101, Name = "Bryan Cranston", Birthday = new DateTime(1956, 3, 7) } },
            new() { Person = new TvMazePerson { Id = 102, Name = "Aaron Paul", Birthday = new DateTime(1979, 8, 27) } }
        };

        var cast2 = new List<TvMazeCastMember>
        {
            new() { Person = new TvMazePerson { Id = 201, Name = "Emilia Clarke", Birthday = new DateTime(1986, 10, 23) } }
        };

        SetupHttpClientMock(shows, new Dictionary<int, List<TvMazeCastMember>>
        {
            { 1, cast1 },
            { 2, cast2 }
        });

        var handler = CreateHandler();
        var command = new ScrapeShowsCommand(StartPage: 0, PageCount: 1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalScraped.Should().Be(2);
        result.NewShows.Should().Be(2);
        result.UpdatedShows.Should().Be(0);
        result.StartPage.Should().Be(0);
        result.PageCount.Should().Be(1);

        var dbShows = await _context.Shows.Include(s => s.Cast).ToListAsync();
        dbShows.Should().HaveCount(2);

        var breakingBad = dbShows.First(s => s.Id == 1);
        breakingBad.Name.Should().Be("Breaking Bad");
        breakingBad.Cast.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenShowAlreadyExists_ShouldUpdateIt()
    {
        // Arrange
        var existingShow = new Show
        {
            Id = 1,
            Name = "Old Name",
            Cast = new List<CastMember>
            {
                new() { CastMemberId = 999, Name = "Old Actor", ShowId = 1 }
            }
        };
        await _context.Shows.AddAsync(existingShow);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var shows = new List<TvMazeShow>
        {
            new() { Id = 1, Name = "Breaking Bad" }
        };

        var cast = new List<TvMazeCastMember>
        {
            new() { Person = new TvMazePerson { Id = 101, Name = "Bryan Cranston", Birthday = new DateTime(1956, 3, 7) } }
        };

        SetupHttpClientMock(shows, new Dictionary<int, List<TvMazeCastMember>> { { 1, cast } });

        var handler = CreateHandler();
        var command = new ScrapeShowsCommand(StartPage: 0, PageCount: 1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalScraped.Should().Be(1);
        result.NewShows.Should().Be(0);
        result.UpdatedShows.Should().Be(1);

        var dbShow = await _context.Shows.Include(s => s.Cast).FirstAsync(s => s.Id == 1);
        dbShow.Name.Should().Be("Breaking Bad");
        dbShow.Cast.Should().HaveCount(1);
        dbShow.Cast.First().Name.Should().Be("Bryan Cranston");
    }

    [Fact]
    public async Task Handle_WhenHttpRequestFails_ShouldContinueWithNextPage()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.tvmaze.com/")
        };

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("TvMazeApi"))
            .Returns(httpClient);

        var handler = CreateHandler();
        var command = new ScrapeShowsCommand(StartPage: 0, PageCount: 2);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalScraped.Should().Be(0);
        result.NewShows.Should().Be(0);
        result.UpdatedShows.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenNoShowsReturned_ShouldReturnEmptyResult()
    {
        // Arrange
        SetupHttpClientMock(new List<TvMazeShow>(), new Dictionary<int, List<TvMazeCastMember>>());

        var handler = CreateHandler();
        var command = new ScrapeShowsCommand(StartPage: 0, PageCount: 1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalScraped.Should().Be(0);
        result.NewShows.Should().Be(0);
        result.UpdatedShows.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldScrapeMultiplePages()
    {
        // Arrange
        var page0Shows = new List<TvMazeShow> { new() { Id = 1, Name = "Show 1" } };
        var page1Shows = new List<TvMazeShow> { new() { Id = 2, Name = "Show 2" } };

        var responses = new Dictionary<string, string>
        {
            { "shows?page=0", JsonSerializer.Serialize(page0Shows) },
            { "shows?page=1", JsonSerializer.Serialize(page1Shows) },
            { "shows/1/cast", JsonSerializer.Serialize(new List<TvMazeCastMember>()) },
            { "shows/2/cast", JsonSerializer.Serialize(new List<TvMazeCastMember>()) }
        };

        SetupHttpClientMockWithResponses(responses);

        var handler = CreateHandler();
        var command = new ScrapeShowsCommand(StartPage: 0, PageCount: 2);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalScraped.Should().Be(2);
        result.NewShows.Should().Be(2);
        result.StartPage.Should().Be(0);
        result.PageCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithCastMembersBirthday_ShouldStoreBirthdayCorrectly()
    {
        // Arrange
        var birthday = new DateTime(1956, 3, 7);
        var shows = new List<TvMazeShow>
        {
            new() { Id = 1, Name = "Breaking Bad" }
        };

        var cast = new List<TvMazeCastMember>
        {
            new() { Person = new TvMazePerson { Id = 101, Name = "Bryan Cranston", Birthday = birthday } }
        };

        SetupHttpClientMock(shows, new Dictionary<int, List<TvMazeCastMember>> { { 1, cast } });

        var handler = CreateHandler();
        var command = new ScrapeShowsCommand(StartPage: 0, PageCount: 1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var castMember = await _context.CastMembers.FirstAsync();
        castMember.Birthday.Should().Be(birthday);
    }

    [Fact]
    public async Task Handle_WithNullBirthday_ShouldStoreNullBirthday()
    {
        // Arrange
        var shows = new List<TvMazeShow>
        {
            new() { Id = 1, Name = "Test Show" }
        };

        var cast = new List<TvMazeCastMember>
        {
            new() { Person = new TvMazePerson { Id = 101, Name = "Unknown Actor", Birthday = null } }
        };

        SetupHttpClientMock(shows, new Dictionary<int, List<TvMazeCastMember>> { { 1, cast } });

        var handler = CreateHandler();
        var command = new ScrapeShowsCommand(StartPage: 0, PageCount: 1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var castMember = await _context.CastMembers.FirstAsync();
        castMember.Birthday.Should().BeNull();
    }

    private ScrapeShowsCommandHandler CreateHandler()
    {
        return new ScrapeShowsCommandHandler(
            _httpClientFactoryMock.Object,
            _context,
            _loggerMock.Object);
    }

    private void SetupHttpClientMock(List<TvMazeShow> shows, Dictionary<int, List<TvMazeCastMember>> castByShowId)
    {
        var responses = new Dictionary<string, string>
        {
            { "shows?page=0", JsonSerializer.Serialize(shows) }
        };

        foreach (var (showId, cast) in castByShowId)
        {
            responses[$"shows/{showId}/cast"] = JsonSerializer.Serialize(cast);
        }

        SetupHttpClientMockWithResponses(responses);
    }

    private void SetupHttpClientMockWithResponses(Dictionary<string, string> responses)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                var path = request.RequestUri?.PathAndQuery.TrimStart('/') ?? "";
                
                if (responses.TryGetValue(path, out var content))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(content)
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.tvmaze.com/")
        };

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("TvMazeApi"))
            .Returns(httpClient);
    }
}
