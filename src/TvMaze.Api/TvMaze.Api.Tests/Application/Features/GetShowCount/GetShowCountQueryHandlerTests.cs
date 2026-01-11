using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TvMaze.Api.Application.Features.GetShowCount;
using TvMaze.Api.Configuration;
using TvMaze.Api.Data;
using TvMaze.Api.Models;
using TvMaze.Api.Services;

namespace TvMaze.Api.Tests.Application.Features.GetShowCount;

public class GetShowCountQueryHandlerTests : IDisposable
{
    private readonly TvMazeContext _context;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<GetShowCountQueryHandler>> _loggerMock;
    private readonly Mock<IOptions<CacheSettings>> _cacheSettingsMock;

    public GetShowCountQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TvMazeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TvMazeContext(options);
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<GetShowCountQueryHandler>>();
        _cacheSettingsMock = new Mock<IOptions<CacheSettings>>();

        _cacheSettingsMock
            .Setup(x => x.Value)
            .Returns(new CacheSettings { ShowCountCacheDurationMinutes = 15 });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_WhenShowsExist_ShouldReturnCorrectCount()
    {
        // Arrange
        await SeedShows(5);
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowCountQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenNoShows_ShouldReturnZero()
    {
        // Arrange
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowCountQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldUseCacheCorrectly()
    {
        // Arrange
        await SeedShows(3);
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowCountQuery();

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            x => x.GetOrCreateValueAsync(
                "shows_count",
                It.IsAny<Func<Task<int>>>(),
                TimeSpan.FromMinutes(15)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheHasValue_ShouldReturnCachedValue()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetOrCreateValueAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<int>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(42);

        var handler = CreateHandler();
        var query = new GetShowCountQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task Handle_WithLargeNumberOfShows_ShouldReturnCorrectCount()
    {
        // Arrange
        await SeedShows(100);
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowCountQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldCallFactoryWhenCacheIsEmpty()
    {
        // Arrange
        await SeedShows(3);

        var factoryCalled = false;
        _cacheServiceMock
            .Setup(x => x.GetOrCreateValueAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<int>>>(),
                It.IsAny<TimeSpan>()))
            .Returns<string, Func<Task<int>>, TimeSpan>(async (key, factory, expiration) =>
            {
                factoryCalled = true;
                return await factory();
            });

        var handler = CreateHandler();
        var query = new GetShowCountQuery();

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        factoryCalled.Should().BeTrue();
    }

    private GetShowCountQueryHandler CreateHandler()
    {
        return new GetShowCountQueryHandler(
            _context,
            _cacheServiceMock.Object,
            _cacheSettingsMock.Object,
            _loggerMock.Object);
    }

    private void SetupCacheServiceToCallFactory()
    {
        _cacheServiceMock
            .Setup(x => x.GetOrCreateValueAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<int>>>(),
                It.IsAny<TimeSpan>()))
            .Returns<string, Func<Task<int>>, TimeSpan>((key, factory, expiration) => factory());
    }

    private async Task SeedShows(int count)
    {
        var shows = Enumerable.Range(1, count).Select(i => new Show
        {
            Id = i,
            Name = $"Show {i}",
            Cast = new List<CastMember>()
        }).ToList();

        await _context.Shows.AddRangeAsync(shows);
        await _context.SaveChangesAsync();
    }
}
