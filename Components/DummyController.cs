namespace PropHuntMiniGame.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Exiled.API.Features;
    using Exiled.API.Features.Roles;
    using MEC;
    using PropHuntMiniGame.Handlers;
    using UnityEngine;

    public class DummyController
    {
        private static readonly System.Random RandomSource = new();

        private readonly Npc npc;
        private readonly GameHandler gameHandler;
        private CoroutineHandle behaviorHandle;
        private Vector3 moveDirection = Vector3.forward;
        private Vector3 desiredDirection = Vector3.forward;
        private Player followTarget;
        private float followUntil;
        private float nextDecisionAt;
        private float pauseUntil;
        private float nextJumpAt;
        private float currentSpeed;
        private float blockedSince = -1f;
        private float escapeUntil;
        private float nextEscapeRepathAt;
        private Vector3 escapeDirection = Vector3.forward;
        private Vector3 lastWallNormal;
        private bool touchedWall;
        private int consecutiveWallTouches;

        public DummyController(Npc npc, GameHandler gameHandler)
        {
            this.npc = npc;
            this.gameHandler = gameHandler;
        }

        public Npc Npc => npc;

        public void Start()
        {
            behaviorHandle = Timing.RunCoroutine(BehaviorLoop());
        }

        public void Stop()
        {
            followTarget = null;

            if (behaviorHandle.IsRunning)
                Timing.KillCoroutines(behaviorHandle);
        }

        private IEnumerator<float> BehaviorLoop()
        {
            while (npc != null && npc.IsAlive && gameHandler.IsActive)
            {
                bool isEscaping = Time.time < escapeUntil;
                if (!isEscaping && Time.time >= nextDecisionAt)
                {
                    UpdateFollowState();
                    ChooseLookAndMove();
                    UpdatePauseState();
                    TryJump();
                    nextDecisionAt = Time.time + 1.1f + ((float)RandomSource.NextDouble() * 2.2f);
                }

                Vector3 steering = isEscaping ? escapeDirection : GetSteeringDirection();
                float turnSpeed = isEscaping
                    ? Mathf.Max(gameHandler.Config.NpcTurnSpeed * 2.5f, 180f)
                    : gameHandler.Config.NpcTurnSpeed;
                moveDirection = Vector3.RotateTowards(
                    moveDirection,
                    steering,
                    Mathf.Deg2Rad * turnSpeed * Time.deltaTime,
                    0f).normalized;
                if (moveDirection == Vector3.zero)
                    moveDirection = Vector3.forward;

                RotateTowards(moveDirection, turnSpeed);

                bool isPaused = followTarget == null && Time.time < pauseUntil;
                float targetSpeed = isPaused ? 0f : (followTarget == null ? gameHandler.Config.NpcWalkSpeed : gameHandler.Config.NpcFollowSpeed);
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 2.2f * Time.deltaTime);
                Vector3 displacement = moveDirection * currentSpeed * Time.deltaTime;
                Vector3 positionBeforeMove = npc.Position;

                bool movedWithoutBlocking = TryMove(displacement);
                if (!movedWithoutBlocking || (touchedWall && consecutiveWallTouches >= 2))
                {
                    if (isEscaping && Time.time >= nextEscapeRepathAt)
                        RepathWallEscape();
                    else if (!isEscaping)
                        BeginWallEscape();
                }

                UpdateBlockedMovement(displacement, positionBeforeMove);

                yield return Timing.WaitForOneFrame;
            }
        }

        private void UpdateFollowState()
        {
            if (followTarget != null && (Time.time >= followUntil || followTarget == null || !followTarget.IsAlive || !followTarget.IsConnected))
                followTarget = null;

            if (followTarget != null)
                return;

            if (RandomSource.NextDouble() > gameHandler.Config.FollowChance)
                return;

            Player candidate = gameHandler.GetFollowCandidate(npc);
            if (candidate == null)
                return;

            followTarget = candidate;
            followUntil = Time.time + 4f + ((float)RandomSource.NextDouble() * 5f);
        }

        private void ChooseLookAndMove()
        {
            if (followTarget != null && followTarget.IsAlive)
            {
                Vector3 toTarget = followTarget.Position - npc.Position;
                toTarget.y = 0f;
                desiredDirection = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.forward;
                return;
            }

            float yaw = npc.Rotation.eulerAngles.y + RandomSource.Next(-85, 86);

            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            int mode = RandomSource.Next(0, 3);

            if (mode == 0)
                desiredDirection = forward;
            else if (mode == 1)
                desiredDirection = RandomSource.Next(0, 2) == 0 ? right : -right;
            else
                desiredDirection = (forward + ((RandomSource.Next(0, 2) == 0 ? right : -right) * 0.6f)).normalized;
        }

        private void TryJump()
        {
            if (Time.time < nextJumpAt || RandomSource.NextDouble() > gameHandler.Config.JumpChance)
                return;

            if (npc.Role is FpcRole fpcRole && fpcRole.IsGrounded)
            {
                Vector3 velocity = fpcRole.Velocity;
                velocity.y = fpcRole.JumpingSpeed;
                fpcRole.Velocity = velocity;
                nextJumpAt = Time.time + 3.5f + ((float)RandomSource.NextDouble() * 4.5f);
            }
        }

        private void UpdatePauseState()
        {
            if (followTarget != null || RandomSource.NextDouble() > gameHandler.Config.PauseChance)
                return;

            float minimum = gameHandler.Config.PauseMinSeconds;
            float maximum = gameHandler.Config.PauseMaxSeconds;
            if (maximum <= 0f)
                return;

            pauseUntil = Time.time + minimum + ((float)RandomSource.NextDouble() * (maximum - minimum));
        }

        private void RotateTowards(Vector3 direction, float turnSpeed)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            npc.Rotation = Quaternion.RotateTowards(
                npc.Rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        private Vector3 ClampToArena(Vector3 direction)
        {
            Vector3 offset = npc.Position - gameHandler.ArenaCenter;
            offset.y = 0f;

            if (offset.magnitude < gameHandler.ArenaRadius * 0.85f)
                return direction;

            Vector3 inward = -offset.normalized;
            return (inward + (direction * 0.25f)).normalized;
        }

        private Vector3 GetSteeringDirection()
        {
            Vector3 steering = desiredDirection;
            steering += GetObstacleAvoidance() * 1.8f;
            steering += GetPlayerSeparation() * 1.1f;
            steering = ClampToArena(steering);
            return steering.sqrMagnitude > 0.001f ? steering.normalized : Vector3.forward;
        }

        private void UpdateBlockedMovement(Vector3 requestedDisplacement, Vector3 positionBeforeMove)
        {
            if (requestedDisplacement.sqrMagnitude < 0.0001f || Time.time < escapeUntil)
            {
                blockedSince = -1f;
                return;
            }

            Vector3 actualDisplacement = npc.Position - positionBeforeMove;
            actualDisplacement.y = 0f;
            float expectedDistance = requestedDisplacement.magnitude;

            if (actualDisplacement.magnitude >= expectedDistance * 0.4f)
            {
                blockedSince = -1f;
                return;
            }

            if (blockedSince < 0f)
                blockedSince = Time.time;

            if (Time.time - blockedSince >= 0.08f)
                BeginWallEscape();
        }

        private void BeginWallEscape()
        {
            if (Time.time < escapeUntil)
                return;

            escapeDirection = FindEscapeDirection();
            desiredDirection = escapeDirection;
            pauseUntil = 0f;
            followTarget = null;
            blockedSince = -1f;
            escapeUntil = Time.time + 0.8f + ((float)RandomSource.NextDouble() * 0.25f);
            nextEscapeRepathAt = Time.time + 0.10f;
            nextDecisionAt = escapeUntil + 0.1f;
        }

        private void RepathWallEscape()
        {
            escapeDirection = FindEscapeDirection();
            desiredDirection = escapeDirection;
            escapeUntil = Time.time + 0.7f;
            nextEscapeRepathAt = Time.time + 0.10f;
        }

        private Vector3 FindEscapeDirection()
        {
            Vector3 origin = npc.Position + Vector3.up;
            Vector3 awayFromWalls = lastWallNormal * 4f;
            float[] probeAngles = { -180f, -135f, -90f, -45f, 0f, 45f, 90f, 135f };

            foreach (float angle in probeAngles)
            {
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * moveDirection;
                if (!Physics.SphereCast(origin, 0.32f, direction, out RaycastHit hit, 2.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 normal = hit.normal;
                normal.y = 0f;
                if (normal.sqrMagnitude > 0.001f)
                    awayFromWalls += normal.normalized * (1f / Mathf.Max(0.2f, hit.distance));
            }

            if (awayFromWalls.sqrMagnitude < 0.001f)
                awayFromWalls = -moveDirection;

            Vector3 baseDirection = awayFromWalls.normalized;
            Vector3 bestDirection = baseDirection;
            float bestScore = float.MinValue;
            float[] candidateAngles = { 0f, -25f, 25f, -50f, 50f, -75f, 75f, -100f, 100f, 180f };

            foreach (float angle in candidateAngles)
            {
                Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * baseDirection;
                float clearance = 3.8f;
                if (Physics.SphereCast(origin, 0.34f, candidate, out RaycastHit hit, clearance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    clearance = hit.distance;

                float score = clearance + (Vector3.Dot(candidate, baseDirection) * 0.6f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = candidate;
                }
            }

            bestDirection.y = 0f;
            return bestDirection.sqrMagnitude > 0.001f ? bestDirection.normalized : -moveDirection;
        }

        private Vector3 GetObstacleAvoidance()
        {
            Vector3 origin = npc.Position + Vector3.up;
            Vector3 result = Vector3.zero;
            float[] angles = { -85f, -60f, -35f, 0f, 35f, 60f, 85f };

            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * moveDirection;
                if (!Physics.SphereCast(origin, 0.28f, direction, out RaycastHit hit, 3.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 normal = hit.normal;
                normal.y = 0f;
                float strength = 1f - Mathf.Clamp01(hit.distance / 3.1f);
                result += normal.sqrMagnitude > 0.001f
                    ? normal.normalized * (1.5f + (strength * 2.5f))
                    : -direction * (angle == 0f ? 2f : 1f);
            }

            result.y = 0f;
            return result;
        }

        private Vector3 GetPlayerSeparation()
        {
            Vector3 result = Vector3.zero;
            foreach (Player player in Player.List.Where(p => p != null && p.IsConnected && p.IsAlive && p != npc))
            {
                Vector3 offset = npc.Position - player.Position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > 2.2f || distance < 0.01f)
                    continue;

                result += offset.normalized * (2.2f - distance);
            }

            return result;
        }

        private bool TryMove(Vector3 displacement)
        {
            if (displacement.sqrMagnitude < 0.0001f)
                return true;

            touchedWall = false;
            Vector3 origin = npc.Position + Vector3.up;
            float distance = displacement.magnitude;
            if (Physics.CapsuleCast(origin - (Vector3.up * 0.75f), origin + (Vector3.up * 0.75f), 0.3f,
                displacement.normalized, out RaycastHit hit, distance + 0.15f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                touchedWall = true;
                consecutiveWallTouches++;
                lastWallNormal = hit.normal;
                lastWallNormal.y = 0f;
                Vector3 slide = Vector3.ProjectOnPlane(displacement, hit.normal);
                if (slide.sqrMagnitude < 0.0001f)
                    return false;

                npc.Position += slide;
                return consecutiveWallTouches < 2;
            }

            consecutiveWallTouches = 0;
            lastWallNormal = Vector3.zero;
            npc.Position += displacement;
            return true;
        }
    }
}
