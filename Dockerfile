# Use the official .NET SDK image to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore as distinct layers
COPY . .
WORKDIR /src/EvaluadorML.Web
RUN dotnet restore

# Build and publish the app
RUN dotnet publish -c Release -o /app --no-restore

# Use the official ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

# Copy the SQLite database if present
# COPY EvaluadorML.Web/app.db ./

# Expose port 8080 for Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRY
