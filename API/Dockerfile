# Runtime image for ASP.NET app (standard Debian-based .NET runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["API.csproj", "./"]
RUN dotnet restore "API.csproj"
COPY . .
RUN dotnet publish "API.csproj" -c Release -o /app/publish

# Final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish ./
# Listen on port 5000
ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "API.dll"]
