# Passo 1: Restore (copia apenas arquivos necessários para restore, para aproveitar cache)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /app

COPY ./src/FiapCloudGames.Payments.slnx ./

COPY ./src/FiapCloudGames.Payments.Api/*.csproj ./FiapCloudGames.Payments.Api/
COPY ./src/FiapCloudGames.Payments.Application/*.csproj ./FiapCloudGames.Payments.Application/
COPY ./src/FiapCloudGames.Payments.Domain/*.csproj ./FiapCloudGames.Payments.Domain/
COPY ./src/FiapCloudGames.Payments.Infrastructure/*.csproj ./FiapCloudGames.Payments.Infrastructure/
COPY ./src/FiapCloudGames.Payments.Observability/*.csproj ./FiapCloudGames.Payments.Observability/
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

# Passo 6: Instalação do agente New Relic (observabilidade)
RUN apt-get update && apt-get install -y curl ca-certificates gnupg \
    && echo 'deb [signed-by=/etc/apt/keyrings/newrelic-apt.gpg] http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
    && curl -fsSL https://download.newrelic.com/NEWRELIC_APT_2DAD550E.public | gpg --dearmor > /etc/apt/keyrings/newrelic-apt.gpg \
    && apt-get update \
    && apt-get install -y newrelic-dotnet-agent \
    && rm -rf /var/lib/apt/lists/*

# Habilitar o agente New Relic
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so

EXPOSE 8081

ENTRYPOINT ["dotnet", "FiapCloudGames.Payments.Api.dll"]

#gera a imagem  projetofiap/payments-api:1 
#docker build -f docker/Dockerfile -t projetofiap/payments-api:1 . 
#docker run -d -p 8081:8081 --name payments-api projetofiap/payments-api:1

#docker push projetofiap/payments-api:1 #sobe imagem para o repositório no Docker Hub