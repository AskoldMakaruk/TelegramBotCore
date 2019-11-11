﻿namespace BotMama
{
    public class MomaConfig
    {
        public string      BotsDir         { get; set; }
        public BotConfig[] BotConfigs      { get; set; }

        public struct BotConfig
        {
            public string Name    { get; set; }
            public string Token   { get; set; }
            public string GitRepo { get; set; }
        }
    }
}