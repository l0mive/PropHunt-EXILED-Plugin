namespace PropHuntMiniGame.Handlers
{
    using System.Collections.Generic;
    using System.Linq;
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Doors;
    using Exiled.Events.EventArgs.Player;
    using Exiled.Events.EventArgs.Server;
    using Interactables.Interobjects.DoorUtils;
    using MEC;
    using PlayerRoles;
    using PropHuntMiniGame.Components;
    using UnityEngine;

    public class GameHandler
    {
        private readonly Plugin plugin;
        private readonly HashSet<Player> hunters = new();
        private readonly HashSet<Player> targets = new();
        private readonly List<DummyController> dummyControllers = new();
        private readonly Dictionary<Player, IdentitySnapshot> identitySnapshots = new();
        private readonly Dictionary<Door, DoorLockType> doorLockSnapshots = new();
        private readonly List<Lift> lockedLifts = new();
        private CoroutineHandle roundTimerHandle;
        private Room arenaRoom;

        public GameHandler(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public bool IsActive { get; private set; }

        public bool IsSoloTest { get; private set; }

        public Config Config => plugin.Config;

        public Vector3 ArenaCenter { get; private set; }

        public float ArenaRadius => Mathf.Max(8f, plugin.Config.ArenaRadius);

        public void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.InteractingElevator += OnInteractingElevator;
        }

        public void UnregisterEvents()
        {
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.InteractingElevator -= OnInteractingElevator;
        }

        public bool StartGame()
        {
            return StartGame(null, false);
        }

        public bool StartTest(int dummyCount)
        {
            return StartGame(dummyCount, true);
        }

        private bool StartGame(int? exactDummyCount, bool isTest)
        {
            if (IsActive)
                return false;

            List<Player> participants = Player.List
                .Where(p => p != null && !p.IsNPC && p.IsAlive)
                .ToList();

            if (participants.Count < 2 && !(isTest && participants.Count == 1))
                return false;

            try
            {
                IsActive = true;
                IsSoloTest = isTest;
                hunters.Clear();
                targets.Clear();
                identitySnapshots.Clear();
                doorLockSnapshots.Clear();
                lockedLifts.Clear();

                PrepareArena();

                Player hunter = participants[Random.Range(0, participants.Count)];
                hunters.Add(hunter);

                foreach (Player player in participants)
                {
                    if (player == hunter)
                        SetupHunter(player);
                    else
                        SetupTarget(player);
                }

                TeleportParticipants(participants);
                LockArena();
                SpawnDummies(exactDummyCount);

                foreach (Player player in participants)
                    ApplyUniformIdentity(player);

                string mode = isTest ? Translation.Get("mode_test") : Translation.Get("mode_game");
                Map.Broadcast(5, Translation.Get("game_started", mode));

                if (roundTimerHandle.IsRunning)
                    Timing.KillCoroutines(roundTimerHandle);

                roundTimerHandle = Timing.RunCoroutine(RoundTimer());

                if (plugin.Config.Debug)
                    Log.Debug($"PropHunt started with {hunters.Count} hunter(s), {targets.Count} target(s), {dummyControllers.Count} requested dummies at {ArenaCenter}. Test: {isTest}.");

                return true;
            }
            catch (System.Exception exception)
            {
                Log.Error(Translation.Get("start_error", exception));
                StopGame();
                return false;
            }
        }

        public void StopGame()
        {
            if (!IsActive && dummyControllers.Count == 0 && identitySnapshots.Count == 0)
                return;

            IsActive = false;

            if (roundTimerHandle.IsRunning)
                Timing.KillCoroutines(roundTimerHandle);

            foreach (DummyController controller in dummyControllers.ToList())
            {
                controller.Stop();

                if (controller.Npc != null && controller.Npc.IsConnected)
                    controller.Npc.Destroy();
            }

            dummyControllers.Clear();
            RestoreIdentities();
            UnlockArena();

            hunters.Clear();
            targets.Clear();
            IsSoloTest = false;
            arenaRoom = null;

            if (plugin.Config.Debug)
                Log.Debug("PropHunt Test stopped and cleaned up.");
        }

        public Player GetFollowCandidate(Npc npc)
        {
            if (npc == null)
                return null;

            List<Player> candidates = Player.List
                .Where(p => p != null && p.IsConnected && p.IsAlive && !p.IsNPC && p != npc)
                .Where(p => Vector3.Distance(p.Position, npc.Position) < 18f)
                .ToList();

            if (candidates.Count == 0)
                return null;

            List<Player> huntersNearby = candidates.Where(p => hunters.Contains(p)).ToList();
            if (huntersNearby.Count > 0 && Random.value < 0.45f)
                return huntersNearby[Random.Range(0, huntersNearby.Count)];

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void PrepareArena()
        {
            arenaRoom = Room.Get(RoomType.Surface);
            if (arenaRoom == null)
                arenaRoom = Room.List.FirstOrDefault(r => r.Zone == ZoneType.Surface);

            if (arenaRoom == null)
                arenaRoom = Room.List.OrderByDescending(r => r.Doors?.Count() ?? 0).FirstOrDefault();

            ArenaCenter = arenaRoom != null ? arenaRoom.Position : Vector3.zero;
        }

        private void SetupHunter(Player player)
        {
            player.Role.Set(RoleTypeId.FacilityGuard, SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
            player.ShowHint(Translation.Get("hunter_hint"), 8f);
        }

        private void SetupTarget(Player player)
        {
            targets.Add(player);
            player.Role.Set(RoleTypeId.ClassD, SpawnReason.ForceClass, RoleSpawnFlags.None);
            player.ClearInventory(destroy: true);
            player.ShowHint(Translation.Get("target_hint"), 8f);
        }

        private void TeleportParticipants(List<Player> participants)
        {
            int index = 0;
            foreach (Player player in participants)
            {
                if (player == null || !player.IsConnected)
                    continue;

                player.Position = GetArenaSpawnPoint(index);
                index++;
            }
        }

        private Vector3 GetArenaSpawnPoint(int index)
        {
            float angle = index * 47f;
            float radius = 3f + (index % 5);
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
            return ArenaCenter + offset + (Vector3.up * 1.2f);
        }

        private void SpawnDummies(int? exactDummyCount)
        {
            int requestedCount = exactDummyCount ?? (targets.Count * plugin.Config.DummiesPerPlayer);
            requestedCount = Mathf.Clamp(requestedCount, 0, plugin.Config.MaxDummies);

            for (int dummyIndex = 0; dummyIndex < requestedCount; dummyIndex++)
            {
                Vector3 spawnPosition = GetArenaSpawnPoint(20 + dummyIndex);
                SpawnDummy(spawnPosition, 0);
            }
        }

        private void SpawnDummy(Vector3 spawnPosition, int attempt)
        {
            if (!IsActive)
                return;

            Npc dummy = null;

            try
            {
                // Explicitly ignore NPCs in round-end checks. Without this, a solo test can end before NPCs finish initializing.
                dummy = Npc.Spawn(plugin.Config.UniformNickname, RoleTypeId.ClassD, ignored: true, position: spawnPosition);
            }
            catch (System.Exception exception)
            {
                Log.Error(Translation.Get("npc_spawn_error", attempt + 1, exception.Message));
            }

            if (dummy == null)
            {
                if (attempt + 1 < plugin.Config.NpcSpawnAttempts)
                    Timing.CallDelayed(0.5f, () => SpawnDummy(spawnPosition, attempt + 1));
                else
                    Log.Error(Translation.Get("npc_create_error", plugin.Config.NpcSpawnAttempts));

                return;
            }

            DummyController controller = new DummyController(dummy, this);
            dummyControllers.Add(controller);
            StartDummyWhenReady(dummy, controller, spawnPosition, 0);
        }

        private void StartDummyWhenReady(Npc dummy, DummyController controller, Vector3 spawnPosition, int attempt)
        {
            Timing.CallDelayed(Mathf.Max(0.5f, Npc.SpawnSetRoleDelay + 0.25f), () =>
            {
                if (!IsActive)
                    return;

                if (dummy == null)
                {
                    dummyControllers.Remove(controller);
                    SpawnDummy(spawnPosition, attempt);
                    return;
                }

                if (!dummy.IsAlive)
                {
                    if (attempt + 1 < plugin.Config.NpcSpawnAttempts)
                    {
                        StartDummyWhenReady(dummy, controller, spawnPosition, attempt + 1);
                        return;
                    }

                    dummyControllers.Remove(controller);
                    if (dummy.IsConnected)
                        dummy.Destroy();

                    Log.Error(Translation.Get("npc_initialize_error", plugin.Config.NpcSpawnAttempts));
                    return;
                }

                dummy.Position = spawnPosition;
                ApplyUniformIdentity(dummy);
                controller.Start();

                if (plugin.Config.Debug)
                    Log.Debug($"PropHunt dummy initialized at {spawnPosition}.");
            });
        }

        private void ApplyUniformIdentity(Player player)
        {
            if (player == null || !player.IsConnected)
                return;

            if (!player.IsNPC && !identitySnapshots.ContainsKey(player))
            {
                identitySnapshots[player] = new IdentitySnapshot(player);
            }

            player.CustomName = plugin.Config.UniformNickname;
            player.DisplayNickname = plugin.Config.UniformNickname;
            player.CustomInfo = plugin.Config.UniformCustomInfo;
            player.RankName = plugin.Config.UniformCustomInfo;
            player.RankColor = "default";
            player.BadgeHidden = true;
            player.InfoArea = (player.InfoArea | PlayerInfoArea.Nickname | PlayerInfoArea.CustomInfo) & ~PlayerInfoArea.Badge;
        }

        private void RestoreIdentities()
        {
            foreach (KeyValuePair<Player, IdentitySnapshot> pair in identitySnapshots.ToList())
            {
                if (pair.Key != null && pair.Key.IsConnected)
                    pair.Value.Restore(pair.Key);
            }

            identitySnapshots.Clear();
        }

        private void LockArena()
        {
            bool surfaceArena = arenaRoom != null && (arenaRoom.Type == RoomType.Surface || arenaRoom.Zone == ZoneType.Surface);

            if (surfaceArena)
            {
                LockSurfaceExits();
                return;
            }

            if (arenaRoom?.Doors == null)
                return;

            foreach (Door door in arenaRoom.Doors)
                LockDoor(door);
        }

        private void LockSurfaceExits()
        {
            foreach (Lift lift in Lift.List)
            {
                if (lift.Type != ElevatorType.GateA && lift.Type != ElevatorType.GateB)
                    continue;

                lockedLifts.Add(lift);
                lift.ChangeLock(DoorLockReason.AdminCommand);

                if (lift.Doors == null)
                    continue;

                foreach (Door door in lift.Doors)
                    LockDoor(door);
            }

            foreach (Door door in Door.List)
            {
                if (door.Type == DoorType.GateA
                    || door.Type == DoorType.GateB
                    || door.Type == DoorType.ElevatorGateA
                    || door.Type == DoorType.ElevatorGateB)
                {
                    LockDoor(door);
                    door.IsOpen = false;
                }
            }

            if (arenaRoom?.Doors == null)
                return;

            foreach (Door door in arenaRoom.Doors)
                LockDoor(door);
        }

        private void LockDoor(Door door)
        {
            if (door == null || doorLockSnapshots.ContainsKey(door))
                return;

            doorLockSnapshots[door] = door.DoorLockType;
            door.IsOpen = false;
            door.ChangeLock(DoorLockType.Isolation);
        }

        private void UnlockArena()
        {
            foreach (Lift lift in lockedLifts)
            {
                if (lift != null)
                    lift.ChangeLock(DoorLockReason.None);
            }

            lockedLifts.Clear();

            foreach (KeyValuePair<Door, DoorLockType> pair in doorLockSnapshots.ToList())
            {
                Door door = pair.Key;
                if (door == null)
                    continue;

                door.Unlock();

                if (pair.Value != DoorLockType.None)
                    door.ChangeLock(pair.Value);
            }

            doorLockSnapshots.Clear();
        }

        private IEnumerator<float> RoundTimer()
        {
            yield return Timing.WaitForSeconds(plugin.Config.RoundDurationSeconds);
            EndGame(Translation.Get("time_up"));
        }

        private void EndGame(string message)
        {
            if (!IsActive)
                return;

            Map.Broadcast(5, $"<color=yellow>{message}</color>");
            StopGame();
        }

        private void OnRoundStarted()
        {
            if (plugin.Config.Debug)
                Log.Debug("Round started.");
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            StopGame();
        }

        private void OnWaitingForPlayers()
        {
            StopGame();
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (!IsActive || ev.Door == null)
                return;

            if (doorLockSnapshots.ContainsKey(ev.Door))
                ev.IsAllowed = false;
        }

        private void OnInteractingElevator(InteractingElevatorEventArgs ev)
        {
            if (!IsActive)
                return;

            if (ev.Lift != null && lockedLifts.Contains(ev.Lift))
                ev.IsAllowed = false;
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (!IsActive || ev.Attacker == null || ev.Player == null)
                return;

            if (!hunters.Contains(ev.Attacker))
                return;

            if (ev.Player.IsNPC)
            {
                ev.IsAllowed = false;
                ApplyBotHitPenalty(ev.Attacker);
                return;
            }

            if (targets.Contains(ev.Player))
            {
                ev.Amount = ev.Player.MaxHealth;
                ev.Attacker.ShowHint(Translation.Get("correct_target", plugin.Config.UniformNickname), 3f);
            }
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (!IsActive || ev.Player == null)
                return;

            if (ev.Player.IsNPC)
            {
                ev.IsAllowed = false;
                return;
            }

            if (hunters.Contains(ev.Player))
            {
                hunters.Remove(ev.Player);
                Timing.CallDelayed(0f, CheckHunterWinCondition);
                return;
            }

            if (targets.Contains(ev.Player))
            {
                targets.Remove(ev.Player);
                Map.Broadcast(3, Translation.Get("target_eliminated", plugin.Config.UniformNickname));

                if (targets.Count == 0)
                    EndGame(Translation.Get("all_targets"));
            }
        }

        private void ApplyBotHitPenalty(Player hunter)
        {
            int activeDummies = GetActiveDummyCount();
            float penaltyPercent = plugin.Config.BotHitPenalty / Mathf.Sqrt(Mathf.Max(1, activeDummies));
            float damage = hunter.MaxHealth * (penaltyPercent / 100f);
            float remainingHealth = hunter.Health - damage;

            if (remainingHealth <= 0f)
            {
                // Keep the player alive until the role changes so the transition to spectator is reliable.
                hunter.Health = 1f;
                hunter.ShowHint(Translation.Get("hunter_eliminated"), 4f);
                hunters.Remove(hunter);
                hunter.Role.Set(RoleTypeId.Spectator, SpawnReason.ForceClass, RoleSpawnFlags.None);
                CheckHunterWinCondition();
                return;
            }

            hunter.Health = remainingHealth;
            hunter.ShowHint(Translation.Get("wrong_target_percent", damage.ToString("0.#"), penaltyPercent.ToString("0.#"), activeDummies), 3f);
        }

        private int GetActiveDummyCount()
        {
            return Mathf.Max(1, dummyControllers.Count(controller => controller.Npc != null && controller.Npc.IsConnected && controller.Npc.IsAlive));
        }

        private void CheckHunterWinCondition()
        {
            if (!IsActive || hunters.Count > 0)
                return;

            EndGame(targets.Count > 0 ? Translation.Get("hiders_win") : Translation.Get("test_hunters_eliminated"));
        }

        private sealed class IdentitySnapshot
        {
            private readonly string customName;
            private readonly string displayNickname;
            private readonly string customInfo;
            private readonly string rankName;
            private readonly string rankColor;
            private readonly bool badgeHidden;
            private readonly PlayerInfoArea infoArea;

            public IdentitySnapshot(Player player)
            {
                customName = player.CustomName;
                displayNickname = player.DisplayNickname;
                customInfo = player.CustomInfo;
                rankName = player.RankName;
                rankColor = player.RankColor;
                badgeHidden = player.BadgeHidden;
                infoArea = player.InfoArea;
            }

            public void Restore(Player player)
            {
                player.CustomName = customName;
                player.DisplayNickname = displayNickname;
                player.CustomInfo = customInfo;
                player.RankName = rankName;
                player.RankColor = rankColor;
                player.BadgeHidden = badgeHidden;
                player.InfoArea = infoArea;
            }
        }
    }
}
