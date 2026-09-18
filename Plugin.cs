namespace PropHuntMiniGame
{
    using System;
    using System.Collections.Generic;
    using Exiled.API.Features;
    using Exiled.API.Interfaces;
    using Exiled.Loader;
    using PropHuntMiniGame.Handlers;

    public class Plugin : Plugin<Config>
    {
        private static readonly Plugin Singleton = new();

        private GameHandler gameHandler;

        private Plugin()
        {
        }

        public static Plugin Instance => Singleton;

        public GameHandler GameHandler => gameHandler;

        public override string Name => "PropHunt MiniGame";

        public override string Prefix => "PropHunt";

        public override string Author => "SCPPlugin";

        public override Version Version => new Version(0, 2, 1);

        public override Version RequiredExiledVersion => new Version(9, 14, 2);

        public bool SaveSettings()
        {
            try
            {
                var configs = new SortedDictionary<string, IConfig>(StringComparer.Ordinal);
                foreach (var loadedPlugin in Loader.Plugins)
                    configs[loadedPlugin.Prefix] = loadedPlugin.Config;

                return ConfigManager.Save(configs);
            }
            catch (Exception exception)
            {
                Log.Error($"Could not save PropHunt settings: {exception}");
                return false;
            }
        }

        public override void OnEnabled()
        {
            Config.Validate();
            gameHandler = new GameHandler(this);
            gameHandler.RegisterEvents();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            gameHandler?.UnregisterEvents();
            gameHandler?.StopGame();
            gameHandler = null;

            base.OnDisabled();
        }
    }
}
