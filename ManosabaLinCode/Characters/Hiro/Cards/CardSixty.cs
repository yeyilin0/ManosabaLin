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
public sealed class CardSixty : ManosabaCardTemplate
{
    private const string SelectCountKey = "SelectCount";
    private const string CopyCountKey = "CopyCount";

    public CardSixty()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 固定基础值
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar(SelectCountKey, 1m),
        new DynamicVar(CopyCountKey, 1m)
    };

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { TransmigrationRules.TransmigrationKeywordId };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        var selectCount = source.DynamicVars[SelectCountKey].IntValue;
        var copyCount = source.DynamicVars[CopyCountKey].IntValue;

        // 选择手牌中的卡牌
        var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, selectCount);
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            prefs,
            null,
            source
        );

        foreach (var originalCard in selectedCards)
        {
            for (var i = 0; i < copyCount; i++)
            {
                var clonedCard = originalCard.CreateClone();

                clonedCard.AddModKeyword(TransmigrationRules.TransmigrationKeywordId);

                CardCmd.Preview(clonedCard);

                await CardPileCmd.AddGeneratedCardToCombat(
                    clonedCard,
                    PileType.Draw,
                    Owner,
                    CardPilePosition.Random
                );
            }

            await Cmd.Wait(0.5f);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[CopyCountKey].UpgradeValueBy(1m);
    }
}
