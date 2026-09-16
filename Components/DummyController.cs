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
                if (Time.time >= nextDecisionAt)
                {
                    UpdateFollowState();
                    ChooseLookAndMove();
                    UpdatePauseState();
                    TryJump();
                    nextDecisionAt = Time.time + 1.1f + ((float)RandomSource.NextDouble() * 2.2f);
                }

                Vector3 steering = GetSteeringDirection();
                moveDirection = Vector3.RotateTowards(
                    moveDirection,
                    steering,
                    Mathf.Deg2Rad * gameHandler.Config.NpcTurnSpeed * Time.deltaTime,
                    0f).normalized;
                if (moveDirection == Vector3.zero)
                    moveDirection = Vector3.forward;

                RotateTowards(moveDirection);

                bool isPaused = followTarget == null && Time.time < pauseUntil;
                float targetSpeed = isPaused ? 0f : (followTarget == null ? gameHandler.Config.NpcWalkSpeed : gameHandler.Config.NpcFollowSpeed);
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 2.2f * Time.deltaTime);
                Vector3 displacement = moveDirection * currentSpeed * Time.deltaTime;

                if (!TryMove(displacement))
                    desiredDirection = Quaternion.Euler(0f, RandomSource.Next(-135, 136), 0f) * moveDirection;

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

        private void RotateTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            npc.Rotation = Quaternion.RotateTowards(
                npc.Rotation,
                targetRotation,
                gameHandler.Config.NpcTurnSpeed * Time.deltaTime);
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

        private Vector3 GetObstacleAvoidance()
        {
            Vector3 origin = npc.Position + Vector3.up;
            Vector3 result = Vector3.zero;
            float[] angles = { -55f, -28f, 0f, 28f, 55f };

            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * moveDirection;
                if (!Physics.Raycast(origin, direction, 2.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;

                result -= direction * (angle == 0f ? 2f : 1f);
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

            Vector3 origin = npc.Position + Vector3.up;
            float distance = displacement.magnitude;
            if (Physics.CapsuleCast(origin - (Vector3.up * 0.75f), origin + (Vector3.up * 0.75f), 0.3f,
                displacement.normalized, out RaycastHit hit, distance + 0.15f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 slide = Vector3.ProjectOnPlane(displacement, hit.normal);
                if (slide.sqrMagnitude < 0.0001f)
                    return false;

                npc.Position += slide;
                return true;
            }

            npc.Position += displacement;
            return true;
        }
    }
}
