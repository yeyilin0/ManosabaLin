using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class CocoEstrangement : ManosabaEmalinCardTemplate
{
    public CocoEstrangement() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var target = cardPlay.Target!;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        var affectAll = bond != null && bond.Estrangement > bond.Affinity;

        if (target.Monster?.NextMove?.Intents == null) return;

        foreach (var intent in target.Monster.NextMove.Intents)
        {
            if (affectAll)
            {
                var enemies = CombatState.Enemies.Where(e => e is { IsAlive: true }).ToList();
                foreach (var enemy in enemies)
                    await ApplyEffect(choiceContext, creature, enemy, intent.IntentType);
            }
            else
            {
                await ApplyEffect(choiceContext, creature, target, intent.IntentType);
            }
        }
    }

    private async Task ApplyEffect(
        PlayerChoiceContext ctx, Creature creature, Creature enemy, IntentType intentType)
    {
        switch (intentType)
        {
            case IntentType.Attack:
            case IntentType.DeathBlow:
                await PowerCmd.Apply<ShrinkPower>(ctx, enemy, 1, creature, this, false);
                break;
            case IntentType.Defend:
            case IntentType.Buff:
                await PowerCmd.Apply<WeakPower>(ctx, enemy, 1, creature, this, false);
                break;
            case IntentType.Debuff:
            case IntentType.DebuffStrong:
                await PowerCmd.Apply<WeakPower>(ctx, enemy, 1, creature, this, false);
                await PowerCmd.Apply<VulnerablePower>(ctx, enemy, 1, creature, this, false);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}