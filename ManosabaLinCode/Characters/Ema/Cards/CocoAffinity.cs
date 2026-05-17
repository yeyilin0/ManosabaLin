using MinionLib.Component.Core;
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
public sealed class CocoAffinity : ManosabaEmalinCardTemplate
{
    public CocoAffinity() : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var target = cardPlay.Target!;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        if (target.Monster?.NextMove?.Intents == null) return;

        var multiplier = (bond != null && bond.Affinity > bond.Estrangement) ? 2 : 1;

        foreach (var intent in target.Monster.NextMove.Intents)
        {
            switch (intent.IntentType)
            {
                case IntentType.Attack:
                case IntentType.DeathBlow:
                    foreach (var ally in CombatState.Allies.Where(a => a is { IsAlive: true }))
                        await PowerCmd.Apply<HardenedShellPower>(choiceContext, ally, 30 * multiplier, creature, this, false);
                    break;

                case IntentType.Defend:
                case IntentType.Buff:
                    await PowerCmd.Apply<RitualPower>(choiceContext, creature, 9 * multiplier, creature, this, false);
                    break;

                case IntentType.Debuff:
                case IntentType.DebuffStrong:
                    foreach (var ally in CombatState.Allies.Where(a => a is { IsAlive: true }))
                        await PowerCmd.Apply<ArtifactPower>(choiceContext, ally, 5 * multiplier, creature, this, false);
                    break;
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}