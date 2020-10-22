﻿using System;
using System.Threading.Tasks;
using BotFramework.Clients;
using BotFramework.Responses;
using Telegram.Bot.Types;

namespace BotFramework.Commands
{
    public interface ICommand
    {
        Task<Response> Execute(IClient client);
    }

    public interface IStaticCommand : ICommand
    {
        bool SuitableFirst(Update message) => false;
        bool SuitableLast(Update  message) => false;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class StaticCommandAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class OnStartCommand : Attribute { }
}