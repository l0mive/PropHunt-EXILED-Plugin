namespace PropHuntMiniGame
{
    using System;
    using Exiled.API.Features;
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

        public override Version Version => new Version(1, 1, 0);

        public override Version RequiredExiledVersion => new Version(9, 14, 2);

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
