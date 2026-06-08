using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin;

/// <summary>
/// 愤怒：0费能力牌，从案卷牌堆打出。
/// 挂载到充能球槽位，生效一回合，期间攻击+2伤害。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class Anger() : CaseFileCard(0, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromOrb<AngerOrb>(); }
    }

    protected override AngerOrb CreateOrb() => AngerOrb.Create(this);
}
