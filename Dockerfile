# Remove existing content and replace with multi-stage Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UserManagementAPI/UserManagementAPI.csproj", "UserManagementAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
RUN dotnet restore "UserManagementAPI/UserManagementAPI.csproj"
COPY . .
WORKDIR "/src/UserManagementAPI"
RUN dotnet publish "UserManagementAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Ensure the app listens on port 80
ENV ASPNETCORE_URLS=http://0.0.0.0:5003
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "UserManagementAPI.dll"]
RUN apt-get update && apt-get install -y libkrb5-3


