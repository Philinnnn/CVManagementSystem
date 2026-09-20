FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CVManagementSystem/*.csproj ./CVManagementSystem/
RUN dotnet restore ./CVManagementSystem/CVManagementSystem.csproj

COPY . .
RUN dotnet publish ./CVManagementSystem/CVManagementSystem.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CVManagementSystem.dll"]