# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KitchenManagement.csproj", "."]
RUN dotnet restore "./KitchenManagement.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "KitchenManagement.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "KitchenManagement.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render requires running on port 8080 or binding to $PORT
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "KitchenManagement.dll"]
