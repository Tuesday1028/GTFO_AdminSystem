#if false
using Gear;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Weapon;

public class BurstLikeGluegun : Feature
{
    public override string Name => "胶枪式连发武器蓄力开火";

    public override FeatureGroup Group => EntryPoint.Groups.Weapon;

    [ArchivePatch(typeof(BWA_Burst), nameof(BWA_Burst.Update))]
    private class BWA_Burst__Update__Patch
    {
        private static bool Prefix(BWA_Burst __instance)
        {
            Update(__instance);
            return false;
        }
    }

    public static void Update(BWA_Burst __instance)
    {
        if (__instance.m_owner == null)
        {
            return;
        }
        bool flag = !__instance.m_owner.Locomotion.IsRunning && !__instance.m_owner.Locomotion.IsInAir;
        __instance.m_fireHeld = flag && (__instance.m_weapon.FireButton || (InputMapper.HasGamepad && InputMapper.GetAxisKeyMouseGamepad(InputAction.GamepadFireTrigger, __instance.m_owner.InputFilter) > 0f));
        if (flag && __instance.m_weapon.FireButtonPressed)
        {
            __instance.m_firePressed = true;
        }
        else if (__instance.m_fireHeld && __instance.m_firePressed)
        {
            __instance.m_firePressed = false;
        }
        if (__instance.m_weapon.IsEnabled || (__instance.m_owner.FPItemHolder.ItemDownTrigger && __instance.m_archetypeData.FireMode == eWeaponFireMode.Burst && !__instance.BurstIsDone()))
        {
            __instance.m_clip = __instance.m_weapon.GetCurrentClip();
            bool flag2 = ((__instance.HasChargeup && __instance.m_inChargeup) || !__instance.m_triggerNeedsPress) ? __instance.m_fireHeld : __instance.m_firePressed;
            if (!__instance.m_inChargeup && !__instance.m_firing && flag2 && Clock.Time > __instance.m_nextBurstTimer)
            {
                if (__instance.m_clip > 0f)
                {
                    if (__instance.HasChargeup)
                    {
                        __instance.m_chargeupTimer = Clock.Time + __instance.ChargeupDelay();
                        __instance.m_inChargeup = true;
                        __instance.m_readyToFire = false;
                        __instance.m_weapon.TriggerAudioChargeup();
                        __instance.m_weapon.FPItemHolder.DontRelax();
                        GuiManager.CrosshairLayer.SetChargeUpVisibleAndProgress(true, 0f);
                    }
                    else
                    {
                        __instance.m_chargeupTimer = 0f;
                        __instance.m_inChargeup = false;
                        GuiManager.CrosshairLayer.SetChargeUpVisibleAndProgress(false, 0f);
                        __instance.m_readyToFire = true;
                    }
                }
                else if (__instance.m_firePressed)
                {
                    if (!__instance.m_clickTriggered || !CellSettingsManager.SettingsData.Gameplay.AutoReload.Value || !__instance.m_weapon.m_inventory.CanReloadCurrent())
                    {
                        __instance.m_weapon.TriggerAudio(__instance.m_weapon.AudioData.eventClick);
                        __instance.m_nextShotTimer = Clock.Time + __instance.ShotDelay();
                        __instance.m_clickTriggered = true;
                    }
                    else if (__instance.m_clickTriggered && CellSettingsManager.SettingsData.Gameplay.AutoReload.Value && __instance.m_weapon.m_inventory.CanReloadCurrent())
                    {
                        __instance.m_weapon.m_inventory.TriggerReload();
                        __instance.m_clickTriggered = false;
                    }
                    if (__instance.m_clip <= 0f && !__instance.m_weapon.m_inventory.CanReloadCurrent())
                    {
                        __instance.m_weapon.m_wasOutOfAmmo = true;
                    }
                }
                if (__instance.m_firePressed)
                {
                    __instance.m_firePressed = false;
                }
            }
            if (__instance.m_inChargeup)
            {
                if (!__instance.m_fireHeld)
                {
                    float percent = Mathf.Clamp01(1f - (__instance.m_chargeupTimer - Clock.Time) / __instance.ChargeupDelay());
                    int count = Mathf.FloorToInt(percent * __instance.m_burstMax);
                    if (count == 0 || count == __instance.m_burstMax)
                    {
                        __instance.OnStopChargeup();
                        __instance.m_nextShotTimer = Clock.Time + __instance.ShotDelay();
                        return;
                    }
                    else
                    {
                        __instance.m_inChargeup = false;
                        __instance.m_readyToFire = true;
                        GuiManager.CrosshairLayer.SetChargeUpVisibleAndProgress(false, 0f);
                        OnStartFiring(__instance, count);
                        goto IL_1;
                    }
                }
                float num = 1f - (__instance.m_chargeupTimer - Clock.Time) / __instance.ChargeupDelay();
                GuiManager.CrosshairLayer.SetChargeUpVisibleAndProgress(true, num);
                if (Clock.Time >= __instance.m_chargeupTimer)
                {
                    __instance.m_inChargeup = false;
                    __instance.m_readyToFire = true;
                    GuiManager.CrosshairLayer.SetChargeUpVisibleAndProgress(false, 0f);
                }
            }
            if (__instance.m_readyToFire && !__instance.m_firing)
            {
                __instance.OnStartFiring();
            }
        IL_1:
            if (__instance.PreFireCheck())
            {
                if (__instance.m_readyToFire && __instance.m_firing && Clock.Time > __instance.m_nextShotTimer)
                {
                    if (__instance.m_clip > 0f)
                    {
                        __instance.OnFireShot();
                        __instance.m_nextShotTimer = Clock.Time + __instance.ShotDelay();
                    }
                    else
                    {
                        __instance.OnStopFiring();
                        __instance.OnFireShotEmptyClip();
                        __instance.m_nextShotTimer = Clock.Time + __instance.ShotDelay();
                    }
                    __instance.PostFireCheck();
                    return;
                }
            }
            else if (__instance.m_firing)
            {
                __instance.OnStopFiring();
                return;
            }
        }
        else
        {
            if (__instance.m_inChargeup)
            {
                __instance.OnStopChargeup();
            }
            if (__instance.m_firing)
            {
                __instance.OnStopFiring();
            }
        }
    }

    public static void OnStartFiring(BWA_Burst __instance, int count)
    {
        __instance.OnStartFiring();
        int currentClip = __instance.m_weapon.GetCurrentClip();
        __instance.m_burstCurrentCount = Mathf.Min(__instance.m_burstMax, Mathf.Min(count, currentClip));
        if (currentClip >= __instance.m_burstMax && !__instance.m_weapon.AudioData.TriggerBurstAudioForEachShot)
        {
            __instance.m_weapon.TriggerBurstFireAudio();
            __instance.m_weapon.FPItemHolder.DontRelax();
        }
    }
}
#endif