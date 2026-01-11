# TVMaze Project Architecture

## Overview

This project is a full-stack application that scrapes, stores, and displays TV show information from the TVMaze API. It consists of a .NET 8 backend API and an Angular frontend, with SQL Server for data persistence.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Angular Frontend                        │
│                   (Port: 4200 - Dev)                        │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │  Show List  │  │ Show Details │  │     Scraper     │   │
│  │  Component  │  │  Component   │  │    Component    │   │
│  └─────────────┘  └──────────────┘  └─────────────────┘   │
│           │               │                   │             │
│           └───────────────┴───────────────────┘             │
│                           │                                 │
│                  ┌────────▼────────┐                       │
│                  │  TVMaze Service  │                       │
│                  └─────────────────┘                       │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP/REST
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    .NET 8 Web API                           │
│                   (Port: 5000/5001)                         │
│  ┌────────────────────────────────────────────────────┐    │
│  │              Controllers Layer                      │    │
│  │  ┌──────────────┐    ┌───────────────────┐        │    │
│  │  │ShowsController│    │ScraperController  │        │    │
│  │  └──────────────┘    └───────────────────┘        │    │
│  └──────────────┬───────────────┬──────────────────────    │
│                 │               │                           │
│  ┌──────────────▼───────────────▼──────────────────────┐   │
│  │            MediatR (CQRS Pattern)                    │   │
│  │  ┌───────────────┐  ┌────────────────┐  ┌────────┐ │   │
│  │  │GetShowsQuery  │  │ScrapeShows     │  │ Cache  │ │   │
│  │  │GetShowById    │  │Command         │  │Service │ │   │
│  │  │GetShowCount   │  │                │  └────────┘ │   │
│  │  └───────────────┘  └────────────────┘             │   │
│  └────────────────────────┬──────────────────────────────  │
│                           │                                 │
│  ┌────────────────────────▼──────────────────────────────┐ │
│  │              Data Layer (EF Core)                     │ │
│  │  ┌──────────────┐    ┌──────────────┐               │ │
│  │  │TvMazeContext │    │ Repositories │               │ │
│  │  └──────────────┘    └──────────────┘               │ │
│  └────────────────────────┬──────────────────────────────┘ │
└───────────────────────────┼────────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────────┐
│                 SQL Server Database                         │
│                    (Port: 1433)                             │
│  ┌──────────┐      ┌───────────┐                          │
│  │  Shows   │ 1──∞ │CastMembers│                          │
│  │  Table   │◄─────┤   Table   │                          │
│  └──────────┘      └───────────┘                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            │
┌───────────────────────────▼────────────────────────────────┐
│              External TVMaze API                            │
│            https://api.tvmaze.com/                          │
└─────────────────────────────────────────────────────────────┘
```

## Technology Stack

### Backend (.NET 8 Web API)
- **Framework**: ASP.NET Core 8.0
- **Database**: 
  - SQLite (Development/Default)
  - SQL Server (Production/Docker)
- **ORM**: Entity Framework Core
- **Architecture Pattern**: CQRS with MediatR
- **Validation**: FluentValidation
- **Resilience**: Polly (HTTP retry policies)
- **Caching**: In-Memory Cache
- **API Documentation**: Swagger/OpenAPI

### Frontend (Angular)
- **Framework**: Angular 21
- **Styling**: Tailwind CSS
- **HTTP Client**: RxJS
- **Routing**: Angular Router

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Database**: Microsoft SQL Server 2022
- **Development**: DevContainer support

## Project Structure

### Backend Structure (`src/TvMaze.Api/`)

```
TvMaze.Api/
├── Application/                    # Business logic layer
│   ├── Behaviors/                  # MediatR pipeline behaviors
│   │   └── ValidationBehavior.cs   # Request validation behavior
│   └── Features/                   # Feature-based organization (CQRS)
│       ├── GetShowById/            # Query: Get single show
│       ├── GetShowCount/           # Query: Get total show count
│       ├── GetShows/               # Query: Get paginated shows
│       └── ScrapeShows/            # Command: Scrape TVMaze API
│
├── Configuration/                  # Configuration models
│   └── CacheSettings.cs
│
├── Controllers/                    # API endpoints
│   ├── ShowsController.cs          # CRUD operations for shows
│   └── ScraperController.cs        # Scraping operations
│
├── Data/                           # Data access layer
│   ├── ITvMazeContext.cs           # DbContext interface
│   └── TvMazeContext.cs            # EF Core DbContext
│
├── Middleware/                     # Custom middleware
│   └── ExceptionHandlingMiddleware.cs
│
├── Models/                         # Domain & data models
│   ├── Show.cs                     # Show entity
│   ├── CastMember.cs               # Cast member entity
│   ├── DTOs/                       # Data transfer objects
│   │   ├── ShowDto.cs
│   │   ├── CastMemberDto.cs
│   │   └── PagedResult.cs
│   ├── Results/                    # Operation results
│   │   └── ScrapeResult.cs
│   └── TvMazeDtos/                 # External API DTOs
│       ├── TvMazeShow.cs
│       ├── TvMazeCastMember.cs
│       └── TvMazePerson.cs
│
├── Services/                       # Application services
│   ├── ICacheService.cs
│   └── MemoryCacheService.cs
│
├── TvMaze.Api.Tests/               # Unit tests
│   └── Application/
│       └── Features/
│
├── Properties/
│   └── launchSettings.json         # Launch configuration
│
├── wwwroot/                        # Static files (Angular build output)
│   └── index.html
│
├── Program.cs                      # Application entry point
├── appsettings.json                # Configuration
└── TvMaze.Api.csproj               # Project file
```

### Frontend Structure (`src/TvMaze.Angular/`)

```
TvMaze.Angular/
├── src/
│   ├── app/
│   │   ├── components/             # UI components
│   │   │   ├── scraper/            # Scraper interface
│   │   │   ├── show-details/       # Show details page
│   │   │   └── show-list/          # Show list page
│   │   │
│   │   ├── models/                 # TypeScript models
│   │   │   ├── show.model.ts
│   │   │   └── scrape-result.model.ts
│   │   │
│   │   ├── services/               # HTTP services
│   │   │   ├── tvmaze.ts           # API service
│   │   │   └── config.ts           # Configuration service
│   │   │
│   │   ├── app.config.ts           # App configuration
│   │   ├── app-routing-module.ts   # Routing configuration
│   │   └── app.ts                  # Root component
│   │
│   ├── assets/                     # Static assets
│   │   ├── config.json             # Development config
│   │   └── config.prod.json        # Production config
│   │
│   ├── index.html                  # Main HTML file
│   ├── main.ts                     # Application bootstrap
│   └── styles.css                  # Global styles
│
├── angular.json                    # Angular CLI configuration
├── package.json                    # NPM dependencies
├── tailwind.config.js              # Tailwind CSS config
└── tsconfig.json                   # TypeScript configuration
```

## Design Patterns & Principles

### 1. CQRS (Command Query Responsibility Segregation)
- **Commands**: Operations that modify state (e.g., `ScrapeShowsCommand`)
- **Queries**: Operations that read data (e.g., `GetShowsQuery`, `GetShowByIdQuery`)
- **Implementation**: MediatR library handles request/response flow

### 2. Repository Pattern
- Abstracted through EF Core `DbContext`
- `ITvMazeContext` interface for testability

### 3. Dependency Injection
- All services registered in `Program.cs`
- Constructor injection throughout the application

### 4. Validation
- FluentValidation for request validation
- `ValidationBehavior` intercepts requests in the MediatR pipeline

### 5. Middleware Pattern
- `ExceptionHandlingMiddleware` for global error handling
- Centralized error responses

### 6. Resilience Patterns
- Polly for HTTP resilience (retry, circuit breaker)
- Exponential backoff for TVMaze API calls

## Data Model

### Show Entity
```csharp
class Show {
    int Id
    string Name
    List<CastMember> Cast
}
```

### CastMember Entity
```csharp
class CastMember {
    int Id
    int ShowId
    string Name
    DateTime Birthday
    Show Show
}
```

**Relationship**: One-to-Many (Show → CastMembers)

## API Endpoints

### Shows Controller (`/api/shows`)
- `GET /api/shows` - Get paginated list of shows
  - Query params: `pageNumber`, `pageSize`, `orderBy`, `searchTerm`
- `GET /api/shows/{id}` - Get show by ID with cast
- `GET /api/shows/count` - Get total show count

### Scraper Controller (`/api/scraper`)
- `POST /api/scraper/scrape` - Trigger TVMaze scraping
  - Query params: `startPage`, `pageCount`

## Caching Strategy

- **Memory Cache**: In-memory caching for frequently accessed data
- **Configurable TTL**:
  - Show by ID: 30 minutes
  - Shows list: 10 minutes
  - Show count: 15 minutes
- **Cache Keys**: Structured to include query parameters

## Error Handling

1. **Validation Errors**: Return 400 Bad Request with validation details
2. **Not Found**: Return 404 with descriptive message
3. **External API Errors**: Retry with Polly, then return 502 Bad Gateway
4. **Unhandled Exceptions**: Global middleware returns 500 with error ID

## Security Considerations

- **CORS**: Configured for cross-origin requests
- **HTTPS**: Redirect in production
- **SQL Injection**: Protected by EF Core parameterization
- **Input Validation**: FluentValidation on all inputs

## Performance Optimizations

1. **Pagination**: Efficient data retrieval with offset/limit
2. **Caching**: Reduced database and API calls
3. **Async/Await**: Non-blocking I/O operations
4. **Database Indexes**: On frequently queried columns
5. **HTTP Client Pooling**: Reused connections to TVMaze API

## Testing Strategy

- **Unit Tests**: Test business logic in isolation
- **Integration Tests**: Test API endpoints with test database
- **Test Project**: `TvMaze.Api.Tests`

## Deployment Architecture

### Development
- SQLite database (file-based)
- Angular dev server (port 4200)
- API (ports 5000/5001)

### Docker Compose
- API container
- SQL Server container
- Angular build served by API (wwwroot)
- Network: Containers communicate via Docker network

## Future Enhancements

1. **Authentication**: JWT-based authentication
2. **Rate Limiting**: Protect API from abuse
3. **Real-time Updates**: SignalR for live scraping status
4. **Advanced Search**: Full-text search with indexes
5. **GraphQL**: Alternative API query language
6. **Microservices**: Split scraper into separate service
7. **Message Queue**: Async scraping with RabbitMQ/Azure Service Bus
8. **Monitoring**: Application Insights or ELK stack
