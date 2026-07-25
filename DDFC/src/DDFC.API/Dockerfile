FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + project files
COPY *.sln .
COPY src/DDFC.Domain/DDFC.Domain.csproj             src/DDFC.Domain/
COPY src/DDFC.Application/DDFC.Application.csproj   src/DDFC.Application/
COPY src/DDFC.Infrastructure/DDFC.Infrastructure.csproj src/DDFC.Infrastructure/
COPY src/DDFC.API/DDFC.API.csproj                   src/DDFC.API/
COPY ../WorkflowEngine/WorkflowEngine/WorkflowEngine.csproj WorkflowEngine/WorkflowEngine/

RUN dotnet restore src/DDFC.API/DDFC.API.csproj

# Copy everything else
COPY . .
COPY ../WorkflowEngine ../WorkflowEngine

RUN dotnet build src/DDFC.API/DDFC.API.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish src/DDFC.API/DDFC.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DDFC.API.dll"]
