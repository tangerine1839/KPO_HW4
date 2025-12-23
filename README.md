Решение состоит из нескольких сервисов:

- **API Gateway (Nginx)**  
  - Отвечает только за роутинг HTTP‑запросов к backend‑сервисам. 

- **Order Service** 
   - Создание заказа по `userId` и сумме
   - Просмотр списка заказов пользователя
   - Просмотр статуса отдельного заказа
- **Payments Service** 
    - Создание счёта пользователя 
    - Пополнение счёта положительной суммой
    - Просмотр баланса счёта
- **Frontend** 
    - Работает через API Gateway по HTTP
    - Логин/создание счёта по `userId` (GUID)
    - Просмотр и пополнение баланса
    - Создание заказа и просмотр списка заказов со статусами `NEW` / `FINISHED` / `CANCELLED`
   
- **RabbitMQ**  
  - Брокер сообщений с at‑least‑once доставкой, используется для связи Order Service и Payments Service
 - **PostgreSQL**  
  - Отдельная БД для Order Service и отдельная БД для Payments Service
 
    **Команда запуска**: docker-compose up --build
    
## После запуска доступны:
- Frontend: `http://localhost:5000`  
- Order Service Swagger: `http://localhost:8081/swagger`  
- Payments Service Swagger: `http://localhost:8082/swagger`  

## Сценарий работы
1. Пользователь отправляет запрос на создание заказа
2. Order Service в одной транзакции создаёт запись заказа и outbox‑сообщение в своей БД  
3. Фоновый процесс Order Service читает outbox и отправляет событие в RabbitMQ 
4. Payments Service читает событие и сохраняет его в Inbox 
5. Payments Service обрабатывает событие: проверяет наличие счёта, достаточность баланса, списывает деньги атомарно, записывает исход (success/fail) и создаёт outbox‑сообщение результата   
6. Фоновый процесс Payments Service публикует событие результата оплаты в RabbitMQ  
7. Order Service получает событие результата и идемпотентно обновляет статус заказа


