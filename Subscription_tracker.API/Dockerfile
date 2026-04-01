FROM mcr.microsoft.com/dotnet/sdk:10.0 as build
WORKDIR /src
COPY ["Subscription_tracker.API.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Subscription_tracker.API.dll"]
