FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AdventureGraphQL.Api/AdventureGraphQL.Api.csproj", "AdventureGraphQL.Api/"]
RUN dotnet restore "AdventureGraphQL.Api/AdventureGraphQL.Api.csproj"

COPY . .
WORKDIR "/src/AdventureGraphQL.Api"
RUN dotnet publish "AdventureGraphQL.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AdventureGraphQL.Api.dll"]
