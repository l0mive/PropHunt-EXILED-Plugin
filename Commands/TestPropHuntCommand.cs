namespace PropHuntMiniGame.Commands
{
    using System;
    using CommandSystem;
    using Exiled.API.Features;
    using Exiled.Permissions.Extensions;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class TestPropHuntCommand : ICommand
    {
        public string Command => "test_prophunt";

        public string[] Aliases => new[] { "testprophunt" };

        public string Description => "Starts a solo PropHunt test with a selected number of dummies.";

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

            if (Plugin.Instance.GameHandler.IsActive)
            {
                response = Translation.Get("already_running");
                return false;
            }

            int maxDummies = Plugin.Instance.Config.MaxDummies;
            if (arguments.Count != 2 || !int.TryParse(GetArgument(arguments, 0), out int dummyCount) || dummyCount < 1 || dummyCount > maxDummies)
            {
                response = Translation.Get("test_usage_with_arena", maxDummies);
                return false;
            }

            if (!Config.TryNormalizeArena(GetArgument(arguments, 1), out string arena))
            {
                response = Translation.Get("arena_invalid");
                return false;
            }

            if (!Plugin.Instance.GameHandler.StartTest(dummyCount, arena, Player.Get(sender)))
            {
                response = Plugin.Instance.GameHandler.LastStartError ?? Translation.Get("test_failed");
                return false;
            }

            response = Translation.Get("test_started", dummyCount);
            return true;
        }

        private static string GetArgument(ArraySegment<string> arguments, int index)
        {
            return arguments.Array[arguments.Offset + index];
        }
    }
}
