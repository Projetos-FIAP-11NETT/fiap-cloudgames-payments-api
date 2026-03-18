# Docker — Payments API

## Build (com testes)

Os testes rodam no estágio de build; se falharem, a imagem não é gerada.

```bash
# a partir da pasta src
docker build -f docker/Dockerfile -t payments-api .
```

## Env vars sobrescrevem appsettings

O ASP.NET Core carrega, nesta ordem: `appsettings.json` → `appsettings.{Environment}.json` → **variáveis de ambiente** → argumentos de linha de comando.  
Ou seja, qualquer valor que você passar por `-e` no `docker run` sobrescreve o que está no appsettings.

Use `__` (dois underscores) para hierarquia, igual ao appsettings:

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | Connection string do PostgreSQL |
| `RabbitmqSettings__Address` | Host do RabbitMQ |
| `RabbitmqSettings__Port` | Porta (ex.: 5672) |
| `RabbitmqSettings__VirtualHost` | Virtual host (ex.: /) |
| `RabbitmqSettings__Username` | Usuário |
| `RabbitmqSettings__Password` | Senha |
| `MassTransitSettings__RetryCount` | Número de retentativas |
| `MassTransitSettings__Interval` | Intervalo entre retentativas (ms) |
| `Payment__ApprovalRate` | Taxa de aprovação simulada (0 a 1) |

## Exemplo de run

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=payments_db;Username=postgres;Password=postgres" \
  -e RabbitmqSettings__Address=host.docker.internal \
  -e RabbitmqSettings__Port=5672 \
  -e RabbitmqSettings__Username=guest \
  -e RabbitmqSettings__Password=guest \
  -e Payment__ApprovalRate=0.9 \
  payments-api
```

No Windows/Mac, `host.docker.internal` aponta para o host (Postgres/RabbitMQ em localhost).
