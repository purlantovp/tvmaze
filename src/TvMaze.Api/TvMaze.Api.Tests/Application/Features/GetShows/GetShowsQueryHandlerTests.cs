using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TvMaze.Api.Application.Features.GetShows;
using TvMaze.Api.Configuration;
using TvMaze.Api.Data;
using TvMaze.Api.Models;
using TvMaze.Api.Models.DTOs;
using TvMaze.Api.Services;

namespace TvMaze.Api.Tests.Application.Features.GetShows;

public class GetShowsQueryHandlerTests : IDisposable
{
    private readonly TvMazeContext _context;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<GetShowsQueryHandler>> _loggerMock;
    private readonly Mock<IOptions<CacheSettings>> _cacheSettingsMock;

    public GetShowsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TvMazeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TvMazeContext(options);
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<GetShowsQueryHandler>>();
        _cacheSettingsMock = new Mock<IOptions<CacheSettings>>();

        _cacheSettingsMock
            .Setup(x => x.Value)
            .Returns(new CacheSettings { ShowsListCacheDurationMinutes = 5 });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResults()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 2, OrderBy: null, SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnSecondPage()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 2, PageSize: 2, OrderBy: null, SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(2);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterByShowName()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: "Breaking");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Breaking Bad");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterByCastName()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: "Cranston");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Breaking Bad");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldBeCaseInsensitive()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: "BREAKING");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Breaking Bad");
    }

    [Fact]
    public async Task Handle_WithOrderByName_ShouldSortAscending()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: "name", SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.Items[0].Name.Should().Be("Breaking Bad");
        result.Items[1].Name.Should().Be("Friends");
        result.Items[2].Name.Should().Be("Game of Thrones");
        result.Items[3].Name.Should().Be("Stranger Things");
        result.Items[4].Name.Should().Be("The Office");
    }

    [Fact]
    public async Task Handle_WithOrderByNameDesc_ShouldSortDescending()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: "name_desc", SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.Items[0].Name.Should().Be("The Office");
        result.Items[1].Name.Should().Be("Stranger Things");
        result.Items[2].Name.Should().Be("Game of Thrones");
        result.Items[3].Name.Should().Be("Friends");
        result.Items[4].Name.Should().Be("Breaking Bad");
    }

    [Fact]
    public async Task Handle_WithDefaultOrdering_ShouldSortById()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.Items[0].Id.Should().Be(1);
        result.Items[1].Id.Should().Be(2);
        result.Items[2].Id.Should().Be(3);
        result.Items[3].Id.Should().Be(4);
        result.Items[4].Id.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldOrderCastByBirthday()
    {
        // Arrange
        var show = new Show
        {
            Id = 1,
            Name = "Test Show",
            Cast = new List<CastMember>
            {
                new() { CastMemberId = 102, Name = "Younger", Birthday = new DateTime(1990, 1, 1), ShowId = 1 },
                new() { CastMemberId = 101, Name = "Older", Birthday = new DateTime(1950, 1, 1), ShowId = 1 }
            }
        };

        await _context.Shows.AddAsync(show);
        await _context.SaveChangesAsync();

        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Cast[0].Name.Should().Be("Older");
        result.Items[0].Cast[1].Name.Should().Be("Younger");
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalPagesCorrectly()
    {
        // Arrange
        await SeedShows(); // 5 shows
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();

        // Act & Assert
        var result1 = await handler.Handle(new GetShowsQuery(1, 2, null, null), CancellationToken.None);
        result1.TotalPages.Should().Be(3); // 5 / 2 = 2.5 -> 3

        var result2 = await handler.Handle(new GetShowsQuery(1, 3, null, null), CancellationToken.None);
        result2.TotalPages.Should().Be(2); // 5 / 3 = 1.67 -> 2

        var result3 = await handler.Handle(new GetShowsQuery(1, 5, null, null), CancellationToken.None);
        result3.TotalPages.Should().Be(1); // 5 / 5 = 1
    }

    [Fact]
    public async Task Handle_WhenNoShows_ShouldReturnEmptyResult()
    {
        // Arrange
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldUseCacheCorrectly()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: "name", SearchTerm: "test");

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            x => x.GetOrCreateAsync(
                "shows_page_1_10_name_test",
                It.IsAny<Func<Task<PagedResult<ShowDto>?>>>(),
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullSearchTerm_ShouldUseCacheKeyWithNone()
    {
        // Arrange
        await SeedShows();
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: null);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            x => x.GetOrCreateAsync(
                "shows_page_1_10_default_none",
                It.IsAny<Func<Task<PagedResult<ShowDto>?>>>(),
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheHasValue_ShouldReturnCachedValue()
    {
        // Arrange
        var cachedResult = new PagedResult<ShowDto>
        {
            Items = new List<ShowDto> { new() { Id = 999, Name = "Cached Show", Cast = new List<CastMemberDto>() } },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1,
            TotalPages = 1
        };

        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PagedResult<ShowDto>?>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(cachedResult);

        var handler = CreateHandler();
        var query = new GetShowsQuery(PageNumber: 1, PageSize: 10, OrderBy: null, SearchTerm: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(cachedResult);
    }

    private GetShowsQueryHandler CreateHandler()
    {
        return new GetShowsQueryHandler(
            _context,
            _cacheServiceMock.Object,
            _cacheSettingsMock.Object,
            _loggerMock.Object);
    }

    private void SetupCacheServiceToCallFactory()
    {
        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PagedResult<ShowDto>?>>>(),
                It.IsAny<TimeSpan>()))
            .Returns<string, Func<Task<PagedResult<ShowDto>?>>, TimeSpan>((key, factory, expiration) => factory());
    }

    private async Task SeedShows()
    {
        var shows = new List<Show>
        {
            new()
            {
                Id = 1,
                Name = "Breaking Bad",
                Cast = new List<CastMember>
                {
                    new() { CastMemberId = 101, Name = "Bryan Cranston", Birthday = new DateTime(1956, 3, 7), ShowId = 1 }
                }
            },
            new()
            {
                Id = 2,
                Name = "Game of Thrones",
                Cast = new List<CastMember>()
            },
            new()
            {
                Id = 3,
                Name = "The Office",
                Cast = new List<CastMember>()
            },
            new()
            {
                Id = 4,
                Name = "Friends",
                Cast = new List<CastMember>()
            },
            new()
            {
                Id = 5,
                Name = "Stranger Things",
                Cast = new List<CastMember>()
            }
        };

        await _context.Shows.AddRangeAsync(shows);
        await _context.SaveChangesAsync();
    }
}
