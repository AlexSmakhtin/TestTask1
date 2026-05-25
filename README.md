# Тестовое задание для компании 
## Локальный запуск

1. Задайте настройки Telegram через user secrets:

```powershell
dotnet user-secrets set "Telegram:BotToken" "<bot-token>" --project RouterNode.Api
dotnet user-secrets set "Telegram:ChatId" "<chat-id>" --project RouterNode.Api
```

2. Запустите приложение:

```powershell
dotnet run --project RouterNode.Api
```

## Docker-запуск

1. Соберите и запустите контейнер:

```powershell
$env:TELEGRAM_BOT_TOKEN="your_bot_token"
$env:TELEGRAM_CHAT_ID="your_chat_id"

docker compose up -d
```
