FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/PhPayrollTimeApi.Api/PhPayrollTimeApi.Api.csproj", "src/PhPayrollTimeApi.Api/"]
COPY ["src/PhPayrollTimeApi.Application/PhPayrollTimeApi.Application.csproj", "src/PhPayrollTimeApi.Application/"]
COPY ["src/PhPayrollTimeApi.Domain/PhPayrollTimeApi.Domain.csproj", "src/PhPayrollTimeApi.Domain/"]
COPY ["src/PhPayrollTimeApi.Infrastructure/PhPayrollTimeApi.Infrastructure.csproj", "src/PhPayrollTimeApi.Infrastructure/"]
RUN dotnet restore "src/PhPayrollTimeApi.Api/PhPayrollTimeApi.Api.csproj"
COPY . .
WORKDIR "/src/src/PhPayrollTimeApi.Api"
RUN dotnet build "PhPayrollTimeApi.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PhPayrollTimeApi.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PhPayrollTimeApi.Api.dll"]
