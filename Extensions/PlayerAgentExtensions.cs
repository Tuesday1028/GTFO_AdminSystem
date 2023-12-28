using Enemies;
using Player;
using UnityEngine;

namespace Hikaria.AdminSystem.Extensions
{
    public static class PlayerAgentExtensions
    {
        public static bool CanSeePosition(this PlayerAgent player, Vector3 targetPos, int layerMask, out RaycastHit hitInfo)
        {
            Ray ray = default;
            ray.origin = player.FPSCamera.Position;
            Ray ray2 = ray;
            Vector3 vector = targetPos - player.FPSCamera.Position;
            float magnitude = vector.magnitude;
            vector /= magnitude;
            ray2.direction = vector;
            return !Physics.Raycast(ray2, out hitInfo, magnitude, layerMask);
        }

        public static bool CanSeeObject(this PlayerAgent player, GameObject targetObj)
        {
            Vector3 position = targetObj.transform.position;
            return player.CanSeePosition(position, LayerManager.MASK_WORLD, out RaycastHit raycastHit) || !(raycastHit.transform.gameObject != targetObj) || raycastHit.transform.IsChildOf(targetObj.transform);
        }

        public static bool CanSeeEnemyNormal(this PlayerAgent player, EnemyAgent enemy)
        {
            Vector3 position = enemy.AimTarget.position;
            return player.CanSeePosition(position, LayerManager.MASK_WORLD, out RaycastHit raycastHit) || !(raycastHit.transform.gameObject != enemy.gameObject) || raycastHit.transform.IsChildOf(enemy.gameObject.transform);
        }

        public static bool CanSeeEnemyPlus(this PlayerAgent player, EnemyAgent enemy)
        {
            foreach (var limb in enemy.Damage.DamageLimbs)
            {
                if (player.CanSeePosition(limb.DamageTargetPos, LayerManager.MASK_WORLD, out RaycastHit raycastHit) || !(raycastHit.transform.gameObject != limb.gameObject) || raycastHit.transform.IsChildOf(limb.gameObject.transform))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CanFireHitObject(this PlayerAgent player, GameObject targetObj)
        {
            return !player.CanSeePosition(targetObj.transform.position, LayerManager.MASK_BULLETWEAPON_RAY, out RaycastHit raycastHit) && (raycastHit.transform.gameObject == targetObj || raycastHit.transform.IsChildOf(targetObj.transform));
        }
    }
}
