# Runtime image for the MVC web application.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Build and publish the MVC application and its Contracts project.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["RaceDay.Mvc/RaceDay.Mvc.csproj", "RaceDay.Mvc/"]
COPY ["RaceDay.Contracts/RaceDay.Contracts.csproj", "RaceDay.Contracts/"]
RUN dotnet restore "RaceDay.Mvc/RaceDay.Mvc.csproj"

COPY . .
WORKDIR /src/RaceDay.Mvc
RUN dotnet publish "RaceDay.Mvc.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    /p:UseAppHost=false

# Run the published MVC app on the container's HTTP port.
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
