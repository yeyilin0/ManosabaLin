using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmotionDesolate() : CaseFileCard<EmotionDesolateOrb>(-1, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
}