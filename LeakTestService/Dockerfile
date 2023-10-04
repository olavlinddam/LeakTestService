# Using the official .net 7.0 image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
# Ports to expose
EXPOSE 80

# Using the official .net 7.0 sdk image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["LeakTestService/LeakTestService.csproj", "LeakTestService/"]
RUN dotnet restore "LeakTestService/LeakTestService.csproj"
COPY . .
WORKDIR "/src/LeakTestService"

FROM build AS publish
RUN dotnet publish "LeakTestService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeakTestService.dll"]

