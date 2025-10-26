// Подключаем необходимые пространства имен.
// Telegram.Bot содержит все основные классы для работы с ботом.

using Microsoft.Extensions.Configuration;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


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

//// Асинхронная функция для обработки входящих обновлений (сообщений).
//async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
//{
//    // Проверяем, является ли обновление сообщением и содержит ли оно текст.
//    // Это важно, потому что обновление может быть, например, колбэком от инлайн-кнопки.
//    if (update.Type != UpdateType.Message)
//        return;
//    if (update.Message!.Type != MessageType.Text)
//        return;

//    var chatId = update.Message.Chat.Id; // Уникальный идентификатор чата с пользователем.
//    var messageText = update.Message.Text; // Текст сообщения от пользователя.

//    Console.WriteLine($"Получено сообщение '{messageText}' в чате {chatId}.");

//    // Обрабатываем команду /start
//    if (messageText == "/start")
//    {
//        // Создаем клавиатуру с одной кнопкой.
//        // ReplyKeyboardMarkup - это кастомная клавиатура, которая появляется вместо стандартной.
//        var replyKeyboard = new ReplyKeyboardMarkup(
//            new[]
//            {
//                // Первый ряд кнопок. Можно добавить несколько кнопок в один массив.
//                new KeyboardButton[] { "ми меня!" },
//            }
//        )
//        {
//            ResizeKeyboard = true // Клавиатура подстроится под размер кнопок (будет компактнее).
//        };

//        // Отправляем сообщение с текстом и нашей клавиатурой.
//        Message sentMessage = await botClient.SendMessage(
//            chatId: chatId,
//            text: "Добро пожаловать! Нажмите на кнопку ниже.",
//            replyMarkup: replyKeyboard, // Прикрепляем клавиатуру к сообщению.
//            cancellationToken: cancellationToken
//        );
//    }
//    // Обрабатываем нажатие на нашу кастомную кнопку.
//    else if (messageText == "ми меня!")
//    {
//        // Отправляем ответное сообщение "Привет!".
//        Message sentMessage = await botClient.SendMessage(
//            chatId: chatId,
//            text: "Привет!",
//            cancellationToken: cancellationToken
//        );
//    }
//    // Обрабатываем любое другое текстовое сообщение.
//    else
//    {
//        Message sentMessage = await botClient.SendMessage(
//            chatId: chatId,
//            text: "Пожалуйста, используйте кнопки.",
//            cancellationToken: cancellationToken
//        );
//    }
//}

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Обрабатываем команды
    if (update.Message is { Text: { } messageText } message)
    {
        var chatId = message.Chat.Id;

        if (messageText == "/start" || messageText == "/menu")
        {
            // Получаем имя пользователя: сначала пробуем Username, если нет — FirstName
            var userName = message.From.FirstName;

            // Создаем INLINE-кнопки (под сообщением)
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎯 Жми епта!", "hello_button"),
                    InlineKeyboardButton.WithCallbackData("ℹ️ Помощь", "help_button")
                }
            });

            await botClient.SendMessage(
                chatId: chatId,
                text: $"Добро пожаловать, {userName}! Используйте кнопки ниже:\n\n⚠️ Эти кнопки НЕ исчезнут!",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
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

