namespace PropHuntMiniGame.Handlers
{
    using System.Collections.Generic;
    using System.Linq;
    using Exiled.API.Enums;
    using Exiled.API.Extensions;
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
        private const float SpawnFloorLevelTolerance = 1.25f;
        private const float SpawnClearanceRadius = 0.35f;
        private readonly Plugin plugin;
        private readonly HashSet<Player> hunters = new();
        private readonly HashSet<Player> targets = new();
        private readonly List<DummyController> dummyControllers = new();
        private readonly Dictionary<Player, IdentitySnapshot> identitySnapshots = new();
        private readonly Dictionary<Door, DoorSnapshot> doorLockSnapshots = new();
        private readonly List<Lift> lockedLifts = new();
        private CoroutineHandle roundTimerHandle;
        private CoroutineHandle pendingStartHandle;
        private Room arenaRoom;
        private string activeArena;

        public GameHandler(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public bool IsActive { get; private set; }

        public bool IsSoloTest { get; private set; }

        public string LastStartError { get; private set; }

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

        public bool StartGame(string arena, Player initiator)
        {
            return StartGame(null, false, arena, initiator);
        }

        public bool StartTest(int dummyCount, string arena, Player initiator = null)
        {
            return StartGame(dummyCount, true, arena, initiator);
        }

        private bool StartGame(int? exactDummyCount, bool isTest, string arena, Player initiator)
        {
            if (IsActive)
                return false;

            LastStartError = null;

            if (!Config.TryNormalizeArena(arena, out activeArena))
            {
                LastStartError = Translation.Get("arena_invalid");
                return false;
            }

            List<Player> participants = Player.List
                .Where(p => p != null && !p.IsNPC && p.IsAlive)
                .ToList();

            if (participants.Count < 2 && !(isTest && participants.Count == 1))
            {
                activeArena = null;
                return false;
            }

            try
            {
                IsActive = true;
                IsSoloTest = isTest;
                hunters.Clear();
                targets.Clear();
                identitySnapshots.Clear();
                doorLockSnapshots.Clear();
                lockedLifts.Clear();

                if (!PrepareArena(initiator))
                {
                    IsActive = false;
                    activeArena = null;
                    return false;
                }

                LockArena();

                pendingStartHandle = Timing.RunCoroutine(DelayedStart(participants, exactDummyCount, isTest));

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

            if (pendingStartHandle.IsRunning)
                Timing.KillCoroutines(pendingStartHandle);

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
            activeArena = null;

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

        private bool PrepareArena(Player initiator)
        {
            switch (activeArena)
            {
                case "here":
                    if (initiator == null || !initiator.IsConnected || !initiator.IsAlive)
                    {
                        LastStartError = Translation.Get("arena_here_requires_player");
                        return false;
                    }

                    ArenaCenter = initiator.Position;
                    arenaRoom = Room.Get(ArenaCenter);
                    return true;

                case "049":
                    arenaRoom = Room.Get(RoomType.Hcz049);
                    if (arenaRoom == null)
                    {
                        LastStartError = Translation.Get("arena_unavailable");
                        return false;
                    }

                    return TrySetSafeFloorNearDoor(Door.Get(DoorType.Scp049Armory), room => room.Type == RoomType.Hcz049)
                        || TrySetArenaCenterFromSpawnLocation(SpawnLocationType.Inside049Armory, room => room.Type == RoomType.Hcz049)
                        || TrySetRoomFloorCenter(arenaRoom);

                case "surface_gate":
                    arenaRoom = Room.Get(RoomType.Surface) ?? Room.List.FirstOrDefault(room => room.Zone == ZoneType.Surface);
                    return TrySetSafeFloorNearDoor(Door.Get(DoorType.SurfaceGate), room => room.Zone == ZoneType.Surface)
                        || TrySetRoomFloorCenter(arenaRoom);

                default:
                    LastStartError = Translation.Get("arena_unavailable");
                    return false;
            }

        }

        private bool TrySetRoomFloorCenter(Room room)
        {
            if (room == null)
            {
                LastStartError = Translation.Get("arena_unavailable");
                return false;
            }

            Vector3[] localOffsets =
            {
                Vector3.zero,
                Vector3.forward * 4f,
                Vector3.back * 4f,
                Vector3.left * 4f,
                Vector3.right * 4f,
            };

            foreach (Vector3 offset in localOffsets)
            {
                Vector3 candidate = room.WorldPosition(offset);
                if (!TryGetFloorPosition(candidate, out Vector3 floorPosition))
                {
                    continue;
                }

                Room floorRoom = Room.Get(floorPosition);
                if (floorRoom == null || floorRoom.Type != room.Type)
                    continue;

                ArenaCenter = floorPosition;
                arenaRoom = room;
                return true;
            }

            LastStartError = Translation.Get("arena_unavailable");
            return false;
        }

        private bool TrySetArenaCenterFromSpawnLocation(SpawnLocationType spawnLocation, System.Func<Room, bool> belongsToArena)
        {
            Vector3 spawnPosition = spawnLocation.GetPosition();
            if (spawnPosition == Vector3.zero || !TryGetFloorPosition(spawnPosition, out Vector3 floorPosition))
                return false;

            Room floorRoom = Room.Get(floorPosition);
            if (floorRoom == null || !belongsToArena(floorRoom))
                return false;

            ArenaCenter = floorPosition;
            arenaRoom ??= floorRoom;
            return true;
        }

        private bool TrySetSafeFloorNearDoor(Door door, System.Func<Room, bool> belongsToArena)
        {
            if (door == null)
                return false;

            Vector3 forward = Vector3.ProjectOnPlane(door.Transform.forward, Vector3.up).normalized;
            if (forward == Vector3.zero)
                forward = Vector3.forward;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3[] directions = { forward, -forward, right, -right };
            float[] distances = { 3.5f, 5f, 6.5f, 8f };

            foreach (float distance in distances)
            {
                foreach (Vector3 direction in directions)
                {
                    Vector3 candidate = door.Position + (direction * distance);
                    if (!TryGetFloorPosition(candidate, out Vector3 floorPosition))
                        continue;

                    Room floorRoom = Room.Get(floorPosition);
                    if (floorRoom == null || !belongsToArena(floorRoom))
                        continue;

                    ArenaCenter = floorPosition;
                    arenaRoom ??= floorRoom;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetFloorPosition(Vector3 candidate, out Vector3 floorPosition)
        {
            if (Physics.Raycast(candidate + (Vector3.up * 2.5f), Vector3.down, out RaycastHit hit, 5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                floorPosition = hit.point + (Vector3.up * 0.05f);
                return HasSpawnClearance(floorPosition);
            }

            floorPosition = default;
            return false;
        }

        private static bool HasSpawnClearance(Vector3 floorPosition)
        {
            Vector3 lowerPoint = floorPosition + (Vector3.up * 0.35f);
            Vector3 upperPoint = floorPosition + (Vector3.up * 1.55f);
            return !Physics.CheckCapsule(
                lowerPoint,
                upperPoint,
                SpawnClearanceRadius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
        }

        private IEnumerator<float> DelayedStart(List<Player> requestedParticipants, int? exactDummyCount, bool isTest)
        {
            yield return Timing.WaitForSeconds(1f);
            pendingStartHandle = default;

            if (!IsActive)
                yield break;

            List<Player> participants = requestedParticipants
                .Where(player => player != null && player.IsConnected && player.IsAlive && !player.IsNPC)
                .ToList();

            if (participants.Count < 2 && !(isTest && participants.Count == 1))
            {
                StopGame();
                yield break;
            }

            try
            {
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
            }
            catch (System.Exception exception)
            {
                Log.Error(Translation.Get("start_error", exception));
                StopGame();
            }
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
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float angle = (index * 47f) + (attempt * 61f);
                float radius = 1.5f + ((index + attempt) % 5) * 1.1f;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                Vector3 candidate = ArenaCenter + offset;

                if (TryGetFloorPosition(candidate, out Vector3 floorPosition)
                    && Mathf.Abs(floorPosition.y - ArenaCenter.y) <= SpawnFloorLevelTolerance
                    && IsArenaFloor(floorPosition))
                    return floorPosition + (Vector3.up * 1.2f);
            }

            return ArenaCenter + (Vector3.up * 1.2f);
        }

        private bool IsArenaFloor(Vector3 position)
        {
            Room room = Room.Get(position);
            if (room == null)
                return false;

            return activeArena switch
            {
                "049" => room.Type == RoomType.Hcz049,
                "surface_gate" => room.Zone == ZoneType.Surface,
                _ => arenaRoom == null || room.Type == arenaRoom.Type,
            };
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
            if (activeArena == "049")
            {
                LockContainmentArena();
                return;
            }

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

        private void LockContainmentArena()
        {
            if (arenaRoom?.Doors == null)
                return;

            Lift scp049Lift = Lift.Get(ElevatorType.Scp049);
            if (scp049Lift != null)
                LockLift(scp049Lift);

            foreach (Door door in arenaRoom.Doors)
            {
                if (scp049Lift?.Doors != null && scp049Lift.Doors.Contains(door))
                    LockDoor(door);
                else
                    OpenAndLockDoor(door);
            }

            OpenAndLockDoor(Door.Get(DoorType.Scp049Gate));
            OpenAndLockDoor(Door.Get(DoorType.Scp173NewGate));
        }

        private void LockSurfaceExits()
        {
            foreach (Lift lift in Lift.List)
            {
                if (lift.Type != ElevatorType.GateA && lift.Type != ElevatorType.GateB)
                    continue;

                LockLift(lift);
            }

            foreach (Door door in Door.List)
            {
                if (IsLiftDoor(door))
                    continue;

                if (IsSurfaceDoor(door))
                    OpenAndLockDoor(door);
            }

            OpenAndLockDoor(Door.Get(DoorType.SurfaceGate));
        }

        private bool IsLiftDoor(Door door)
        {
            return door != null && lockedLifts.Any(lift => lift?.Doors != null && lift.Doors.Contains(door));
        }

        private static bool IsSurfaceDoor(Door door)
        {
            return door != null
                && (door.Type == DoorType.SurfaceGate
                    || (door.Rooms != null && door.Rooms.Any(room => room != null && room.Zone == ZoneType.Surface)));
        }

        private void LockDoor(Door door)
        {
            if (door == null || doorLockSnapshots.ContainsKey(door))
                return;

            doorLockSnapshots[door] = new DoorSnapshot(door);
            door.IsOpen = false;
            door.ChangeLock(DoorLockType.Isolation);
        }

        private void OpenAndLockDoor(Door door)
        {
            if (door == null || doorLockSnapshots.ContainsKey(door))
                return;

            doorLockSnapshots[door] = new DoorSnapshot(door);
            door.ChangeLock(DoorLockType.Isolation);
            door.IsOpen = true;
        }

        private void LockLift(Lift lift)
        {
            if (lift == null || lockedLifts.Contains(lift))
                return;

            lockedLifts.Add(lift);
            lift.ChangeLock(DoorLockReason.AdminCommand);

            if (lift.Doors == null)
                return;

            foreach (Door door in lift.Doors)
                LockDoor(door);
        }

        private void UnlockArena()
        {
            foreach (Lift lift in lockedLifts)
            {
                if (lift != null)
                    lift.ChangeLock(DoorLockReason.None);
            }

            lockedLifts.Clear();

            foreach (KeyValuePair<Door, DoorSnapshot> pair in doorLockSnapshots.ToList())
            {
                Door door = pair.Key;
                if (door == null)
                    continue;

                door.Unlock();

                if (pair.Value.LockType != DoorLockType.None)
                    door.ChangeLock(pair.Value.LockType);

                door.IsOpen = pair.Value.IsOpen;
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

        private readonly struct DoorSnapshot
        {
            public DoorSnapshot(Door door)
            {
                LockType = door.DoorLockType;
                IsOpen = door.IsOpen;
            }

            public DoorLockType LockType { get; }

            public bool IsOpen { get; }
        }
    }
}
