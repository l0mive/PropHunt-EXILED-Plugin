namespace PropHuntMiniGame
{
    using System;
    using System.ComponentModel;
    using Exiled.API.Interfaces;

    public class Config : IConfig
    {
        [Description("Whether the plugin is enabled.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether debug messages are printed to the console.")]
        public bool Debug { get; set; } = false;

        [Description("Plugin message language: English, Russian, or Ukrainian.")]
        public PluginLanguage Language { get; set; } = PluginLanguage.Russian;

        [Description("How many dummy NPCs spawn per real target player.")]
        public int DummiesPerPlayer { get; set; } = 3;

        [Description("Maximum number of dummy NPCs that may be spawned by one PropHunt game or test.")]
        public int MaxDummies { get; set; } = 60;

        [Description("How many times the plugin retries creating an NPC when EXILED returns null.")]
        public int NpcSpawnAttempts { get; set; } = 4;

        [Description("Base percentage of the hunter's maximum health removed for shooting a dummy. The actual percentage decreases as more NPCs are alive.")]
        public float BotHitPenalty { get; set; } = 25f;

        [Description("Minigame duration in seconds.")]
        public int RoundDurationSeconds { get; set; } = 180;

        [Description("Display nickname applied to every participant and NPC during the event.")]
        public string UniformNickname { get; set; } = "Subject";

        [Description("CustomInfo / prefix shown when aiming at a participant or NPC.")]
        public string UniformCustomInfo { get; set; } = "Тестируемый";

        [Description("Chance (0-1) that an NPC starts following a nearby player.")]
        public float FollowChance { get; set; } = 0.18f;

        [Description("Walking speed of wandering NPCs in meters per second.")]
        public float NpcWalkSpeed { get; set; } = 1.75f;

        [Description("Walking speed of NPCs that are following a player in meters per second.")]
        public float NpcFollowSpeed { get; set; } = 2.25f;

        [Description("Maximum NPC turning speed in degrees per second.")]
        public float NpcTurnSpeed { get; set; } = 75f;

        [Description("Chance (0-1) that a wandering NPC pauses after choosing a new direction.")]
        public float PauseChance { get; set; } = 0.30f;

        [Description("Minimum duration of a random NPC pause in seconds.")]
        public float PauseMinSeconds { get; set; } = 0.45f;

        [Description("Maximum duration of a random NPC pause in seconds.")]
        public float PauseMaxSeconds { get; set; } = 1.35f;

        [Description("Chance (0-1) that an NPC jumps when changing behavior.")]
        public float JumpChance { get; set; } = 0.12f;

        [Description("How far NPCs may wander from the arena center.")]
        public float ArenaRadius { get; set; } = 18f;

        public void Validate()
        {
            DummiesPerPlayer = Math.Max(1, DummiesPerPlayer);
            MaxDummies = Math.Max(1, MaxDummies);
            NpcSpawnAttempts = Math.Max(1, NpcSpawnAttempts);
            BotHitPenalty = ClampPercent(BotHitPenalty);
            RoundDurationSeconds = Math.Max(10, RoundDurationSeconds);
            FollowChance = Clamp01(FollowChance);
            NpcWalkSpeed = Math.Max(0.2f, NpcWalkSpeed);
            NpcFollowSpeed = Math.Max(0.2f, NpcFollowSpeed);
            NpcTurnSpeed = Math.Max(5f, NpcTurnSpeed);
            PauseChance = Clamp01(PauseChance);
            PauseMinSeconds = Math.Max(0f, PauseMinSeconds);
            PauseMaxSeconds = Math.Max(PauseMinSeconds, PauseMaxSeconds);
            JumpChance = Clamp01(JumpChance);
            ArenaRadius = Math.Max(8f, ArenaRadius);
            UniformNickname = string.IsNullOrWhiteSpace(UniformNickname) ? "Subject" : UniformNickname.Trim();
            UniformCustomInfo = string.IsNullOrWhiteSpace(UniformCustomInfo) ? "Тестируемый" : UniformCustomInfo.Trim();
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float ClampPercent(float value)
        {
            return Math.Max(0f, Math.Min(100f, value));
        }

        public static bool TryNormalizeArena(string value, out string arena)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "049":
                case "scp049":
                    arena = "049";
                    return true;
                case "surface_gate":
                case "surface":
                    arena = "surface_gate";
                    return true;
                case "here":
                    arena = "here";
                    return true;
                default:
                    arena = null;
                    return false;
            }
        }
    }
}
