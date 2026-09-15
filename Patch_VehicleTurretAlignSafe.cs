using System;
using HarmonyLib;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace VehicleFrameworkHotfix
{
    /// <summary>
    /// 戦闘中（ターゲット指定中・連射中）にセーブされたデータをロードした際の不具合を防止するパッチ。
    /// 1. 相手車両の DrawPos が初期化前で NullReferenceException になるのを防止。
    /// 2. 連射中にセーブ・ロードした際、queuedToFire が true のまま取り残されて
    ///    タレットが永久に射撃・リロード不能（手のマークが出てフリーズ）になるスタックを解消。
    /// </summary>
    public static class Patch_VehicleTurretLoadSafety
    {
        [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.AlignToTargetRestricted))]
        public static class Patch_AlignToTargetRestricted
        {
            [HarmonyPrefix]
            public static bool Prefix(VehicleTurret __instance)
            {
                try
                {
                    LocalTargetInfo target = __instance.targetInfo;
                    if (!target.IsValid)
                    {
                        return false;
                    }

                    Vector3 targetPoint = Vector3.zero;
                    bool hasValidPoint = false;

                    if (target.HasThing && target.Thing != null)
                    {
                        if (target.Thing.Spawned)
                        {
                            try
                            {
                                targetPoint = target.Thing.DrawPos;
                                hasValidPoint = true;
                            }
                            catch
                            {
                                // DrawPos 取得例外時はセル座標へフォールバック
                            }
                        }
                    }

                    if (!hasValidPoint)
                    {
                        IntVec3 cell = target.Cell;
                        if (cell.IsValid)
                        {
                            targetPoint = cell.ToVector3Shifted();
                            hasValidPoint = true;
                        }
                    }

                    if (hasValidPoint)
                    {
                        __instance.TurretRotationTargeted = __instance.TurretLocation.AngleToPoint(targetPoint);
                        if (__instance.attachedTo != null)
                        {
                            __instance.TurretRotationTargeted -= __instance.attachedTo.TurretRotation;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[VehicleFrameworkHotfix] Suppressed exception in AlignToTargetRestricted: {ex.Message}");
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.PostPostLoadInit))]
        public static class Patch_PostPostLoadInit
        {
            [HarmonyPostfix]
            public static void Postfix(VehicleTurret __instance)
            {
                try
                {
                    // 連射中セーブからのロード時、VFは turretQueue を復元しないため
                    // queuedToFire が true のまま放置されると射撃・索敵・リロードが永久停止する。
                    // そのためフラグを安全に解除して再開可能にする。
                    if (__instance.queuedToFire)
                    {
                        __instance.queuedToFire = false;
                    }

                    // ターゲットを保持している場合は、CompVehicleTurrets の Ticker を起動して
                    // ロード後すぐに索敵・旋回・射撃を再開させる
                    if (__instance.targetInfo.IsValid && __instance.vehicle != null && __instance.vehicle.Spawned)
                    {
                        var comp = __instance.vehicle.GetComp<CompVehicleTurrets>();
                        if (comp != null)
                        {
                            comp.QueueTicker(__instance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[VehicleFrameworkHotfix] Warning during PostPostLoadInit cleanup: {ex.Message}");
                }
            }
        }
    }
}
