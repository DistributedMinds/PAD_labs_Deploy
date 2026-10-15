FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Broker_gRPC/Broker_gRPC.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV PORT=8080
ENV DB_PATH=/data/broker_messages.db
ENTRYPOINT ["dotnet", "Broker_gRPC.dll"]
