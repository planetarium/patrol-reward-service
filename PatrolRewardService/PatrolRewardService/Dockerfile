﻿FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PatrolRewardService/PatrolRewardService/PatrolRewardService.csproj", "PatrolRewardService/"]
RUN dotnet restore "PatrolRewardService"
COPY . .
WORKDIR "/src/PatrolRewardService/PatrolRewardService"
RUN dotnet build "PatrolRewardService.csproj" -c Release -o /app/build

FROM build AS publish
ARG COMMIT
RUN dotnet publish "PatrolRewardService.csproj" -c Release -o /app/publish --version-suffix $COMMIT

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PatrolRewardService.dll"]
