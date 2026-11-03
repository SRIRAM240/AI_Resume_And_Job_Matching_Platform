# =============================================================
# Stage 1: Build & Publish .NET 10 Web API
# =============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project definition and restore dependencies
COPY ResumeJobMatching.Api.csproj ./
RUN dotnet restore "ResumeJobMatching.Api.csproj"

# Copy remaining source code and publish release binary
COPY . ./
RUN dotnet publish "ResumeJobMatching.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =============================================================
# Stage 2: ASP.NET Core Runtime
# =============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Directory for storing uploaded resumes persistently
RUN mkdir -p /app/App_Data/Resumes

# Expose backend port
EXPOSE 5175
ENV ASPNETCORE_URLS=http://+:5175

ENTRYPOINT ["dotnet", "ResumeJobMatching.Api.dll"]

