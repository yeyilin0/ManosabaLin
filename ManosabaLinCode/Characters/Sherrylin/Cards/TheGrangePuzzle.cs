using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheGrangePuzzle() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string BaseCountKey = "BaseCount";

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar(BaseCountKey, 1),
        new IntVar("Bonus", 0)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<XlmPower>();
        }
    }

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RemoveOnPlayComponent()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var xlmStacks = (int)Owner.Creature.GetPowerAmount<XlmPower>();
        var bonus = (int)DynamicVars["Bonus"].BaseValue;
        var upgradeCount = xlmStacks + bonus;
        var rng = Owner.RunState.Rng.CombatCardSelection;

        // 随机升级消耗堆中的卡
        if (upgradeCount > 0)
        {
            var exhaustCards = PileType.Exhaust.GetPile(Owner).Cards.ToList();
            if (exhaustCards.Count > 0)
            {
                var cardsToUpgrade = exhaustCards.OrderBy(_ => rng.NextFloat()).Take(upgradeCount).ToList();
                foreach (var card in cardsToUpgrade)
                {
                    CardCmd.Upgrade(card, CardPreviewStyle.None);
                }
            }
        }

        // 获得 XlmPower层数 / 2 的能量
        var energyGain = xlmStacks / 2;
        if (energyGain > 0)
        {
            await PlayerCmd.GainEnergy(energyGain, Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Bonus"].UpgradeValueBy(2m);
        this.RemoveComponent<RemoveOnPlayComponent>();
    }
}
