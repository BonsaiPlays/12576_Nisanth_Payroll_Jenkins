# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore dependencies
COPY PayrollApi.csproj ./
RUN dotnet restore

# Copy the rest of the source code
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Expose port 5000 for the API
EXPOSE 5111

# Set environment variable for ASP.NET Core to listen on port 5000
ENV ASPNETCORE_URLS=http://+:5111

# Run the application
ENTRYPOINT ["dotnet", "PayrollApi.dll"]
