﻿using System;
using BotFramework.Commands;
using Serilog;
using System.Collections.Generic;

namespace BotFramework.Bot
{
    public class BotConfiguration
    {
        public bool            Webhook        { get; set; }
        public string?         Token          { get; set; }
        public ILogger?        Logger         { get; set; }
        public IClientStorage? Storage        { get; set; }
        public List<Type>      AllCommands    { get; set; } = new List<Type>();
        public List<Type>      StaticCommands { get; set; } = new List<Type>();
        public List<Type>      StartCommands  { get; set; } = new List<Type>();
    }
}