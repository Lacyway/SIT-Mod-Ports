﻿using BepInEx.Logging;
using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotGrenadeClass : SAINBase, ISAINClass
    {
        public SAINBotGrenadeClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            GrenadeDangerPoint = null;
            for (int i = ActiveGrenades.Count - 1; i >= 0; i--)
            {
                var tracker = ActiveGrenades[i];
                if (tracker == null || tracker.Grenade == null)
                {
                    ActiveGrenades.RemoveAt(i);
                }
            }
            for (int i = 0; i < ActiveGrenades.Count; i++)
            {
                GrenadeTracker tracker = ActiveGrenades[i];
                if (tracker != null && tracker.Grenade != null && tracker.GrenadeSpotted)
                {
                    GrenadeDangerPoint = tracker.Grenade.transform.position;
                    break;
                }
            }
        }

        public void Dispose()
        {
        }

        public Vector3? GrenadeDangerPoint { get; private set; }


        public GrenadeThrowType GetThrowType(out GrenadeThrowDirection direction, out Vector3 ThrowAtPoint)
        {
            ThrowAtPoint = default;

            if (AllowCheck())
            {
                if (CanThrowOverObstacle(out ThrowAtPoint))
                {
                    direction = GrenadeThrowDirection.Over;
                }
                else if (CanThrowAroundObstacle(out ThrowAtPoint))
                {
                    direction = GrenadeThrowDirection.Around;
                }
                else
                {
                    direction = GrenadeThrowDirection.None;
                    return GrenadeThrowType.None;
                }

                float distance = (BotOwner.Position - SAIN.Enemy.EnemyPosition).magnitude;

                if (distance <= 10f)
                {
                    return GrenadeThrowType.Close;
                }
                if (distance <= 30f)
                {
                    return GrenadeThrowType.Mid;
                }
                else
                {
                    return GrenadeThrowType.Far;
                }
            }
            else
            {
                direction= GrenadeThrowDirection.None;
                return GrenadeThrowType.None;
            }
        }

        private bool CanThrowAroundObstacle(out Vector3 ThrowAtPoint)
        {
            ThrowAtPoint = Vector3.zero;

            if (!AllowCheck())
            {
                return false;
            }

            Vector3 headPos = SAIN.Transform.Head;
            Vector3 direction = SAIN.Enemy.EnemyHeadPosition - headPos;

            float distance = direction.magnitude;

            if (distance > 50f)
            {
                return false;
            }

            if (SAIN.Enemy.LastSeenPosition == null)
            {
                return false;
            }
            Vector3 lastKnownPos = SAIN.Enemy.LastSeenPosition.Value;
            lastKnownPos.y += 1.45f;

            Vector3 lastKnownDirection = lastKnownPos - headPos;

            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            if (Physics.Raycast(headPos, lastKnownDirection, out var hit, lastKnownDirection.magnitude + 5f, mask) && (hit.point - headPos).magnitude > lastKnownDirection.magnitude)
            {
                ThrowAtPoint = hit.point;
                return true;
            }

            return false;
        }

        private bool CanThrowOverObstacle(out Vector3 ThrowAtPoint)
        {
            ThrowAtPoint = Vector3.zero;

            if (!AllowCheck())
            {
                return false;
            }

            var enemyHead = SAIN.Enemy.EnemyHeadPosition;
            var botHead = SAIN.Transform.Head;
            var direction = enemyHead - botHead;
            float distance = direction.magnitude;

            if (distance > 50f)
            {
                return false;
            }

            var mask = LayerMaskClass.HighPolyWithTerrainMask;

            if (Physics.Raycast(botHead, direction, out var hit, distance, mask))
            {
                if (Vector3.Distance(hit.point, botHead) < 0.33f)
                {
                    return false;
                }

                float height = hit.collider.bounds.size.y;
                var objectPos = hit.collider.transform.position;
                objectPos.y += height + 0.5f;

                var directionToHeight = objectPos - botHead;

                if (directionToHeight.magnitude > 30f)
                {
                    return false;
                }

                if (!Physics.Raycast(botHead, directionToHeight, directionToHeight.magnitude, mask))
                {
                    ThrowAtPoint = objectPos;
                    return true;
                }
            }

            return false;
        }

        private bool AllowCheck()
        {
            if (!BotOwner.WeaponManager.Grenades.HaveGrenade)
            {
                return false;
            }

            var enemy = SAIN.Enemy;

            if (enemy == null)
            {
                return false;
            }
            if (enemy.IsVisible && enemy.CanShoot)
            {
                return false;
            }

            return true;
        }

        public void EnemyGrenadeThrown(Grenade grenade, Vector3 dangerPoint)
        {
            if (SAIN.BotActive && !SAIN.GameIsEnding)
            {
                float reactionTime = GetReactionTime(SAIN.Info.Profile.DifficultyModifier);
                var tracker = BotOwner.gameObject.AddComponent<GrenadeTracker>();
                tracker.Initialize(grenade, dangerPoint, reactionTime);
                ActiveGrenades.Add(tracker);
            }
        }

        public List<GrenadeTracker> ActiveGrenades { get; private set; } = new List<GrenadeTracker>();

        private int GrenadePositionComparerer(GrenadeTracker A, GrenadeTracker B)
        {
            if (A == null && B != null)
            {
                return 1;
            }
            else if (A != null && B == null)
            {
                return -1;
            }
            else if (A == null && B == null)
            {
                return 0;
            }
            else
            {
                float AMag = (BotOwner.Position - A.DangerPoint).sqrMagnitude;
                float BMag = (BotOwner.Position - B.DangerPoint).sqrMagnitude;
                return AMag.CompareTo(BMag);
            }
        }

        private static bool EnemyGrenadeHeard(Vector3 grenadePosition, Vector3 playerPosition, float distance)
        {
            return (grenadePosition - playerPosition).magnitude < distance;
        }

        private static float GetReactionTime(float diffMod)
        {
            float reactionTime = 0.33f;
            reactionTime *= diffMod;
            reactionTime *= Random.Range(0.75f, 1.25f);

            float min = 0.15f;
            float max = 0.66f;

            return Mathf.Clamp(reactionTime, min, max);
        }
    }
}