﻿using System.Threading.Tasks;
using BotFramework.Clients;
using BotFramework.Responses;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BotFramework.Commands
{
    public abstract class MessageStaticCommand : IStaticCommand
    {
        public abstract Task<Response> Execute(IClient client);

        public bool Suitable(Update message) =>
        message.Message != null && message.Type == UpdateType.Message && Suitable(message.Message);

        public abstract bool Suitable(Message message);

    }
}