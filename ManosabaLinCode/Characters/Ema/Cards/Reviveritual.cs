using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Reviveritual : ManosabaCardTemplate
{
    public Reviveritual() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly) { }

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
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        if (target == null) return;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var targetMaxHp = (int)target.MaxHp;

        // 击杀队友
        await CreatureCmd.Kill(target);

        // 复活队友1点生命
        await CreatureCmd.SetCurrentHp(target, 1);

        // 给予 Reviveritualpower 和 999 BufferPower
        await PowerCmd.Apply<Reviveritualpower>(
            choiceContext, target, 1, source.Owner.Creature, this, false);

        await PowerCmd.Apply<BufferPower>(
            choiceContext, target, 999, source.Owner.Creature, this, false);

        // 收集所有活着的友方玩家
        var aliveAllies = source.CombatState.Allies
            .Where(a => a.IsAlive && a.IsPlayer)
            .ToList();

        if (aliveAllies.Count == 0) return;

        var combatState = source.CombatState;
        var rng = source.Owner.RunState.Rng.CombatCardSelection;
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", new Type[] { typeof(Player) });
        var genericMethod = createCardMethod.MakeGenericMethod(typeof(Revivefragment));

        for (int i = 0; i < targetMaxHp; i++)
        {
            // 随机选一个活着的队友
            var ally = rng.NextItem(aliveAllies);
            var allyPlayer = ally.Player;
            var fragment = (CardModel)genericMethod.Invoke(combatState, new object[] { allyPlayer });
            var pileType = rng.NextDouble() < 0.5 ? PileType.Draw : PileType.Discard;
            await CardPileCmd.AddGeneratedCardToCombat(fragment, pileType, allyPlayer);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}