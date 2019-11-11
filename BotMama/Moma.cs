﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BotFramework.Bot;
using Newtonsoft.Json;


namespace BotMama
{
    public static class Moma
    {
        public static  char          S = Path.DirectorySeparatorChar;
        public static  MomaConfig    Config     { get; set; }
        public static  List<IClient> Clients    { get; set; }
        private static string        ConfigPath { get; set; }

        public static async void Configure(string momaConfigPath)
        {
            ConfigPath = momaConfigPath;
            if (File.Exists(ConfigPath))
            {
                Config = LoadConfig();
            }
            else
            {
                Config = new MomaConfig
                {
                    BotsDir = $"data{S}Bots",
                };
                SaveConfig();
            }

            if (Config.BotConfigs == null)
            {
                Config.BotConfigs = new MomaConfig.BotConfig[] {default};
                SaveConfig();
            }

            if (!Directory.Exists(Config.BotsDir))
                Directory.CreateDirectory(Config.BotsDir);

            foreach (var botConfig in Config.BotConfigs)
            {
                if (botConfig.Name == null)
                {
                    Log("Error in config: botname is null");
                    continue;
                }

                if (botConfig.GitRepo == null)
                {
                    Log("Error in config: gitrepo is null");
                    continue;
                }

                var dirname = Config.BotsDir + S + botConfig.Name;
                if (!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                    await CloneRepo(botConfig.GitRepo, dirname);
                }

                var innerDirs = Directory.GetDirectories(dirname);
                if (innerDirs.FirstOrDefault(d => d == "obj") == null) await DotnetRestore(dirname);

                if (innerDirs.FirstOrDefault(d => d == "bin") == null) await DotnetBuild(dirname);
            }

            Clients = LoadAssemblies(Config.BotsDir).ToList();
        }

        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        private static IEnumerable<IClient> LoadAssemblies(string botsDir)
        {
            foreach (var dir in Directory.GetDirectories(botsDir))
            {
                foreach (var file in Directory.GetFiles(dir, "*.dll"))
                {
                    var assembly = Assembly.LoadFrom(file);
                    AppDomain.CurrentDomain.Load(assembly.GetName());
                    yield return new Client("823973981:AAGYpq1Eyl_AAYGXLeW8s28uCH89S7fsHZA", assembly);
                }
            }
        }

        private static async Task CloneRepo(string giturl, string dirname)
        {
            var result = await CliWrap.Cli.Wrap("git")
                                      .SetArguments($"clone {giturl} {dirname}")
                                      .EnableExitCodeValidation(false)
                                      .ExecuteAsync();
            Log(result.StandardOutput);
        }

        private static async Task DotnetRestore(string dirname)
        {
            var result = await CliWrap.Cli.Wrap("dotnet")
                                      .SetArguments($"restore {dirname}")
                                      .EnableExitCodeValidation(false)
                                      .ExecuteAsync();
            Log(result.StandardOutput);
        }

        private static async Task DotnetBuild(string dirname)
        {
            var result = await CliWrap.Cli.Wrap("dotnet")
                                      .SetArguments($"build {dirname}")
                                      .EnableExitCodeValidation(false)
                                      .ExecuteAsync();
            Log(result.StandardOutput);
        }

        private static MomaConfig LoadConfig()
        {
            return JsonConvert.DeserializeObject<MomaConfig>(File.ReadAllText(ConfigPath));
        }

        private static void SaveConfig()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Config));
        }
    }
}