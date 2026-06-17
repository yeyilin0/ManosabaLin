using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmotionCuriosity() : CaseFileCard<EmotionCuriosityOrb>(-1, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new UniqueComponent()];
}
