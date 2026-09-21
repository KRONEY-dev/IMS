# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AccountsService/AccountsService.API/AccountsService.API.csproj", "AccountsService/AccountsService.API/"]
COPY ["AccountsService/AccountsService.Application/AccountsService.Application.csproj", "AccountsService/AccountsService.Application/"]
COPY ["AccountsService/AccountsService.Domain/AccountsService.Domain.csproj", "AccountsService/AccountsService.Domain/"]
COPY ["AccountsService/AccountsService.Infrastructure/AccountsService.Infrastructure.csproj", "AccountsService/AccountsService.Infrastructure/"]
COPY ["Shared.Kernel.AspNetCore/Shared.Kernel.AspNetCore.csproj", "Shared.Kernel.AspNetCore/"]
COPY ["Shared.Kernel/Shared.Kernel.csproj", "Shared.Kernel/"]
COPY ["Shared.Kernel.EntityFrameworkCore/Shared.Kernel.EntityFrameworkCore.csproj", "Shared.Kernel.EntityFrameworkCore/"]
RUN dotnet restore "./AccountsService/AccountsService.API/AccountsService.API.csproj"
COPY . .
WORKDIR "/src/AccountsService/AccountsService.API"
RUN dotnet build "./AccountsService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AccountsService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountsService.API.dll"]