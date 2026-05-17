using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSixtyEight() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 固定基础值 1
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new RepeatVar(1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 选择手牌中一张带有轮回关键词的卡牌
        var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 1);
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            source.Owner,
            prefs,
            c => c.HasModKeyword(TransmigrationRules.TransmigrationKeywordId),
            source
        );

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null) return;

        // 复制选中的卡牌到手中
        for (var i = 0; i < source.DynamicVars.Repeat.IntValue; i++)
        {
            var clonedCard = selectedCard.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(clonedCard, PileType.Hand, source.Owner);
            await Cmd.Wait(0.1f);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Repeat.UpgradeValueBy(1);
    }
}
