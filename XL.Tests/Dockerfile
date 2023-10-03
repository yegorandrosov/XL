FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base
WORKDIR /app
COPY . .
RUN dotnet restore XL.Tests.sln
ENTRYPOINT ["dotnet", "test", "XL.Tests.sln"]
