using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Attackfourfour() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PerjuryPower>(); }
    }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { TransmigrationRules.TransmigrationKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<PerjuryPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);

        // 只随机打出攻击牌
        var drawPile = PileType.Draw.GetPile(source.Owner);
        var card = drawPile.Cards
            .Where(c => c.Type == CardType.Attack && !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList()
            .StableShuffle(source.Owner.RunState.Rng.Shuffle)
            .FirstOrDefault();

        if (card != null)
            await CardCmd.AutoPlay(choiceContext, card, null);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}