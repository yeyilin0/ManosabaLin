using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class FinalFarewell() : ManosabaCardTemplate(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<FinalFarewellPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 清除魔女化
        var withPower = Owner.Creature.GetPower<WithPower>();
        if (withPower != null)
            await PowerCmd.Remove(withPower);

        await PowerCmd.Apply<FinalFarewellPower>(choiceContext, Owner.Creature, 1, Owner.Creature, source);

        if (IsUpgraded)
            Owner.Creature.GetPower<FinalFarewellPower>()?.RecordStorm(80m);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 用户 2026-10-15 指令：升级后必须真的获得固有（原 CanonicalKeywords + IsUpgraded 的写法不生效）。
        // 必须走 AddKeyword 的实时写入 —— _keywords 是首次读取时从 CanonicalKeywords 快照下来的
        // 实例状态（且随克隆传播），「先读过关键字、再升级」时会被快照吞掉。
        // 规范先例：原版 Afterimage / 同池 SuperStrength / 本池 SherrylinMentalTrauma。
        AddKeyword(CardKeyword.Innate);
    }
}
