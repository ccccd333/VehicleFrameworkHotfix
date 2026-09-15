using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace VehicleFrameworkHotfix
{
    [HarmonyPatch]
    public static class Patch_VRF_DynamicAssault_EnemySafety
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("VehicleRaidFramework.VRF_JobGiver_DynamicAssault");
            if (type == null)
            {
                Log.Message("[VehicleFrameworkHotfix] VRF_JobGiver_DynamicAssault not found, skipping enemy safety patch.");
                return null;
            }
            return AccessTools.Method(type, "FindNearestEnemy", new Type[] { typeof(VehiclePawn) });
        }

        [HarmonyPostfix]
        public static void Postfix(VehiclePawn vehicle, ref Thing __result)
        {
            if (vehicle == null || vehicle.Map == null) return;

            // If VRF returned a target, verify it with strict vanilla HostileTo
            if (__result != null)
            {
                if (IsInvalidEnemy(vehicle, __result))
                {
                    // Target is friendly/player! Sanitize it.
                    __result = null;
                }
            }

            // If no valid target found yet, scan attackTargetsCache with vanilla HostileTo double-check
            if (__result == null)
            {
                __result = FindSafeNearestEnemy(vehicle);
            }
        }

        public static bool IsInvalidEnemy(VehiclePawn vehicle, Thing target)
        {
            if (target == null || target.Destroyed || !target.Spawned || target.Map != vehicle.Map)
            {
                return true;
            }
            if (target == vehicle)
            {
                return true;
            }
            if (target.Map.fogGrid != null && target.Map.fogGrid.IsFogged(target.Position))
            {
                return true;
            }
            if (target is Pawn p && (p.Dead || p.Downed))
            {
                return true;
            }

            // 1. Double check using vanilla GenHostility: must be truly hostile to this vehicle!
            if (!vehicle.HostileTo(target))
            {
                return true;
            }

            // 2. Double check faction level
            if (target.Faction != null)
            {
                // Never target own faction
                if (target.Faction == vehicle.Faction)
                {
                    return true;
                }

                // If this vehicle is friendly to the player (ally/neutral reinforcement), NEVER target player or player's allies!
                if (vehicle.Faction != null && !vehicle.Faction.HostileTo(Faction.OfPlayer))
                {
                    if (target.Faction == Faction.OfPlayer)
                    {
                        return true;
                    }
                    if (!target.Faction.HostileTo(Faction.OfPlayer))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Thing FindSafeNearestEnemy(VehiclePawn vehicle)
        {
            var targets = vehicle.Map?.attackTargetsCache?.TargetsHostileToFaction(vehicle.Faction);
            if (targets == null || targets.Count == 0) return null;

            Thing bestThing = null;
            float bestDistSq = float.MaxValue;

            foreach (var attackTarget in targets)
            {
                Thing t = attackTarget.Thing;
                if (IsInvalidEnemy(vehicle, t)) continue;

                float distSq = (t.Position - vehicle.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestThing = t;
                }
            }

            return bestThing;
        }
    }
}
