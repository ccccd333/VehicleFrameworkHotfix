using System;
using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace VehicleFrameworkHotfix
{
    /// <summary>
    /// VRF（Vehicle Raid Framework）によって呼び出された敵・NPC大型車両がマップ端から撤退する際、
    /// 車体の先端や角が端に触れただけで即座にデスポーンしてしまう早期消失バグを防止するパッチ。
    /// プレイヤー車両や他MODの通常車両には干渉せず、VRFの脱出処理時のみ安全に適用されます。
    /// </summary>
    [HarmonyPatch(typeof(PathingHelper), nameof(PathingHelper.ExitMapForVehicle))]
    public static class Patch_PrematureExitPrevention
    {
        [HarmonyPrefix]
        public static bool Prefix(VehiclePawn vehicle, Job job)
        {
            if (vehicle == null || !vehicle.Spawned || vehicle.Map == null)
            {
                return true;
            }

            // プレイヤー所有の車両ならVF標準の挙動を維持（一切干渉しない）
            if (vehicle.Faction == Faction.OfPlayer)
            {
                return true;
            }

            // VRFの脱出ジョブまたはDutyを実行している車両にのみ限定
            bool isVRFExit = (job?.def?.defName == "VRF_VehicleExitMap") ||
                             (vehicle.mindState?.duty?.def?.defName == "VRF_VehicleExitMap");

            if (!isVRFExit)
            {
                return true;
            }

            // 小型車両（1x1等）はVF標準の判定で問題なし
            IntVec2 size = vehicle.def.Size;
            if (size.x <= 1 && size.z <= 1)
            {
                return true;
            }

            Map map = vehicle.Map;
            IntVec3 pos = vehicle.Position;

            // 車体中心がすでにマップ端または脱出グリッドに到達していれば脱出を許可
            if (pos.OnEdge(map) || map.exitMapGrid.IsExitCell(pos))
            {
                return true;
            }

            // 中心からマップ四辺への最短距離
            int distToEdge = Math.Min(
                Math.Min(pos.x, map.Size.x - 1 - pos.x),
                Math.Min(pos.z, map.Size.z - 1 - pos.z)
            );

            // 車両がまだ移動中かつ中心が端から離れている場合は、早期デスポーンを防止して移動を継続
            bool isMoving = vehicle.pather != null && vehicle.pather.Moving;
            if (isMoving && distToEdge > 2)
            {
                return false;
            }

            // 移動停止している（スタック・目的地到達）か端まで近接していれば脱出完了させる
            return true;
        }
    }
}
