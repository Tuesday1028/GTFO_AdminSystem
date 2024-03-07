using Hikaria.AdminSystem.Interfaces;
using Hikaria.AdminSystem.Managers;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Misc;

[EnableFeatureByDefault]
[DisallowInGameToggle]
internal class PlayerScream : Feature, IOnPlayerEvent
{
    public override string Name => "鬼叫";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;

    [FeatureConfig]
    public static PlayerScreamSetting Settings { get; set; }

    public class PlayerScreamSetting
    {
        [FSDisplayName("启用")]
        public bool Enable { get; set; } = false;
        [FSDisplayName("EventID")]
        public uint eventID { get; set; } = 3184121378U;
    }

    public static void WantToSay(int playerID, uint eventID, uint inDialogID, uint startDialogID, uint subtitleId)
    {
        PlayerVoiceManager.WantToSayInternal(playerID, eventID, inDialogID, startDialogID, subtitleId);
    }

    public override void Init()
    {
        DevConsole.AddCommand(Command.Create<string>("WantToSay", "鬼叫", "鬼叫", Parameter.Create("Params", "参数"), (str) =>
        {
            var input = str.Split(',').ToList();
            if (input.Count <= 5)
            {
                for (int i = input.Count; i < 5; i++)
                {
                    input.Add("");
                }
            }
            if (!int.TryParse(input[0], out var playerID))
            {
                playerID = 0;
            }
            if (!uint.TryParse(input[1], out var eventID))
            {
                eventID = 0;
            }
            if (!uint.TryParse(input[2], out var inDialogID))
            {
                inDialogID = 0;
            }
            if (!uint.TryParse(input[3], out var startDialogID))
            {
                startDialogID = 0;
            }
            if (!uint.TryParse(input[4], out var subtitleId))
            {
                subtitleId = 0;
            }
            WantToSay(playerID, eventID, inDialogID, startDialogID, subtitleId);
        }));

        GameEventManager.RegisterSelfInGameEventManager(this);
    }


    public void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason)
    {
        if (!player.IsLocal || !player.HasPlayerAgent || playerEvent != SNet_PlayerEvent.PlayerAgentSpawned || !Settings.Enable)
        {
            return;
        }
        WantToSay(player.PlayerSlotIndex(), Settings.eventID, 0U, 0U, 0U);
    }
}
