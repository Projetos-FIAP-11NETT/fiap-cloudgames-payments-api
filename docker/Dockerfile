# Passo 1: Restore (copia apenas arquivos necessários para restore, para aproveitar cache)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /app

COPY ./src/FiapCloudGames.Payments.slnx ./

COPY ./src/FiapCloudGames.Payments.Api/*.csproj ./FiapCloudGames.Payments.Api/
COPY ./src/FiapCloudGames.Payments.Application/*.csproj ./FiapCloudGames.Payments.Application/
COPY ./src/FiapCloudGames.Payments.Domain/*.csproj ./FiapCloudGames.Payments.Domain/
COPY ./src/FiapCloudGames.Payments.Infrastructure/*.csproj ./FiapCloudGames.Payments.Infrastructure/
COPY ./src/FiapCloudGames.Payments.Tests/*.csproj ./FiapCloudGames.Payments.Tests/
COPY ./src/FiapCloudGames.Queue/*.csproj ./FiapCloudGames.Queue/

RUN dotnet restore FiapCloudGames.Payments.slnx

# Passo 2: Build (copia o código restante e compila, aproveitando cache do restore)

FROM restore AS build
WORKDIR /app

COPY ./src/. .

RUN dotnet build FiapCloudGames.Payments.slnx \
    -c Release \
    --no-restore

# Passo 3: Test (rodar testes unitários, aproveitando cache do build)

FROM build AS test
RUN dotnet test FiapCloudGames.Payments.slnx \
    -c Release \
    --no-build \
    --verbosity normal

# Passo 4: Publish (gera os arquivos de publicação, aproveitando cache do build)

FROM build AS publish
RUN dotnet publish FiapCloudGames.Payments.Api/FiapCloudGames.Payments.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-build

# Passo 5: Runtime (gera a imagem final, copiando apenas os arquivos de publicação)

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Instalar dependências necessárias para o driver PostgreSQL
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8081

ENTRYPOINT ["dotnet", "FiapCloudGames.Payments.Api.dll"]

#gera a imagem  projetofiap/payments-api:1 
#docker build -f docker/Dockerfile -t projetofiap/payments-api:1 . 
#docker run -d -p 8081:8081 --name payments-api projetofiap/payments-api:1

#docker push projetofiap/payments-api:1 #sobe imagem para o repositório no Docker Hub