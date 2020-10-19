﻿using System;
using BotFramework.Bot;
using BotFramework.BotTask;
using BotFramework.Responses;
using Telegram.Bot.Types;

namespace BotFramework.Commands
{
    public interface ICommand
    {
        BotTask.BotTask<Response> Execute(IClient client);
    }

    public interface IStaticCommand : ICommand
    {
        bool Suitable(Update message);
    }

    public interface IOnStartCommand : IStaticCommand
    {
        
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class StaticCommandAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class OnStartCommand : Attribute { }
}