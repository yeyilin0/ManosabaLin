using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 狂想漫延：X卡，从抽牌堆随机打出X张卡，升级X+2
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class MadnessSpread() : ManosabaCardTemplate(-1, CardType.Attack, CardRarity.Common, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var count = source.ResolveEnergyXValue();
        if (IsUpgraded)
            count += 2;

        await CardPileCmd.AutoPlayFromDrawPile(choiceContext, source.Owner, count, CardPilePosition.Top, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级：X+2（在 OnPlay 中通过 IsUpgraded 判断）
    }
}
