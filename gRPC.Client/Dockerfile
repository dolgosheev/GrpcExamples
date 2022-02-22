FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["gRPC.Client/gRPC.Client.csproj", "gRPC.Client/"]
RUN dotnet restore "gRPC.Client/gRPC.Client.csproj"
COPY . .
WORKDIR "/src/gRPC.Client"
RUN dotnet build "gRPC.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "gRPC.Client.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "gRPC.Client.dll"]
