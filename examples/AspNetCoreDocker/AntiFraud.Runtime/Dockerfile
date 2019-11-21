FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["MultiService/AntiFraud.Runtime/AntiFraud.Runtime.csproj", "MultiService/AntiFraud.Runtime/"]
COPY ["MultiService/AntiFraud.Domain/AntiFraud.Domain.csproj", "MultiService/AntiFraud.Domain/"]
COPY ["MultiService/Users.Contract/Users.Contract.csproj", "MultiService/Users.Contract/"]
RUN dotnet restore "MultiService/AntiFraud.Runtime/AntiFraud.Runtime.csproj"
COPY . .
WORKDIR "/src/MultiService/AntiFraud.Runtime"
RUN dotnet build "AntiFraud.Runtime.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AntiFraud.Runtime.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AntiFraud.Runtime.dll"]