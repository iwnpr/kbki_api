# qbch-backup-tool

Консольная утилита восстановления данных **fallback-сценария** QBCH.

## Назначение

`QBCH_api` при штатной работе сохраняет результат обработки запроса в **Redis**, а
затем отправляет уведомление в **Kafka**. Если запись в Redis не удаётся (Redis
недоступен), обработчик
[`QBCHProcessingCompleteHandler`](../QBCH_api/QBCHProcessing/V2/StoreProcessingData/Event/QBCHProcessingCompleteHandler.cs)
перехватывает исключение и сохраняет данные запроса в **backup-файл**:

```
backup/{RequestId}.json      # один JSON-объект на файл
```

Эта утилита предназначена для **ручного запуска во время инцидента** (или после его
устранения). Для каждого backup-файла она:

1. **вычитывает** запись из файла;
2. **повторно отправляет** данные в Redis и уведомление в Kafka;
3. при **успехе удаляет** обработанный backup-файл.

Файлы, которые не удалось восстановить, остаются на диске — их можно обработать
повторно после устранения причины сбоя.

## Что именно восстанавливается

Утилита реконструирует те же поля redis-хэша, что формирует
`ConstractResultData` в API (`request_date_time`, `request_certificate_thumbprint`,
`response_date_time`, `response_guid`, `ip_address`, `error_code`, `error_message`,
`request_signed_data`, `request_xml`, `response_signed_data`, `response_xml`,
`validation_date_time`), и отправляет в Kafka сообщение с ключом-значением
`QBCH:{service}:{RequestId}` — как в исходном обработчике.

> **Ограничение.** В backup-файле физически отсутствуют «сырые» данные сертификата
> (`request_certificate_data`), ошибки пакетного запроса (`package_error`) и
> клиентский `request_id`. Эти поля восстановить невозможно, они не записываются.

Ключ Redis и запись в него **идемпотентны**: повторный запуск утилиты для того же
файла безопасен (данные перезапишутся теми же значениями).

## Сборка

Утилита входит в решение `QBCH_api.sln` отдельным проектом.

```bash
dotnet build QBCH_backup_tool/QBCH_backup_tool.csproj -c Release
```

## Конфигурация

Утилите нужны те же параметры подключения, что и `QBCH_api`. Проще всего передать
готовый `appsettings` нужного окружения от API через `--config`.

| Параметр | Назначение |
| --- | --- |
| `ConnectionStrings:Redis` | строка подключения StackExchange.Redis |
| `RedisCache:DBIndex` | индекс БД Redis (по умолчанию `0`) |
| `KafkaService:BootstrapServers` | адрес брокера Kafka |
| `KafkaService:Topic` | топик уведомлений |

Источники конфигурации (каждый следующий переопределяет предыдущий):

1. `appsettings.json` рядом с утилитой (только логирование Serilog и значения по умолчанию — **секретов не содержит**);
2. `appsettings.{environment}.json` (если указан `--environment`);
3. файлы из `--config`;
4. переменные окружения;
5. переопределения `--define` (`-D`).

## Использование

```
qbch-backup-tool [опции]
```

| Опция | Описание |
| --- | --- |
| `-d, --backup-dir <путь>` | каталог с backup-файлами (по умолчанию `backup`) |
| `-f, --file <путь>` | обработать конкретный файл (можно повторять); каталог не сканируется |
| `-t, --target <both\|redis\|kafka>` | куда отправлять (по умолчанию `both`) |
| `--service-name <имя>` | имя сервиса / redis-scope (по умолчанию `dlrequest`) |
| `-c, --config <путь>` | доп. json-файл конфигурации (можно повторять) |
| `-e, --environment <env>` | окружение для `appsettings.{env}.json` |
| `-D, --define <Ключ=Значение>` | переопределить параметр конфигурации |
| `-n, --dry-run` | ничего не отправлять и не удалять — только показать план |
| `--keep` | не удалять backup-файлы даже при успехе |
| `--stop-on-error` | остановиться на первой ошибке (по умолчанию — продолжать) |
| `-v, --verbose` | подробный (Debug) вывод в консоль |
| `-h, --help` | справка |

### Цели (`--target`)

- `both` (по умолчанию) — записать в Redis, затем отправить уведомление в Kafka.
  Файл удаляется, только если **обе** операции успешны. Kafka не отправляется, если
  запись в Redis не удалась.
- `redis` — только запись в Redis. Файл удаляется при успехе записи. Подключение к
  Kafka не требуется.
- `kafka` — только повторная отправка уведомления в Kafka (например, когда данные
  в Redis уже есть, а сообщение Kafka было потеряно). Подключение к Redis не требуется.

### Коды возврата

| Код | Значение |
| --- | --- |
| `0` | все записи обработаны успешно (или обрабатывать нечего) |
| `1` | одна или несколько записей не обработаны |
| `2` | ошибка запуска / конфигурации |

## Примеры

```bash
# 1. Сначала — «сухой прогон»: посмотреть, что будет сделано, ничего не меняя.
qbch-backup-tool --config /opt/qbch_api/appsettings.Production.json --dry-run --verbose

# 2. Восстановить все записи, взяв настройки из appsettings API.
qbch-backup-tool --config /opt/qbch_api/appsettings.Production.json

# 3. Восстановить один файл только в Redis.
qbch-backup-tool -c appsettings.Production.json -t redis -f backup/8f1c2e34-....json

# 4. Переотправить только уведомления в Kafka (данные в Redis уже есть).
qbch-backup-tool -c appsettings.Production.json -t kafka

# 5. Указать подключение напрямую, без файла конфигурации.
qbch-backup-tool \
  -D 'ConnectionStrings:Redis=10.10.100.84:6379,password=***,abortConnect=false' \
  -D KafkaService:BootstrapServers=10.10.100.71:9092 \
  -D KafkaService:Topic=RedisMessagesTopicv2

# 6. Каталог backup в нестандартном месте, файлы не удалять (для проверки).
qbch-backup-tool -c appsettings.Production.json -d /var/lib/qbch/backup --keep
```

## Рекомендуемый порядок действий при инциденте

1. Убедитесь, что Redis/Kafka снова доступны.
2. Запустите утилиту с `--dry-run --verbose` и проверьте план восстановления.
3. Запустите без `--dry-run`. При частичных ошибках (код возврата `1`) изучите лог,
   устраните причину и запустите повторно — оставшиеся файлы обработаются заново.
4. Проверьте, что каталог `backup` опустел (или содержит только неисправимые записи).

## Логирование

Логи пишутся в консоль и в файлы `logs/qbch-backup-tool-YYYYMMDD.log` (ротация по дням,
UTF-8). Настройка — в секции `Serilog` файла `appsettings.json`. Файловый лог всегда
пишется на уровне `Debug` и служит журналом восстановления для разбора инцидента.
