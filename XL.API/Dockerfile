FROM mcr.microsoft.com/dotnet/aspnet:8.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-preview AS build
WORKDIR /src
COPY ["XL.API/XL.API.csproj", "XL.API/"]
RUN dotnet restore "XL.API/XL.API.csproj"
COPY . .
WORKDIR "/src/XL.API"
RUN dotnet build "XL.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "XL.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "XL.API.dll"]