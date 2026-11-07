FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore from the project files only, so this layer stays cached until a dependency changes.
COPY FoodDelivery.slnx Directory.Build.props Directory.Packages.props ./
COPY FoodDelivery.Domain/FoodDelivery.Domain.csproj FoodDelivery.Domain/
COPY FoodDelivery.Application/FoodDelivery.Application.csproj FoodDelivery.Application/
COPY FoodDelivery.Infrastructure/FoodDelivery.Infrastructure.csproj FoodDelivery.Infrastructure/
COPY FoodDelivery.WebApi/FoodDelivery.WebApi.csproj FoodDelivery.WebApi/
RUN dotnet restore FoodDelivery.WebApi/FoodDelivery.WebApi.csproj

COPY FoodDelivery.Domain/ FoodDelivery.Domain/
COPY FoodDelivery.Application/ FoodDelivery.Application/
COPY FoodDelivery.Infrastructure/ FoodDelivery.Infrastructure/
COPY FoodDelivery.WebApi/ FoodDelivery.WebApi/
RUN dotnet publish FoodDelivery.WebApi/FoodDelivery.WebApi.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The aspnet image ships a non-root "app" user.
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "FoodDelivery.WebApi.dll"]
