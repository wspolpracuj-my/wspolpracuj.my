## Poniżej jesli chcemy wejsc do kontenera w celu rozwoju aplikacji
# FROM mcr.microsoft.com/dotnet/sdk:10.0
# WORKDIR /workspace

## Poniżej jesli chcemy uruchomic aplikacje z poziomu kontenera
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# Kopiujemy zawartość wcześniej wygenerowanego folderu 'publish' do kontenera
COPY ./publish .

EXPOSE 8081

# Uruchomienie aplikacji
ENTRYPOINT ["dotnet", "wspolpracujmy.dll"]