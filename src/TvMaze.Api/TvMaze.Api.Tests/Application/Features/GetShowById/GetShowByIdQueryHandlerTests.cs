using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TvMaze.Api.Application.Features.GetShowById;
using TvMaze.Api.Configuration;
using TvMaze.Api.Data;
using TvMaze.Api.Models;
using TvMaze.Api.Models.DTOs;
using TvMaze.Api.Services;

namespace TvMaze.Api.Tests.Application.Features.GetShowById;

public class GetShowByIdQueryHandlerTests : IDisposable
{
    private readonly TvMazeContext _context;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<GetShowByIdQueryHandler>> _loggerMock;
    private readonly Mock<IOptions<CacheSettings>> _cacheSettingsMock;

    public GetShowByIdQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TvMazeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TvMazeContext(options);
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<GetShowByIdQueryHandler>>();
        _cacheSettingsMock = new Mock<IOptions<CacheSettings>>();

        _cacheSettingsMock
            .Setup(x => x.Value)
            .Returns(new CacheSettings { ShowByIdCacheDurationMinutes = 10 });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_WhenShowExists_ShouldReturnShowDto()
    {
        // Arrange
        var show = new Show
        {
            Id = 1,
            Name = "Breaking Bad",
            Cast = new List<CastMember>
            {
                new() { CastMemberId = 101, Name = "Bryan Cranston", Birthday = new DateTime(1956, 3, 7), ShowId = 1 },
                new() { CastMemberId = 102, Name = "Aaron Paul", Birthday = new DateTime(1979, 8, 27), ShowId = 1 }
            }
        };

        await _context.Shows.AddAsync(show);
        await _context.SaveChangesAsync();

        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowByIdQuery(1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Breaking Bad");
        result.Cast.Should().HaveCount(2);
        result.Cast.First().Name.Should().Be("Bryan Cranston");
    }

    [Fact]
    public async Task Handle_WhenShowDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowByIdQuery(999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
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
                new() { CastMemberId = 102, Name = "Younger Actor", Birthday = new DateTime(1990, 1, 1), ShowId = 1 },
                new() { CastMemberId = 101, Name = "Older Actor", Birthday = new DateTime(1950, 1, 1), ShowId = 1 },
                new() { CastMemberId = 103, Name = "Middle Actor", Birthday = new DateTime(1970, 1, 1), ShowId = 1 }
            }
        };

        await _context.Shows.AddAsync(show);
        await _context.SaveChangesAsync();

        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowByIdQuery(1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Cast.Should().HaveCount(3);
        result.Cast[0].Name.Should().Be("Older Actor");
        result.Cast[1].Name.Should().Be("Middle Actor");
        result.Cast[2].Name.Should().Be("Younger Actor");
    }

    [Fact]
    public async Task Handle_ShouldUseCacheCorrectly()
    {
        // Arrange
        var show = new Show
        {
            Id = 1,
            Name = "Breaking Bad",
            Cast = new List<CastMember>()
        };

        await _context.Shows.AddAsync(show);
        await _context.SaveChangesAsync();

        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowByIdQuery(1);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(
            x => x.GetOrCreateAsync(
                "show_1",
                It.IsAny<Func<Task<ShowDto?>>>(),
                TimeSpan.FromMinutes(10)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheHasValue_ShouldReturnCachedValue()
    {
        // Arrange
        var cachedDto = new ShowDto
        {
            Id = 1,
            Name = "Cached Show",
            Cast = new List<CastMemberDto>()
        };

        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<ShowDto?>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(cachedDto);

        var handler = CreateHandler();
        var query = new GetShowByIdQuery(1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(cachedDto);
    }

    [Fact]
    public async Task Handle_ShouldHandleCastMembersWithNullBirthday()
    {
        // Arrange
        var show = new Show
        {
            Id = 1,
            Name = "Test Show",
            Cast = new List<CastMember>
            {
                new() { CastMemberId = 101, Name = "Known Birthday", Birthday = new DateTime(1980, 1, 1), ShowId = 1 },
                new() { CastMemberId = 102, Name = "Unknown Birthday", Birthday = null, ShowId = 1 }
            }
        };

        await _context.Shows.AddAsync(show);
        await _context.SaveChangesAsync();

        SetupCacheServiceToCallFactory();

        var handler = CreateHandler();
        var query = new GetShowByIdQuery(1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Cast.Should().HaveCount(2);
        result.Cast.Should().Contain(c => c.Name == "Unknown Birthday" && c.Birthday == null);
        result.Cast.Should().Contain(c => c.Name == "Known Birthday" && c.Birthday == new DateTime(1980, 1, 1));
    }

    private GetShowByIdQueryHandler CreateHandler()
    {
        return new GetShowByIdQueryHandler(
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
                It.IsAny<Func<Task<ShowDto?>>>(),
                It.IsAny<TimeSpan>()))
            .Returns<string, Func<Task<ShowDto?>>, TimeSpan>((key, factory, expiration) => factory());
    }
}
