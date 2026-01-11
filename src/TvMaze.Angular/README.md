# TVMaze Angular Application

Angular 21 application for browsing TV shows and cast information.

## Prerequisites

- Node.js v20.19+ or v22.12+ (v24.12.0 installed in devcontainer)
- Angular CLI 21 (installed globally in devcontainer)
- .NET 8 API running at `http://localhost:5002/api` (configurable)

## Getting Started

### Install Dependencies

```bash
npm install
```

### Development Server

Run the development server:

```bash
ng serve
```

Or using npm script:

```bash
npm start
```

Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

### Build

Build the project:

```bash
ng build
```

Build for production:

```bash
ng build --configuration production
```

The build artifacts will be stored in the `dist/` directory.

### Watch Mode

Run the build in watch mode for development:

```bash
npm run watch
```

## Configuration

The API URL is configured in `/src/assets/config.json`:

```json
{
  "apiUrl": "http://localhost:5002/api"
}
```

For production, use `/src/assets/config.prod.json` or replace `config.json` during deployment.

## Project Structure

```
src/
├── app/
│   ├── components/
│   │   ├── show-list/          # TV shows list with pagination and search
│   │   └── show-details/       # Cast details for a specific show
│   ├── models/
│   │   └── show.model.ts       # TypeScript interfaces
│   ├── services/
│   │   ├── config.ts           # Configuration service
│   │   └── tvmaze.ts           # API service
│   ├── app-routing-module.ts   # Route configuration
│   ├── app-module.ts           # Main module
│   └── app.ts                  # Root component
├── assets/
│   ├── config.json             # Development configuration
│   └── config.prod.json        # Production configuration
└── styles.css                  # Global styles with Tailwind
```

## Features

- ✅ Paginated list of TV shows
- ✅ Sort shows by name (A-Z, Z-A)
- ✅ Search shows by name or cast member
- ✅ View detailed cast information
- ✅ Responsive design with Tailwind CSS
- ✅ Loading states and error handling

## Using ng Commands in DevContainer

The Angular CLI (`ng`) command is available globally in the devcontainer. The PATH is automatically configured in:

- `~/.bashrc`
- `~/.zshrc`
- VSCode integrated terminal

You can run any Angular CLI command directly:

```bash
ng serve
ng build
ng generate component my-component
ng test
```

## Technology Stack

- **Angular**: 21.0.0
- **TypeScript**: 5.9.2
- **Tailwind CSS**: 3.4.19
- **RxJS**: 7.8.0
- **Node.js**: 24.12.0
- **npm**: 11.6.2
