// Подключаем необходимые пространства имен.
// Telegram.Bot содержит все основные классы для работы с ботом.

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration.Json;


using Microsoft.Extensions.Configuration;

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

// Обработчик события, который будет вызываться при получении любого сообщения.
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,    // Функция для обработки обновлений
    errorHandler: HandlePollingErrorAsync, // Функция для обработки ошибок
    receiverOptions: null,
    cancellationToken: cts.Token
);

// Получаем информацию о нашем боте (чтобы убедиться, что подключение прошло успешно).
var me = await botClient.GetMe();
Console.WriteLine($"Бот @{me.Username} запущен и готов к работе!");
Console.ReadLine(); // Не даем приложению сразу завершиться.

// Асинхронная функция для обработки входящих обновлений (сообщений).
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Проверяем, является ли обновление сообщением и содержит ли оно текст.
    // Это важно, потому что обновление может быть, например, колбэком от инлайн-кнопки.
    if (update.Type != UpdateType.Message)
        return;
    if (update.Message!.Type != MessageType.Text)
        return;

    var chatId = update.Message.Chat.Id; // Уникальный идентификатор чата с пользователем.
    var messageText = update.Message.Text; // Текст сообщения от пользователя.

    Console.WriteLine($"Получено сообщение '{messageText}' в чате {chatId}.");

    // Обрабатываем команду /start
    if (messageText == "/start")
    {
        // Создаем клавиатуру с одной кнопкой.
        // ReplyKeyboardMarkup - это кастомная клавиатура, которая появляется вместо стандартной.
        var replyKeyboard = new ReplyKeyboardMarkup(
            new[]
            {
                // Первый ряд кнопок. Можно добавить несколько кнопок в один массив.
                new KeyboardButton[] { "Нажми меня!" },
            }
        )
        {
            ResizeKeyboard = true // Клавиатура подстроится под размер кнопок (будет компактнее).
        };

        // Отправляем сообщение с текстом и нашей клавиатурой.
        Message sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: "Добро пожаловать! Нажмите на кнопку ниже.",
            replyMarkup: replyKeyboard, // Прикрепляем клавиатуру к сообщению.
            cancellationToken: cancellationToken
        );
    }
    // Обрабатываем нажатие на нашу кастомную кнопку.
    else if (messageText == "Нажми меня!")
    {
        // Отправляем ответное сообщение "Привет!".
        Message sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: "Привет!",
            cancellationToken: cancellationToken
        );
    }
    // Обрабатываем любое другое текстовое сообщение.
    else
    {
        Message sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: "Пожалуйста, используйте кнопки.",
            cancellationToken: cancellationToken
        );
    }
}

// Функция для обработки ошибок, которые могут возникнуть при опросе серверов Telegram.
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

