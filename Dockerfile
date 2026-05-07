FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Emojimental/Emojimental.csproj", "Emojimental/"]
RUN dotnet restore "Emojimental/Emojimental.csproj"

COPY . .
RUN dotnet publish "Emojimental/Emojimental.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

COPY --from=build /app/publish/wwwroot .

EXPOSE 80
