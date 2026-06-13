using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

/// <summary>
/// 魔女化（情绪卡）：获得魔女化充能球，本回合造成三倍伤害。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class WitchificationEmotion() : CaseFileCard<WitchificationEmotionOrb>(-1, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
}
