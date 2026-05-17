using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSix : ManosabaCardTemplate
{
    public CardSix() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerjuryPower>();
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return HoverTipFactory.FromCard<Hiroparanoid>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("ParanoidCount", 1m); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 根据伪证层数抽牌
        var perjury = source.Owner.Creature.GetPower<PerjuryPower>();
        var cardsToDraw = perjury?.Amount ?? 0;
        if (cardsToDraw > 0) await CardPileCmd.Draw(choiceContext, cardsToDraw, source.Owner);

        // 根据正义层数回复能量
        var justice = source.Owner.Creature.GetPower<JusticePower>();
        var energyToGain = justice?.Amount ?? 0;
        if (energyToGain > 0) await PlayerCmd.GainEnergy(energyToGain, source.Owner);

        // 将 Hiroparanoid 加入抽牌堆
        var paranoid = source.CombatState.CreateCard<Hiroparanoid>(source.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(paranoid, PileType.Draw, source.Owner,
            CardPilePosition.Random));

        await Cmd.Wait(0.5f);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        RemoveKeyword(CardKeyword.Exhaust);
        EnergyCost.UpgradeBy(-1);
    }
}