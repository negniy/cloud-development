# Cloud Development (вариант 14 «Медицинский пациент»)

> Студент: Дмитрий Степанов, группа 6511.

## Описание

Проект представляет собой распределённое ASP.NET Core приложение для генерации и обработки карточек медицинских пациентов.

Система построена на основе микросервисной архитектуры с использованием:

- **ASP.NET Core**
- **.NET Aspire**
- **Ocelot API Gateway**
- **Redis**
- **LocalStack**
- **AWS SNS + S3**
- **Blazor WebAssembly**

---

## Компоненты системы

### GeneratorService

ASP.NET Core API, выполняющий:

- генерацию карточки пациента по идентификатору;
- кеширование результатов в Redis;
- публикацию события в SNS после генерации пациента.

---

### EventSink

ASP.NET Core сервис-подписчик SNS.

Функции:

- получение webhook-уведомлений от SNS;
- подтверждение подписки;
- сохранение JSON-файлов пациентов в S3 bucket.

---

### API Gateway (Ocelot)

Выполняет:

- маршрутизацию запросов;
- балансировку нагрузки между репликами GeneratorService.

---

### Client (Blazor WASM)

Пользовательский интерфейс.

Позволяет:

- отправлять запросы на генерацию пациента;
- отображать полученные данные.

---

### Redis

Используется как распределённый кеш.

---

### LocalStack

Локальная эмуляция AWS-сервисов:

- SNS
- S3

---

### .NET Aspire AppHost

Используется для:

- оркестрации сервисов;
- запуска инфраструктуры;
- управления зависимостями;
- health checks.

---

## Архитектура

```text
Client (Blazor WASM)
        ↓
API Gateway (Ocelot)
        ↓
GeneratorService (3 реплики)
        ↓
Redis Cache
        ↓
SNS Topic (LocalStack)
        ↓
EventSink
        ↓
S3 Bucket (LocalStack)
```

---

## Event-Driven Pipeline

После генерации пациента:

1. GeneratorService публикует сообщение в SNS Topic.
2. EventSink получает webhook-уведомление.
3. JSON пациента сохраняется в S3 bucket.

---

## REST API

### `GET /patient?id={int}`

| Параметр | Тип | Обязателен | Описание |
|----------|-----|------------|----------|
| `id`     | int (>0) | да | Одновременно идентификатор пациента и seed для генератора; проверяется на положительность. |

---

## Ответ `200 OK`

Возвращает JSON-объект пациента:

| Поле | Тип | Пример | Описание |
|------|------|------|------|
| `id` | int | `42` | Идентификатор пациента |
| `fullName` | string | `Ирина Смирнова Сергеевна` | ФИО пациента |
| `birthday` | DateOnly | `1993-07-20` | Дата рождения |
| `address` | string | `г. Самара, ул. Карла Маркса...` | Адрес |
| `height` | double | `167.35` | Рост |
| `weight` | double | `61.12` | Вес |
| `bloodType` | int | `2` | Группа крови |
| `resus` | bool | `true` | Резус-фактор |
| `lastVisit` | DateOnly | `2026-02-17` | Последний визит |
| `vaccination` | bool | `false` | Наличие прививки |

---

## Возможные статусы

| Статус | Описание |
|--------|----------|
| `200 OK` | Пациент успешно получен |
| `400 BadRequest` | Некорректный id (`id <= 0`) |
| `500 Internal Server Error` | Внутренняя ошибка |

---

## Кеширование

Используется Redis.

При повторном запросе:

- данные возвращаются из кеша;
- уменьшается время ответа;
- в логах появляется сообщение:

```text
Patient with id: X was found in cache
```

---

## Балансировка нагрузки

Реализован кастомный балансировщик:

```text
instanceIndex = id % N
```

где:

- `id` — идентификатор пациента;
- `N` — количество реплик сервиса.

---

## AWS SNS + S3

### SNS

Используется для публикации событий о создании пациента.

### EventSink

Подписывается на SNS Topic и получает уведомления.

### S3

JSON-представления пациентов сохраняются в bucket:

```text
landplot-bucket
```

---

## Интеграционные тесты

Реализованы integration tests с использованием:

- `xUnit`
- `Aspire.Hosting.Testing`
- `LocalStack`

Проверяются:

- доступность API Gateway;
- сохранение файлов в S3;
- корректная работа event-driven pipeline.

---

## Структура репозитория

```text
cloud-development/
├─ Client.Wasm/
│  ├─ Components/
│  ├─ Layout/
│  ├─ Pages/
│  └─ wwwroot/
│
├─ EventSink2/
│  ├─ Controllers/
│  │  └─ SnsSubscriberController.cs
│  ├─ Storage/
│  │  ├─ IS3Service.cs
│  │  └─ S3AwsService.cs
│  └─ Program.cs
│
├─ PatientApp.Gateway/
│  ├─ LoadBalancer/
│  │  └─ QueryBasedLoadBalancer.cs
│  ├─ ocelot.json
│  └─ Program.cs
│
├─ PatientApp.Generator/
│  ├─ Messaging/
│  │  └─ SnsPublisherService.cs
│  ├─ Models/
│  │  └─ Patient.cs
│  ├─ Services/
│  │  ├─ Generator.cs
│  │  └─ PatientService.cs
│  └─ Program.cs
│
├─ PatientApp.AppHost/
├─ PatientApp.ServiceDefaults/
│
├─ Tests/
│  ├─ Fixture.cs
│  └─ IntegrationTests.cs
│
├─ .github/workflows/
├─ CloudDevelopment.sln
└─ README.md
```

---

## Используемые технологии

- ASP.NET Core
- Blazor WebAssembly
- Ocelot
- Redis
- AWS SNS
- AWS S3
- LocalStack
- Docker
- .NET Aspire
- xUnit

---

## Скрины работы
<details>
  Сервисы
<img width="1826" height="1047" alt="image" src="https://github.com/user-attachments/assets/7a37841f-9f22-4b00-9310-2ba17bce203e" />
</details>
<details>
  Ответ клиенту
<img width="1534" height="861" alt="Ответ клиенту" src="https://github.com/user-attachments/assets/23cc8f99-ef49-4937-894f-8cce0d3506ac" />

</details>
<details>
  Трассировки
  <img width="1753" height="760" alt="Трассировки" src="https://github.com/user-attachments/assets/72deb2da-0acc-4a33-b193-ae64d472d2dd" />
</details>
<details>
  Тесты
  <img width="982" height="765" alt="image" src="https://github.com/user-attachments/assets/23029a1b-564b-4116-bb62-e95434a3dcaa" />

</details>
<details>
  Файл в хранилище
 <img width="1749" height="1005" alt="image" src="https://github.com/user-attachments/assets/ce8aad0a-d065-4047-8ccd-3ddf6ec201d2" />

</details>
