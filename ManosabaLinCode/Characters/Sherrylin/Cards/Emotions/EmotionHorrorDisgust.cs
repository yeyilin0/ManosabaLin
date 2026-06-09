using ManosabaLin.Characters.Sherrylin.Orbs;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmotionHorrorDisgust() : CaseFileCard<EmotionHorrorDisgustOrb, EmotionHorrorDisgustPower>(0, CardRarity.Ancient, TargetType.Self);
