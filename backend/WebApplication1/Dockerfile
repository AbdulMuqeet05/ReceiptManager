
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY ["WebApplication1/WebApplication1.csproj", "WebApplication1/"]
COPY ["ProductsApplicationLayer/ProductsApplicationLayer.csproj", "ProductsApplicationLayer/"]
RUN dotnet restore "WebApplication1/WebApplication1.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/app/WebApplication1"
RUN dotnet publish -c Release -o /out


# Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]