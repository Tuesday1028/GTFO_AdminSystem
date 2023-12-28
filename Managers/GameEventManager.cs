using Hikaria.AdminSystem.Interfaces;
using SNetwork;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Interfaces;
using static Hikaria.AdminSystem.Interfaces.IOnSessionMemberChanged;

namespace Hikaria.AdminSystem.Managers
{
    [HideInModSettings]
    [EnableFeatureByDefault]
    [DoNotSaveToConfig]
    [DisallowInGameToggle]
    public class GameEventManager : Feature
    {
        public override string Name => "游戏事件监听";

        public static new IArchiveLogger FeatureLogger { get; set; }

        private static void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason)
        {
            foreach (var instance in _instancesOnPlayerEvent)
            {
                try
                {
                    instance.OnPlayerEvent(player, playerEvent, reason);
                }
                catch (Exception ex)
                {
                    FeatureLogger.Error(ex.ToString());
                }
                try
                {
                    switch (playerEvent)
                    {
                        case SNet_PlayerEvent.PlayerLeftSessionHub:
                        case SNet_PlayerEvent.PlayerAgentDeSpawned:
                            OnSessionMemberChanged(player, SessionMemberEvent.LeftSessionHub);
                            break;
                        case SNet_PlayerEvent.PlayerAgentSpawned:
                            OnSessionMemberChanged(player, SessionMemberEvent.JoinSessionHub);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    FeatureLogger.Error(ex.ToString());
                }
            }
        }

        private static void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            FeatureLogger.Msg(ConsoleColor.White, $"{player.NickName} [{player.Lookup}] {playerEvent}");
            foreach (var instance in _instancesSessionMemberChanged)
            {
                try
                {
                    instance.OnSessionMemberChanged(player, playerEvent);
                }
                catch (Exception ex)
                {
                    FeatureLogger.Error(ex.ToString());
                }
            }
        }

        private static void OnAfterLevel()
        {
            foreach (var instance in _instancesOnAfterLevel)
            {
                try
                {
                    instance.OnAfterLevel();
                }
                catch (Exception ex)
                {
                    FeatureLogger.Error(ex.ToString());
                }
            }
        }

        public override void OnGameStateChanged(int state)
        {
            if (state == (int)eGameStateName.AfterLevel)
            {
                OnAfterLevel();
            }
        }

        [ArchivePatch(typeof(SNet_GlobalManager), nameof(SNet_GlobalManager.Setup))]
        private class SNet_GlobalManager__Setup__Patch
        {
            private static void Postfix()
            {
                SNet_Events.OnPlayerEvent += new Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason>(OnPlayerEvent);
            }
        }

        public static void RegisterSelfInGameEventManager<T>(T instance)
        {
            Type type = typeof(T);
            if (typeof(IOnPlayerEvent).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                _instancesOnPlayerEvent.Add((IOnPlayerEvent)instance);
            if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                _instancesSessionMemberChanged.Add((IOnSessionMemberChanged)instance);
            if (typeof(IOnAfterLevel).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                _instancesOnAfterLevel.Add((IOnAfterLevel)instance);
        }

        private static readonly HashSet<IOnPlayerEvent> _instancesOnPlayerEvent = new();

        private static readonly HashSet<IOnSessionMemberChanged> _instancesSessionMemberChanged = new();

        private static readonly HashSet<IOnAfterLevel> _instancesOnAfterLevel = new();
    }
}
