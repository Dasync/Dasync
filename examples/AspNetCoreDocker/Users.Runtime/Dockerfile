FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["MultiService/Users.Runtime/Users.Runtime.csproj", "MultiService/Users.Runtime/"]
COPY ["MultiService/Users.Domain/Users.Domain.csproj", "MultiService/Users.Domain/"]
COPY ["MultiService/Users.Contract/Users.Contract.csproj", "MultiService/Users.Contract/"]
RUN dotnet restore "MultiService/Users.Runtime/Users.Runtime.csproj"
COPY . .
WORKDIR "/src/MultiService/Users.Runtime"
RUN dotnet build "Users.Runtime.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Users.Runtime.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Users.Runtime.dll"]