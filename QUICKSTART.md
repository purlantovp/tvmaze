# TVMaze Application - Quick Start Guide

This guide will help you get the TVMaze application up and running quickly.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js (v18+)** and **npm (v11+)** - [Download](https://nodejs.org/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop) (optional, for containerized setup)
- **SQL Server** or use SQLite (default)
- **Git** - [Download](https://git-scm.com/)

## Quick Start Options

Choose one of the following methods to run the application:

### Option 1: Docker Compose (Recommended for Production-like Environment)

This method runs both the API and SQL Server in containers.

1. **Navigate to the project root**
   ```bash
   cd tvmaze
   ```

2. **Start the services**
   ```bash
   docker-compose up -d
   ```

3. **Verify services are running**
   ```bash
   docker-compose ps
   ```
   You should see `mssql` and `devcontainer` services running.

4. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Database: localhost:1433 (SA password: `DevPassword!23`)

### Option 2: Local Development (Recommended for Development)

This method runs the API and Angular frontend separately on your local machine.

#### Step 1: Set up and run the Backend API

1. **Navigate to the API project**
   ```bash
   cd src/TvMaze.Api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run database migrations** (creates SQLite database)
   ```bash
   dotnet ef database update
   ```
   > Note: The database will be created automatically on first run if using the default SQLite configuration.

4. **Run the API**
   ```bash
   dotnet run
   ```

5. **Verify the API is running**
   - API: https://localhost:5001 or http://localhost:5000
   - Swagger UI: https://localhost:5001/swagger

#### Step 2: Set up and run the Angular Frontend

1. **Open a new terminal** and navigate to the Angular project
   ```bash
   cd src/TvMaze.Angular
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Configure API endpoint** (if needed)
   
   Edit `src/assets/config.json` to point to your API:
   ```json
   {
     "apiUrl": "http://localhost:5000/api"
   }
   ```

4. **Run the Angular development server**
   ```bash
   npm start
   ```
   or
   ```bash
   ng serve
   ```

5. **Access the application**
   - Open your browser to: http://localhost:4200

### Option 3: Run Angular from API (Production-like)

This method serves the Angular app from the .NET API's wwwroot folder.

1. **Build the Angular application**
   ```bash
   cd src/TvMaze.Angular
   npm install
   npm run build
   ```

2. **Copy build output to API wwwroot**
   
   The build output in `dist/tv-maze.angular/browser` should be copied to `src/TvMaze.Api/wwwroot/`

3. **Run the API**
   ```bash
   cd src/TvMaze.Api
   dotnet run
   ```

4. **Access the application**
   - Open your browser to: http://localhost:5000

## First Steps After Installation

### 1. Scrape TVMaze Data

Before you can view shows, you need to scrape data from the TVMaze API.

**Using the UI:**
1. Navigate to the Scraper page in the application
2. Set the start page (e.g., 0)
3. Set the number of pages to scrape (e.g., 10)
4. Click "Start Scraping"

**Using Swagger:**
1. Go to http://localhost:5000/swagger
2. Find the `POST /api/scraper/scrape` endpoint
3. Click "Try it out"
4. Set parameters:
   - `startPage`: 0
   - `pageCount`: 10
5. Click "Execute"

**Using cURL:**
```bash
curl -X POST "http://localhost:5000/api/scraper/scrape?startPage=0&pageCount=10"
```

### 2. View Shows

Once data is scraped:

1. Navigate to the Shows list page
2. Browse paginated results
3. Click on a show to view details including cast members
4. Use search functionality to find specific shows or actors

### 3. Explore the API

Visit the Swagger documentation to explore all available endpoints:
- http://localhost:5000/swagger (or https://localhost:5001/swagger)

## Configuration

### Backend Configuration

Configuration is managed in `src/TvMaze.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tvmaze.db"
  },
  "TvMazeSettings": {
    "BaseUrl": "https://api.tvmaze.com/"
  },
  "CacheSettings": {
    "ShowByIdCacheDurationMinutes": 30,
    "ShowsListCacheDurationMinutes": 10,
    "ShowCountCacheDurationMinutes": 15,
    "EnableCaching": true
  }
}
```

**To use SQL Server instead of SQLite:**
1. Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost,1433;Database=TvMaze;User Id=sa;Password=DevPassword!23;TrustServerCertificate=True;"
   }
   ```
2. Update `Program.cs` to use SQL Server:
   ```csharp
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
   ```

### Frontend Configuration

Configuration is managed in `src/TvMaze.Angular/src/assets/config.json`:

```json
{
  "apiUrl": "http://localhost:5000/api"
}
```

For production, use `config.prod.json` and update the build configuration.

## Common Tasks

### Running Tests

**Backend tests:**
```bash
cd src/TvMaze.Api.Tests
dotnet test
```

**Frontend tests:**
```bash
cd src/TvMaze.Angular
npm test
```

### Building for Production

**Backend:**
```bash
cd src/TvMaze.Api
dotnet publish -c Release -o ./publish
```

**Frontend:**
```bash
cd src/TvMaze.Angular
npm run build
```

### Checking Logs

**Docker logs:**
```bash
docker-compose logs -f
```

**API logs:**
Check the console output where `dotnet run` is executed.

### Stopping the Application

**Docker:**
```bash
docker-compose down
```

**Local Development:**
- Press `Ctrl+C` in each terminal running the API and Angular dev server

## Troubleshooting

### Database Connection Issues

**SQLite:**
- Ensure the `tvmaze.db` file can be created in the API project directory
- Check file permissions

**SQL Server:**
- Verify SQL Server is running: `docker ps`
- Test connection: `sqlcmd -S localhost,1433 -U sa -P 'DevPassword!23'`
- Check firewall settings

### API Not Accessible

- Verify the API is running on the expected port
- Check `Properties/launchSettings.json` for port configuration
- Ensure no other service is using ports 5000/5001

### Angular Build Errors

- Delete `node_modules` and reinstall:
  ```bash
  rm -rf node_modules
  npm install
  ```
- Clear npm cache:
  ```bash
  npm cache clean --force
  ```

### TVMaze API Rate Limiting

- The TVMaze API may rate-limit requests
- Reduce `pageCount` when scraping
- Wait before retrying if you receive 429 errors
- The API includes retry logic with exponential backoff

### CORS Issues

- Ensure the API CORS policy allows your frontend origin
- Check `Program.cs` CORS configuration
- Verify the Angular app is making requests to the correct API URL

## Development Tips

### Hot Reload

- **Backend**: Use `dotnet watch run` for automatic recompilation
- **Frontend**: `ng serve` includes hot reload by default

### Debug Mode

**Backend (Visual Studio Code):**
1. Open the project in VS Code
2. Press F5 or use the Debug panel
3. Select ".NET Core Launch (web)"

**Frontend:**
1. Use browser developer tools
2. Angular DevTools extension for Chrome/Firefox

### Database Management

**View SQLite database:**
- Use [DB Browser for SQLite](https://sqlitebrowser.org/)
- VS Code extension: SQLite Viewer

**View SQL Server database:**
- Use SQL Server Management Studio (SSMS)
- Azure Data Studio
- VS Code extension: SQL Server (mssql)

## Next Steps

- Read [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture information
- Explore the codebase starting with [Program.cs](src/TvMaze.Api/Program.cs)
- Review API documentation in Swagger
- Customize the application to your needs

## Getting Help

- Check the Swagger documentation for API details
- Review the code comments and XML documentation
- Consult the [TVMaze API documentation](https://www.tvmaze.com/api)

## Useful Commands Reference

```bash
# Backend
dotnet restore              # Restore dependencies
dotnet build               # Build the project
dotnet run                 # Run the application
dotnet watch run           # Run with hot reload
dotnet test                # Run tests
dotnet ef migrations add   # Add migration
dotnet ef database update  # Update database

# Frontend
npm install                # Install dependencies
npm start                  # Start dev server
npm run build              # Production build
npm test                   # Run tests
ng generate component      # Generate component
ng generate service        # Generate service

# Docker
docker-compose up -d       # Start services
docker-compose down        # Stop services
docker-compose ps          # List services
docker-compose logs -f     # View logs
docker-compose restart     # Restart services
```

## Quick Reference URLs

- **Local API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Angular App**: http://localhost:4200
- **SQL Server**: localhost:1433
- **TVMaze API**: https://api.tvmaze.com/

---

Happy coding! 🚀
