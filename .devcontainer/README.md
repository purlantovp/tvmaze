# Devcontainer for tvmaze

This folder contains a minimal VS Code Dev Container configuration for a .NET 8 + Angular 21 project. To use it:

1. Open the `tvmaze` folder in VS Code.
2. Run "Remote-Containers: Reopen in Container" from the Command Palette.

The container starts a dev service (based on the .NET 8 devcontainer image) and an `mssql` service for local development. The SQL Server listens on port 1433 and uses the `sa` user with the password `DevPassword!23` by default.
