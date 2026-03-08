FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TrainingLog/TrainingLog.csproj TrainingLog/
COPY TrainingLog/packages.lock.json TrainingLog/
RUN dotnet restore TrainingLog/TrainingLog.csproj --locked-mode
COPY TrainingLog/ TrainingLog/
RUN dotnet publish TrainingLog/TrainingLog.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TrainingLog.dll"]
