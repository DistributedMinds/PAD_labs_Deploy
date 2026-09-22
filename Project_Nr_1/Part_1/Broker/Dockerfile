FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Broker/Broker.csproj Broker/
COPY Message_Agent.Common/Message_Agent.Common.csproj Message_Agent.Common/
RUN dotnet restore Broker/Broker.csproj

COPY Broker/ Broker/
COPY Message_Agent.Common/ Message_Agent.Common/
RUN dotnet publish Broker/Broker.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "Broker.dll"]
