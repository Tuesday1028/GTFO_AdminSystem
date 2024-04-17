using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Security;

[HideInModSettings]
public class CConsoleCompatible : Feature
{
    public override string Name => "CConsole 兼容";

    public override FeatureGroup Group => EntryPoint.Groups.Security;

    [ArchivePatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.SetGenerationChecksum))]
    private static class Inject_BlockJoiningNormalLobby
    {
        private static void Prefix(ref ulong checksum)
        {
            checksum = ~checksum;
        }
    }

    [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.SlaveSendSessionQuestion))]
    private static class Inject_BlockJoiningNormalLobby2
    {
        private static void Prefix()
        {
            if (SNet.GameRevision == CellBuildData.GetRevision())
            {
                SNet.GameRevision = ~SNet.GameRevision;
            }
        }

        private static void Postfix()
        {
            SNet.GameRevision = CellBuildData.GetRevision();
        }
    }

    [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.SlaveWantsToJoin))]
    private static class Inject_BlockJoiningNormalLobby3
    {
        private static void Prefix()
        {
            if (SNet.GameRevision == CellBuildData.GetRevision())
            {
                SNet.GameRevision = ~SNet.GameRevision;
            }
        }

        private static void Postfix()
        {
            SNet.GameRevision = CellBuildData.GetRevision();
        }
    }
}
