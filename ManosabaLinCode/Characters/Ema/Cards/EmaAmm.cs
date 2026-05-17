using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Emalin;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class EmaAmm : ManosabaEmalinCardTemplate
{
    public EmaAmm() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmaWitchFactorPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var enemies = source.CombatState.Enemies
            .Where(e => e is { IsAlive: true })
            .ToList();

        foreach (var enemy in enemies)
        {
            var stacks = (int)(enemy.MaxHp / 5m);
            if (stacks > 0)
                await PowerCmd.Apply<EmaWitchFactorPower>(
                    choiceContext, enemy, stacks,
                    source.Owner.Creature, source, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}