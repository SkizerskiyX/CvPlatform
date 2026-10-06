# Support tickets → Dropbox → Power Automate

Кнопки Help в верхней панели и Create support ticket внизу доступны на любой странице. Отправлять обращения могут вошедшие пользователи любой роли. API получает имя, почту и роли из текущего аккаунта, название позиции — из страницы позиции или доступного резюме, адреса администраторов — из пользователей роли Admin. В запросе пользователь не может подменить отправителя, получателей или название позиции. Query-параметры и токены в ссылку не включаются.

POST /api/support/tickets загружает JSON в Dropbox по HTTPS. Нет SMTP, новой БД или миграций. Без настройки загрузки сайт запускается, форма возвращает понятную ошибку. Ограничение: три запроса в минуту на аккаунт на одном экземпляре сервера. При повторе после сетевой ошибки используется тот же ID/путь файла, без создания дополнительного файла. Это не гарантия доставки письма ровно один раз: повторы действий самого flow надо учитывать отдельно.

## Dropbox

1. Создай бесплатный Dropbox аккаунт или используй существующий.
2. https://www.dropbox.com/developers/apps → Create app → Scoped access → App folder. Название, например `cvplatform-support-demo` (должно быть свободным).
3. Permissions → включи `files.content.write`, сохрани. Для чтения через встроенный Power Automate коннектор используется его отдельное подключение, а не ключи нашего приложения.
4. Settings → скопируй App key и App secret. Не публикуй их.
5. Settings → Redirect URIs → добавь точно `https://cvplatform-h1rj.onrender.com/api/support/dropbox/callback` и сохрани.
6. Настрой Render по таблице ниже и задеплой. На основном сайте CV Platform войди администратором → Пользователи → Подключить Dropbox. В браузере войди в нужный Dropbox аккаунт и нажми Allow. Тот же аккаунт должен использоваться в Power Automate.
7. Сайт сам обменяет код на refresh token, создаст папку `/support-tickets` и сохранит зашифрованный токен в базе. PowerShell, Postman и ручной обмен кода не нужны. Физический путь для flow: `/Apps/<имя приложения>/support-tickets`; путь в API App-folder приложения: `/support-tickets`.

Согласие запускается только администратором, возврат защищён state-cookie с TTL 10 минут и PKCE; права администратора проверяются заново при возврате. Refresh token зашифрован AES-GCM, не возвращается браузеру. Он переживает перезапуск Render. Если разрешение отозвано в Dropbox, приложение/секрет или ключ шифрования изменились, переподключи Dropbox. Refresh token не означает вечную гарантию доступа.

## Render — основной сервис CV Platform, не Odoo

Добавь Environment variables:

| Key | Value |
| --- | --- |
| Support__PublicOrigin | https://cvplatform-h1rj.onrender.com |
| Support__AppKey | Dropbox App key |
| Support__AppSecret | Dropbox App secret |
| Support__EncryptionKey | отдельный случайный секрет, минимум 32 символа; сохранить неизменным между деплоями |
| Support__Folder | /support-tickets |

Отправь код в GitHub и задеплой основной сервис. Миграция AddSupportDropboxConnection создаст таблицу зашифрованного подключения автоматически. Удали Support__AccessToken, если добавлял временный токен. Support__RefreshToken вручную больше не нужен. Проверь, что в CV Platform есть аккаунт Admin с правильной почтой. Получатели берутся из базы, а не из формы пользователя. Адрес Odoo не используется для Dropbox callback.

## Power Automate

Для входа и доступа к cloud flows нужен подходящий рабочий/учебный Microsoft аккаунт и разрешения среды. Наличие бесплатного Dropbox не обеспечивает наличие лицензии Power Automate. Проверь доступ к make.powerautomate.com до настройки. Мы не создаём платные подписки автоматически.

1. Create → Automated cloud flow. Назови `CV Platform support tickets`.
2. Dropbox → When a file is created. Подключи тот же Dropbox аккаунт. Folder: `/Apps/<имя приложения>/support-tickets`. Используй именно триггер новых файлов, не всех изменений.
3. Если триггер содержит только метаданные, добавь Dropbox → Get file content, File — идентификатор файла из триггера. Если содержит File content, используй его непосредственно.
4. Data Operations → Parse JSON. Content — File content предыдущего шага; Schema — содержимое `ticket.schema.json`. Если коннектор возвращает бинарную обёртку `$content`, декодируй `base64ToString(...)`; не разбирай метаданные файла вместо содержимого.
5. Office 365 Outlook → Send an email (V2). Подключи свой рабочий/учебный почтовый аккаунт. Получателем может быть Gmail — Google SMTP не используется. To: expression `join(body('Parse_JSON')?['Admins'' e-mail addresses'], ';')` (имя Parse_JSON замени, если переименовал действие). Subject: `CV Platform support` + Priority + Summary из dynamic content.
6. Body: добавь заголовок «Новое обращение», затем Summary, Reported by, Position, Link, Priority, Created at, Ticket ID через dynamic content. Для безопасной простой демонстрации не вставляй пользовательские строки в HTML-атрибуты, не собирай HTML через сырое concat. Используй обычный текст с переносами и видимой ссылкой. Если нужен HTML, предварительно экранируй `&`, `<`, `>`, кавычки в динамических значениях.
7. Вместо Outlook можно добавить Microsoft Teams → Post message in a chat or channel и выбрать администратора/канал. Это разрешённая техлидом замена уведомления в снятом с поддержки мобильном Power Automate.
8. Save → включи flow. Создай новое обращение через сайт и проверь Run history. Сам факт загрузки JSON не подтверждает доставку уведомления.

Стандартный Gmail-коннектор для личных @gmail.com имеет ограничения на комбинацию сервисов, Dropbox не входит в разрешённый список. Поэтому простой вариант — отправить письмо НА Gmail через Office 365 Outlook или показать Teams. Если нужен именно отправитель Gmail с Dropbox, понадобится Bring your own application в Gmail-коннекторе и отдельный Google OAuth; это не требуется выбранным сценарием.

## Демонстрация

Открой опубликованный сайт, войди пользователем, открой позицию, Help, введи summary и priority, отправь. Покажи JSON в Dropbox, поля отправителя/роли/позиции/ссылки/почты администраторов, успешный flow Run history, письмо на Gmail или сообщение Teams. Из другой страницы Position будет null. Получение на телефоне покажи в Gmail/Teams, не в снятом с поддержки приложении Power Automate.

Проверки загрузчика: `dotnet run --project tests/CvPlatform.SupportChecks`. Настоящий Dropbox/flow требует ключей и подключения аккаунтов; автоматические проверки используют поддельный HTTP-обработчик и не отправляют письма.

Источники: [Dropbox OAuth](https://developers.dropbox.com/oauth-guide), [Upload API](https://docs.dropboxapi.com/dropbox-api/api-reference/user-endpoints/files/upload), [Dropbox connector](https://learn.microsoft.com/en-us/connectors/dropbox/), [Gmail restrictions](https://learn.microsoft.com/en-us/connectors/gmail/), [Power Automate account requirements](https://learn.microsoft.com/en-us/power-automate/frequently-asked-questions).
