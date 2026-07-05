using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Reviveritual : ManosabaCardTemplate
{
    public Reviveritual() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<Reviveritualpower>();
            yield return HoverTipFactory.FromPower<BufferPower>();
            yield return HoverTipFactory.FromCard<Revivefragment>();
            yield return HoverTipFactory.FromPower<HiroMagicRevivePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;

        // 有目标选目标，没有默认选自己
        if (target == null)
        {
            target = source.Owner.Creature;
        }

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var targetMaxHp = (int)target.MaxHp;

        // 给予 HiroMagicRevivePower
        await PowerCmd.Apply<HiroMagicRevivePower>(
            choiceContext, target, 1, source.Owner.Creature, this, false);

        // 999伤害
        await CreatureCmd.Damage(choiceContext, target, 999m, ValueProp.Unblockable | ValueProp.Unpowered, this, cardPlay);

        // 失去39血
        await CreatureCmd.Damage(choiceContext, target, 39m, ValueProp.Unblockable | ValueProp.Unpowered, this, cardPlay);

        // 给予 Reviveritualpower 和 999 BufferPower
        await PowerCmd.Apply<Reviveritualpower>(
            choiceContext, target, 3, source.Owner.Creature, this, false);

        await PowerCmd.Apply<BufferPower>(
            choiceContext, target, 999, source.Owner.Creature, this, false);

        // 塞碎片
        var aliveAllies = source.CombatState.Allies
            .Where(a => a.IsAlive && a.IsPlayer)
            .ToList();

        if (aliveAllies.Count > 0)
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;

            for (int i = 0; i < targetMaxHp; i++)
            {
                var ally = rng.NextItem(aliveAllies);
                var allyPlayer = ally.Player;
                var fragment = source.CombatState.CreateCard<Revivefragment>(allyPlayer);
                var pileType = rng.NextDouble() < 0.5 ? PileType.Draw : PileType.Discard;
                await CardPileCmd.AddGeneratedCardToCombat(fragment, pileType, allyPlayer);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
