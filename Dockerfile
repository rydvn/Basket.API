#Build Stage
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

WORKDIR /workdir

COPY ["./src/Basket API/Basket API.csproj", "./src/Basket API/"]

RUN dotnet restore "./src/Basket API/Basket API.csproj" -s https://api.nuget.org/v3/index.json -s http://dsget.azurewebsites.net/nuget

COPY ["./src/Basket API", "./src/Basket API/"]

RUN dotnet publish "./src/Basket API/Basket API.csproj" -c Release -o /publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
COPY --from=build-env /publish /publish
WORKDIR /publish
EXPOSE 5000
ENTRYPOINT ["dotnet", "Basket API.dll"]