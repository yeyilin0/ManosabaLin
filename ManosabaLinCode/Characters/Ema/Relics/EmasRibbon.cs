using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Emalin.Relics;

[RegisterRelic(typeof(EmalinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Emalin))]
public sealed class EmasRibbon : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
}
