// Подключаем необходимые пространства имен.
// Telegram.Bot содержит все основные классы для работы с ботом.

using Microsoft.Extensions.Configuration;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net;

// Загружаем конфигурацию
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();




// Получаем токен из конфигурации
var botToken = configuration["BotConfiguration:BotToken"]
    ?? throw new Exception("Bot token not found in configuration!");



// Создаем экземпляр клиента Telegram Bot.
// Токен, который вы получили от BotFather, подставляется в строку ниже.
var botClient = new TelegramBotClient(botToken);


// Начинаем получать обновления (сообщения, команды, нажатия кнопок) от пользователей.
// UsingCancellation использует токен отмены, чтобы корректно завершить работу бота при остановке.

using var cts = new CancellationTokenSource();



// Устанавливаем команды бота
await botClient.SetMyCommands(
    new[]
    {
        new BotCommand { Command = "menu", Description = "Меню" },
        new BotCommand { Command = "help", Description = "Помощь" }
    },
    cancellationToken: cts.Token
);


// Обработчик события, который будет вызываться при получении любого сообщения.
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,    // Функция для обработки обновлений
    errorHandler: HandlePollingErrorAsync, // Функция для обработки ошибок
    receiverOptions: null,
    cts.Token
);

// Получаем информацию о нашем боте (чтобы убедиться, что подключение прошло успешно).
var me = await botClient.GetMe();
Console.WriteLine($"Бот @{me.Username} запущен и готов к работе!");
Console.ReadLine(); // Не даем приложению сразу завершиться.



//////////////\функция для отправки запроса в Yandex GPT
static async Task<string> GetGptResponseAsync(string prompt, string apiKey, string folderId, CancellationToken cancellationToken)
{
    using var httpClient = new HttpClient();

    var requestData = new
    {
        modelUri = $"gpt://{folderId}/yandexgpt", // что делает эта строка что значит yandexgpt что еще есть кроме yandexgpt
        completionOptions = new
        {
            stream = false,
            temperature = 0.3,
            maxTokens = "2000"
        },
        messages = new[]
        {
            new
            {
                role = "user",
                text = prompt
            }
        }
    };

    //var requestData = new
    //{
    //    modelUri = $"art://{folderId}/yandexart", // или другой ID модели
    //    generationOptions = new
    //    {
    //        mimeType = "image/jpeg", // или png
    //        seed = 42 // для воспроизводимости
    //    },
    //    prompt = prompt // текст, по которому рисуется изображение
    //};

    var json = System.Text.Json.JsonSerializer.Serialize(requestData);

    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {apiKey}");

    var response = await httpClient.PostAsync("https://llm.api.cloud.yandex.net/foundationModels/v1/completion", content, cancellationToken);
    //var response = await httpClient.PostAsync("https://llm.api.cloud.yandex.net/imageGeneration/v1/imageGenerationAsync", content, cancellationToken);
    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

    if (response.IsSuccessStatusCode)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
        var result = doc.RootElement.GetProperty("result");
        var alternatives = result.GetProperty("alternatives");
        var text = alternatives[0].GetProperty("message").GetProperty("text").GetString();

        return text ?? "Не удалось получить ответ.";
    }
    else
    {
        return $"Ошибка: {responseContent}";
    }
}

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Обрабатываем команды
    if (update.Message is { Text: { } messageText } message)
    {
        var chatId = message.Chat.Id;

        // Проверяем, не является ли это командой
        if (messageText.StartsWith("/"))
        {
            // Обработка команд (/start, /menu и т.д.)
            if (messageText == "/start" || messageText == "/menu")
            {
                var userName = message.From.FirstName ?? "Пользователь";

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎯 Нажми меня!", "hello_button"),
                    InlineKeyboardButton.WithCallbackData("ℹ️ Помощь", "help_button")
                }
            });

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Добро пожаловать, {userName}! Используйте кнопки ниже:\n\n⚠️ Эти кнопки НЕ исчезнут!",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken
                );
            }
        }
        else
        {
            // Это обычное сообщение — отправляем в GPT
            var apiKey = configuration["YandexGptConfiguration:ApiKey"] ?? throw new InvalidOperationException("Yandex API Key not found!");
            var folderId = configuration["YandexGptConfiguration:FolderId"] ?? throw new InvalidOperationException("Yandex Folder ID not found!");

            await botClient.SendMessage(
                chatId: chatId,
                text: "Обрабатываю запрос...",
                cancellationToken: cancellationToken
            );

            var gptResponse = await GetGptResponseAsync(messageText, apiKey, folderId, cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: gptResponse,
                cancellationToken: cancellationToken
            );
        }
    }

    // ⭐ Обрабатываем нажатия INLINE-кнопок
    else if (update.CallbackQuery is { } callbackQuery)
    {
        var chatId = callbackQuery.Message!.Chat.Id;

        switch (callbackQuery.Data)
        {
            case "hello_button":
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Привет! Кнопки остались на месте! 🎉",
                    cancellationToken: cancellationToken);
                break;

            case "help_button":
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Я бот с неисчезающими кнопками!",
                    cancellationToken: cancellationToken);
                break;
        }

        // Подтверждаем нажатие кнопки (убираем "часики")
        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }
}




///////////// Функция для обработки ошибок, которые могут возникнуть при опросе серверов Telegram.\\\\\\\\\\\\\\\\

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        // Формируем удобное сообщение об ошибке.
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString() // Любая другая ошибка.
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}

