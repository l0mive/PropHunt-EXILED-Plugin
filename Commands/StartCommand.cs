namespace PropHuntMiniGame.Commands
{
    using System;
    using CommandSystem;
    using Exiled.Permissions.Extensions;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class StartCommand : ICommand
    {
        public static StartCommand Instance { get; } = new();

        public string Command => "prophunt";

        public string[] Aliases => new[] { "startPropHunt", "ttstart" };

        public string Description => "Starts, stops, or configures the PropHunt minigame.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("PropHunt.start"))
            {
                response = Translation.Get("permission");
                return false;
            }

            if (Plugin.Instance?.GameHandler == null)
            {
                response = Translation.Get("not_initialized");
                return false;
            }

            if (arguments.Count == 0)
            {
                return Start(out response);
            }

            string subcommand = GetArgument(arguments, 0).ToLowerInvariant();
            if (subcommand == "start")
                return Start(out response);

            if (subcommand == "stop")
            {
                if (!Plugin.Instance.GameHandler.IsActive)
                {
                    response = Translation.Get("not_running");
                    return false;
                }

                Plugin.Instance.GameHandler.StopGame();
                response = Translation.Get("stopped");
                return true;
            }

            if (subcommand == "settings")
                return Configure(arguments, out response);

            response = Translation.Get("command_usage");
            return false;
        }

        private static bool Start(out string response)
        {
            if (Plugin.Instance.GameHandler.IsActive)
            {
                response = Translation.Get("already_running");
                return false;
            }

            if (!Plugin.Instance.GameHandler.StartGame())
            {
                response = Translation.Get("start_failed");
                return false;
            }

            response = Translation.Get("started");
            return true;
        }

        private static bool Configure(ArraySegment<string> arguments, out string response)
        {
            if (arguments.Count == 1)
            {
                Config current = Plugin.Instance.Config;
                response = $"{Translation.Get("settings")}: language={Translation.GetLanguageName(current.Language)}, dummies={current.DummiesPerPlayer}, maxdummies={current.MaxDummies}, spawnattempts={current.NpcSpawnAttempts}, penaltypercent={current.BotHitPenalty}, duration={current.RoundDurationSeconds}, followchance={current.FollowChance}, walkspeed={current.NpcWalkSpeed}, followspeed={current.NpcFollowSpeed}, turnspeed={current.NpcTurnSpeed}, pausechance={current.PauseChance}, pausemin={current.PauseMinSeconds}, pausemax={current.PauseMaxSeconds}, jumpchance={current.JumpChance}, radius={current.ArenaRadius}, debug={current.Debug}. {Translation.Get("settings_change")}";
                return true;
            }

            if (arguments.Count < 3)
            {
                response = Translation.Get("settings_usage");
                return false;
            }

            Config config = Plugin.Instance.Config;
            string name = GetArgument(arguments, 1).ToLowerInvariant();
            string value = GetArgument(arguments, 2);

            if (name == "language" || name == "lang" || name == "язык" || name == "мова")
            {
                if (!Translation.TryParseLanguage(value, out PluginLanguage language))
                {
                    response = Translation.Get("language_invalid");
                    return false;
                }

                config.Language = language;
                response = Translation.Get("language_updated", Translation.GetLanguageName(language));
                return true;
            }

            if (name == "dummies" && int.TryParse(value, out int dummies))
                config.DummiesPerPlayer = dummies;
            else if (name == "maxdummies" && int.TryParse(value, out int maxDummies))
                config.MaxDummies = maxDummies;
            else if (name == "spawnattempts" && int.TryParse(value, out int spawnAttempts))
                config.NpcSpawnAttempts = spawnAttempts;
            else if ((name == "penalty" || name == "penaltypercent") && float.TryParse(value, out float penalty))
                config.BotHitPenalty = penalty;
            else if (name == "duration" && int.TryParse(value, out int duration))
                config.RoundDurationSeconds = duration;
            else if (name == "followchance" && float.TryParse(value, out float followChance))
                config.FollowChance = followChance;
            else if (name == "walkspeed" && float.TryParse(value, out float walkSpeed))
                config.NpcWalkSpeed = walkSpeed;
            else if (name == "followspeed" && float.TryParse(value, out float followSpeed))
                config.NpcFollowSpeed = followSpeed;
            else if (name == "turnspeed" && float.TryParse(value, out float turnSpeed))
                config.NpcTurnSpeed = turnSpeed;
            else if (name == "pausechance" && float.TryParse(value, out float pauseChance))
                config.PauseChance = pauseChance;
            else if (name == "pausemin" && float.TryParse(value, out float pauseMin))
                config.PauseMinSeconds = pauseMin;
            else if (name == "pausemax" && float.TryParse(value, out float pauseMax))
                config.PauseMaxSeconds = pauseMax;
            else if (name == "jumpchance" && float.TryParse(value, out float jumpChance))
                config.JumpChance = jumpChance;
            else if (name == "radius" && float.TryParse(value, out float radius))
                config.ArenaRadius = radius;
            else if (name == "debug" && bool.TryParse(value, out bool debug))
                config.Debug = debug;
            else
            {
                response = Translation.Get("invalid_setting");
                return false;
            }

            config.Validate();
            response = Translation.Get("setting_updated", name);
            return true;
        }

        private static string GetArgument(ArraySegment<string> arguments, int index)
        {
            return arguments.Array[arguments.Offset + index];
        }
    }
}
