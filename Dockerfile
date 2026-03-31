# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ source
COPY . .

# ⚠️ SỬA LẠI ĐÚNG PATH FILE .csproj
RUN dotnet restore AutoCurriculum.csproj

# Build + publish
RUN dotnet publish AutoCurriculum.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

# Chạy app
ENTRYPOINT ["dotnet", "AutoCurriculum.dll"]