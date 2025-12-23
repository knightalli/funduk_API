# funduk_API

# Инструкция по запуску:

1. `docker compose up --build`
2. `cd src/UserService`
3. `dotnet ef migrations add Initial \
  --project UserService.Infrastructure \
  --startup-project UserService`
4. `cd src/OrderService`
5. `dotnet ef migrations add Initial \
  --project OrderService.Infrastructure \
  --startup-project OrderService`
6. `cd src/ProductService`
7. `dotnet ef migrations add Initial \
  --project ProductService.Infrastructure \
  --startup-project ProductService`

# Описание:

Разработать API Gateway (или Backend for Frontend — BFF), который объединяет данные из нескольких микросервисов и предоставляет клиентам единое REST API. Сервис должен поддерживать агрегацию ответов, кэширование данных и маршрутизацию запросов.

# Цель:

Научиться проектировать слой Gateway между фронтендом и микросервисами, объединять данные из разных источников, применять кэширование, ограничение доступа и fallback-логику.

# Компоненты системы
1. User Service — хранит информацию о пользователях.
2. Order Service — возвращает список заказов пользователя.
3. Product Service — хранит данные о товарах.
4. API Gateway (BFF) — агрегирует данные и предоставляет клиентам единый REST API.
   
# Пример сценария
GET /api/profile/{userId}

Gateway объединяет данные из трёх сервисов и возвращает общий JSON-ответ, кэшируя результат на 30 секунд.

# Функциональные требования
- REST API с агрегацией данных.
- Кэширование (Redis).
- Retry и fallback при недоступности сервисов.
- Rate limiting и авторизация (JWT).
  
# Нефункциональные требования
- Язык: C# (.NET 8).
- Фреймворк: ASP.NET Core Minimal API или YARP.
- Контейнеризация: Docker Compose.
- Мониторинг: Prometheus + Grafana.
- Логирование: Serilog.
  
# Дополнительно
- Поддержка GraphQL.
- Circuit Breaker через Polly.
- gRPC-транспорт для внутренних вызовов.
- Использование Ocelot как альтернативы YARP.
