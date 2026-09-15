using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vehicles;
using Verse;

namespace VehicleFrameworkHotfix
{
    /// <summary>
    /// CompVehicleTurrets のセーブ・ロード時におけるデータ破損および例外を防止するパッチ。
    /// セーブ時に turretQuotas 等の不正キーを自動クリーニングし、
    /// ロード時にも例外ハンドリングにより車両ノードや搭乗員ポーンの消失を防ぎます。
    /// </summary>
    [HarmonyPatch(typeof(CompVehicleTurrets), nameof(CompVehicleTurrets.PostExposeData))]
    public static class Patch_CompVehicleTurretsExposeSafe
    {
        [HarmonyPrefix]
        public static void Prefix(CompVehicleTurrets __instance)
        {
            if (Scribe.mode == LoadSaveMode.Saving && __instance != null)
            {
                try
                {
                    Traverse traverse = Traverse.Create(__instance);

                    // turretQuotas 辞書内の null キーや無効タレット参照のクリーンアップ
                    var quotas = traverse.Field("turretQuotas").GetValue<Dictionary<VehicleTurret, int>>();
                    if (quotas != null && quotas.Count > 0)
                    {
                        var turrets = __instance.Turrets;
                        List<VehicleTurret> invalidKeys = null;

                        foreach (var key in quotas.Keys)
                        {
                            if (key == null || (turrets != null && !turrets.Contains(key)))
                            {
                                if (invalidKeys == null) invalidKeys = new List<VehicleTurret>();
                                invalidKeys.Add(key);
                            }
                        }

                        if (invalidKeys != null)
                        {
                            foreach (var key in invalidKeys)
                            {
                                quotas.Remove(key);
                            }
                        }
                    }

                    // turretQueue 内の null 参照のクリーンアップ
                    var queue = traverse.Field("turretQueue").GetValue<List<CompVehicleTurrets.TurretData>>();
                    if (queue != null && queue.Count > 0)
                    {
                        queue.RemoveAll(item => item == null || item.turret == null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[VehicleFrameworkHotfix] Warning during turret data cleanup before save: {ex.Message}");
                }
            }
        }

        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception, CompVehicleTurrets __instance)
        {
            if (__exception != null)
            {
                string vehicleLabel = __instance?.Vehicle?.LabelShortCap ?? "Unknown Vehicle";
                Log.Warning($"[VehicleFrameworkHotfix] Suppressed exception during CompVehicleTurrets.PostExposeData for {vehicleLabel} (Mode={Scribe.mode}): {__exception}");

                // 例外発生時でも車両が完全消滅しないよう最低限の初期化を保証
                if (__instance != null)
                {
                    Traverse traverse = Traverse.Create(__instance);
                    var turrets = traverse.Field("turrets").GetValue<List<VehicleTurret>>();
                    if (turrets == null)
                    {
                        traverse.Field("turrets").SetValue(new List<VehicleTurret>());
                    }
                }

                // 例外を吸収して SaveableFromNode の連鎖クラッシュ・ポーン消失を防止
                return null;
            }

            return null;
        }
    }
}
