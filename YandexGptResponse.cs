using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppTelegramBot
{
    internal class YandexGptResponse
    {
        public string? Result { get; set; }
    }

    internal class YandexGptRequest
    {
        public string? Text { get; set; }
        public string? FolderId { get; set; }
    }
}
