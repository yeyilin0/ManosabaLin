using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmotionSurprise() : CaseFileCard(0, CardRarity.Ancient, TargetType.Self)
{
    protected override OrbModel CreateEmotionOrb() => new EmotionSurpriseOrb();
}
