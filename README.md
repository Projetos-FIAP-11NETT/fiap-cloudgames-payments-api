# Fiap CloudGames — Payments API

API de pagamentos do ecossistema **CloudGames** (FIAP). Responsável por criar, processar e consultar pagamentos de pedidos, com integração a filas (RabbitMQ / Amazon SQS) para processamento assíncrono.

## Tecnologias

- **.NET 10** — ASP.NET Core Web API  
- **PostgreSQL** — persistência (Entity Framework Core)  
- **RabbitMQ / Amazon SQS** — mensageria (MassTransit + consumer/publisher SQS para o evento `IOrderPlaced`, usado no deploy real na AWS)  
- **MediatR** — CQRS (commands/queries)  
- **FluentValidation** — validação de comandos  
- **OpenAPI / Scalar** — documentação da API  
- **New Relic** — observabilidade (atributos customizados, erros, transações HTTP e MassTransit)

## Estrutura da solução

```
src/
├── FiapCloudGames.Payments.Api          # Web API, controllers, configuração
├── FiapCloudGames.Payments.Application  # Casos de uso, CQRS, DTOs, validadores
├── FiapCloudGames.Payments.Domain        # Entidades, value objects, enums
├── FiapCloudGames.Payments.Infrastructure # EF Core, repositórios, migrations
├── FiapCloudGames.Payments.Observability # New Relic (middleware HTTP, filter MassTransit)
├── FiapCloudGames.Payments.Tests         # Testes unitários (xUnit, FluentAssertions)
└── FiapCloudGames.Queue                 # Configuração de filas (RabbitMQ)
```

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (local ou container)
- RabbitMQ (local ou container), se for usar processamento via fila

## Executando localmente

1. **Restaurar e buildar**

   ```bash
   dotnet restore
   dotnet build
   ```

2. **Configurar conexão e RabbitMQ**

   Edite `src/FiapCloudGames.Payments.Api/appsettings.Development.json` (ou use variáveis de ambiente):

   - `ConnectionStrings:DefaultConnection` — connection string do PostgreSQL  
   - `RabbitmqSettings` — host, porta, virtual host, usuário e senha  
   - `Payment:ApprovalRate` (opcional) — taxa de aprovação simulada (0 a 1)

3. **Aplicar migrations**

   A API aplica migrations na inicialização (via `MigrationConfig`). Certifique-se de que o banco exista e a connection string esteja correta.

4. **Subir a API**

   ```bash
   cd src/FiapCloudGames.Payments.Api
   dotnet run
   ```

   Por padrão a API fica em `https://localhost:7xxx` (verifique o `launchSettings.json`).

## Docker

Para build e execução com Docker, uso de variáveis de ambiente e exemplo de `docker run`, veja **[docker/README.md](docker/README.md)**.  
A imagem inclui o **agente New Relic** para .NET; configure `NEW_RELIC_LICENSE_KEY` e `NEW_RELIC_APP_NAME` (e demais env vars do New Relic) no ambiente para enviar dados ao dashboard.

## Observabilidade (New Relic)

- **HTTP:** o `ObservabilityMiddleware` adiciona `TraceId` e `CorrelationId` às transações e encaminha exceções para o New Relic.
- **HTTP Logging:** os middlewares `RequestResponseLoggingMiddleware` e `ExceptionMiddleware` registram logs com `x-correlation-id` no padrão `[user-service] CorrelationId: ...`.
- **MassTransit:** o `NewRelicConsumeFilter` nomeia transações por fila/mensagem e adiciona `MessageType`, `CorrelationId`; exceções no consumer são reportadas.
- **Consumer Logging:** o `OrderPlacedConsumer` e o `PaymentProcessedPublisher` registram logs no padrão `[payments-service] CorrelationId: ...`.
- Em execução local sem o agente instalado, as chamadas à API do New Relic não geram erro (apenas não há envio de dados).

### Como testar e ver no New Relic

1. **Rodar com o agente New Relic**  
   - **Docker:** use o Dockerfile (já inclui o agente). Passe as variáveis:  
     `NEW_RELIC_LICENSE_KEY`, `NEW_RELIC_APP_NAME` (ex.: `Payments-API`).  
   - **Local (Windows):** instale o [.NET Agent](https://docs.newrelic.com/docs/apm/agents/net-agent/installation/net-agent-windows/) e configure a licença; rode a API com `dotnet run`.

2. **Testar transações HTTP**  
   Chame qualquer endpoint (ex.: `GET /api/payments/user/{guid}` ou `POST /api/payments/simulate`). No New Relic: **APM > Application (Payments-API) > Transactions**.

3. **Testar o consumer (fila) e CorrelationId**  
   - Com a API em **Development** (e RabbitMQ + PostgreSQL rodando), chame:  
     `POST /api/payments/test-order-placed`  
   - Esse endpoint publica um `IOrderPlaced` na fila; o consumer processa e o **NewRelicConsumeFilter** envia a transação para o New Relic.
    - A resposta traz um `correlationId`; no New Relic use **APM > Transactions** (ou **Distributed Tracing**) e filtre por atributo customizado **CorrelationId** = esse valor para ver a transação do consumer com `MessageType`, fila, etc.
    - Para teste manual no RabbitMQ UI (sem endpoint HTTP), publique no exchange `amq.default` com routing key `payments-order-placed`, `content_type=application/vnd.masstransit+json` e envelope MassTransit.

### Exemplo de logs com CorrelationId

- HTTP request: `[user-service] CorrelationId: <id> | Inicio da Requisicao POST /api/payments/test-order-placed`
- HTTP response: `[user-service] CorrelationId: <id> | Final da Requisicao POST /api/payments/test-order-placed | StatusCode: 202 15ms`
- Consumer: `[payments-service] CorrelationId: <id> | OrderPlacedConsumer - Received OrderId: 91234 ...`

### Exemplo de payload para RabbitMQ UI

```json
{
   "messageId": "8f1e9d25-9b0c-4c79-8a84-2f3c0a3e4d11",
   "correlationId": "2f34573f-31e8-4db1-9f92-3ba2d7b95db9",
   "messageType": [
      "urn:message:FiapCloudGames.Queue.Contracts:IOrderPlaced"
   ],
   "message": {
      "orderId": 91234,
      "userId": "3f9a4f7a-317a-4b00-bf68-1f57e3662a22",
      "gameId": "0ddab3ed-3adb-4e07-9904-ea6f84e65f5e",
      "price": 99.9,
      "email": "teste@exemplo.com",
      "name": "Teste Rabbit"
   }
}
```

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET`  | `api/payments/{id}` | Busca pagamento por ID |
| `GET`  | `api/payments/user/{userId}` | Lista pagamentos do usuário |
| `POST` | `api/payments/simulate` | Simula processamento de pagamento (para testes) |
| `POST` | `api/payments/test-order-placed` | **(Só Development)** Publica IOrderPlaced na fila para testar consumer e New Relic |

Em produção, o fluxo real de pagamento é acionado por eventos (ex.: `OrderPlacedEvent`) via fila; o `POST /simulate` existe apenas para testes manuais.

## Testes

- **Framework:** xUnit  
- **Assertions:** FluentAssertions  
- **Cobertura:** Domain (Payment, PaymentMethod), Application (ValidationBehavior), Infrastructure (PaymentsRepository)

Rodar todos os testes:

```bash
dotnet test src/FiapCloudGames.Payments.Tests/FiapCloudGames.Payments.Tests.csproj
```

Com cobertura (se tiver `coverlet` configurado):

```bash
dotnet test src/FiapCloudGames.Payments.Tests/FiapCloudGames.Payments.Tests.csproj --collect:"XPlat Code Coverage"
```

## Configuração

Principais chaves em `appsettings.json` / variáveis de ambiente:

| Configuração | Descrição |
|--------------|-----------|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL |
| `RabbitmqSettings__Address` | Host do RabbitMQ |
| `RabbitmqSettings__Port` | Porta (ex.: 5672) |
| `RabbitmqSettings__VirtualHost` | Virtual host (ex.: `/`) |
| `RabbitmqSettings__Username` / `__Password` | Credenciais |
| `Payment__ApprovalRate` | Taxa de aprovação simulada (0–1) |

No Docker, use `__` (dois underscores) para hierarquia; ver exemplos em [docker/README.md](docker/README.md).

## Health checks

A API expõe health checks para o banco (PostgreSQL) e para dependências configuradas. Consulte a configuração em `HealthCheckConfiguration` e as rotas mapeadas em `Program.cs`.

## Licença

Uso acadêmico / FIAP CloudGames.
