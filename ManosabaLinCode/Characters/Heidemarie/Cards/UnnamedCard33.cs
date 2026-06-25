using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnnamedCard33() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string AuroraCountKey = "AuroraCount";
    public const string DiscardCountKey = "DiscardCount";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AuroraCountKey, 1m),
        new CardsVar(1),
        new DynamicVar(DiscardCountKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var auroraCount = DynamicVars[AuroraCountKey].IntValue;
        var drawCount = DynamicVars.Cards.IntValue;

        await SwordTokenGeneration.AddAuroraSwordsToHand(choiceContext, Owner, auroraCount, this);
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);

        if (!IsUpgraded)
            return;

        var discardCount = DynamicVars[DiscardCountKey].IntValue;
        if (discardCount <= 0)
            return;

        var selected = (await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, discardCount),
            null,
            this)).ToArray();

        if (selected.Length == 0)
            return;

        await CardCmd.Discard(choiceContext, selected);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars[DiscardCountKey].UpgradeValueBy(1m);
    }
}
