FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["gRPC.Server/gRPC.Server.csproj", "gRPC.Server/"]
RUN dotnet restore "gRPC.Server/gRPC.Server.csproj"
COPY . .
WORKDIR "/src/gRPC.Server"
RUN dotnet build "gRPC.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "gRPC.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "gRPC.Server.dll"]
