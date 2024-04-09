using Enemies;
using Player;
using UnityEngine;

namespace Hikaria.AdminSystem.Extensions
{
    public static class PlayerAgentExtensions
    {
        public static bool Raycast(this PlayerAgent player, Vector3 target, int layerMask, out RaycastHit hit)
        {
            Vector3 vector = target - player.FPSCamera.Position;
            float magnitude = vector.magnitude;
            vector /= magnitude;
            return Physics.Raycast(player.FPSCamera.Position, vector, out hit, magnitude, layerMask);
        }

        public static bool CanSeeObject(this PlayerAgent player, GameObject targetObj, out RaycastHit hit)
        {
            return player.Raycast(targetObj.transform.position, LayerManager.MASK_WORLD, out hit) && hit.collider.transform.IsChildOf(targetObj.transform);
        }

        public static bool CanSeeEnemyNormal(this PlayerAgent player, EnemyAgent enemy)
        {
            return !player.Raycast(enemy.AimTarget.position, LayerManager.MASK_WORLD, out _);
        }

        public static bool CanSeeEnemyPlus(this PlayerAgent player, EnemyAgent enemy)
        {
            foreach (var limb in enemy.Damage.DamageLimbs)
            {
                if (limb == null) continue;
                if (!player.Raycast(limb.DamageTargetPos, LayerManager.MASK_WORLD, out _))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CanFireHitObject(this PlayerAgent player, GameObject targetObj)
        {
            return player.Raycast(targetObj.transform.position, LayerManager.MASK_BULLETWEAPON_RAY, out var hit) && hit.transform.IsChildOf(targetObj.gameObject.transform);
        }
    }
}
