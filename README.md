# funduk_API

# Инструкция по запуску (не закрывать вновь открытые вкладки):

1. `docker compose up -d`

2. Перейти на http://localhost:15101/swagger/index.html и зарегистрировать пользователя в POST /users
<img width="1784" height="971" alt="image" src="https://github.com/user-attachments/assets/81ddf176-9946-4e09-ae6c-473fefcf2b2d" />

3. Через POST в /auth/login заходим и получаем JWT-токен (запоминаем)
<img width="1990" height="975" alt="image" src="https://github.com/user-attachments/assets/80cb98c7-5994-4bcd-881d-bd93b3924deb" />

4. Перейти на http://localhost:15103/swagger/index.html и зарегистрировать продукт в POST /products
<img width="1789" height="960" alt="image" src="https://github.com/user-attachments/assets/ecac8a45-03f8-4ed6-ab94-d879d846ee9f" />

5. Перейти на http://localhost:15102/swagger/index.html и зарегистировать заказ в POST /orders (нужен UserId)
<img width="1797" height="941" alt="image" src="https://github.com/user-attachments/assets/859cdd0f-8fee-4bb9-a9da-f29df591af0b" />

6. На той же вкладке добавляем в PATCH /orders/{id}/items/add/ продукт в наш заказ (нужен OrderId, ProductId)
<img width="1987" height="802" alt="image" src="https://github.com/user-attachments/assets/a142c0e2-a311-49fa-aa22-9cc5dcb8a6a5" />

7. Перейти на http://localhost:15000/swagger/index.html и авторизироваться через кнопку Authorize, заполняем JWT-токен из шага 3
<img width="947" height="453" alt="image" src="https://github.com/user-attachments/assets/f5007539-394f-48bb-b17b-428e4d18c722" />

8. Загрузить через GET /api/profile/{userId} информацию (заказы и личная информация) о пользователе (нужен UserId)
<img width="1970" height="1224" alt="image" src="https://github.com/user-attachments/assets/40231c55-db3d-4ac3-ae68-0243ad48459a" />


# Описание (плюсами отмечено то, что добавлено):

Разработать API Gateway (или Backend for Frontend — BFF), который объединяет данные из нескольких микросервисов и предоставляет клиентам единое REST API. Сервис должен поддерживать агрегацию ответов, кэширование данных и маршрутизацию запросов.

# Цель:

Научиться проектировать слой Gateway между фронтендом и микросервисами, объединять данные из разных источников, применять кэширование, ограничение доступа и fallback-логику.

# Компоненты системы
- (+) 1. User Service — хранит информацию о пользователях.
- (+) 2. Order Service — возвращает список заказов пользователя.
- (+) 3. Product Service — хранит данные о товарах.
- (+) 4. API Gateway (BFF) — агрегирует данные и предоставляет клиентам единый REST API.
   
# Пример сценария
GET /api/profile/{userId}

Gateway объединяет данные из трёх сервисов и возвращает общий JSON-ответ, кэшируя результат на 30 секунд.

# Функциональные требования
- (+) - REST API с агрегацией данных.
- (+) - Кэширование (Redis).
- (+) - Retry и fallback при недоступности сервисов.
- (+) - Rate limiting и авторизация (JWT).
  
# Нефункциональные требования
- (+) - Язык: C# (.NET 8).
- (+) - Фреймворк: ASP.NET Core Minimal API или YARP.
- (+) - Контейнеризация: Docker Compose.
- (+) - Мониторинг: Prometheus + Grafana.
- (+) - Логирование: Serilog.
  
# Дополнительно
- .  - Поддержка GraphQL.
- (+) - Circuit Breaker через Polly.
- .  - gRPC-транспорт для внутренних вызовов.
- .  - Использование Ocelot как альтернативы YARP.
