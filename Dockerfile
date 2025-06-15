# Stage 1: Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

# Stage 2: Build and publish app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first
COPY ["UserManagementAPI/UserManagementAPI.csproj", "UserManagementAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]

# Restore dependencies
RUN dotnet restore "UserManagementAPI/UserManagementAPI.csproj"

# Copy remaining source code
COPY . .

WORKDIR "/src/UserManagementAPI"
RUN dotnet publish "UserManagementAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5003
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "UserManagementAPI.dll"]
