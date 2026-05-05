using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardEightyOne() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new BlockVar(IsUpgraded ? 8m : 5m, ValueProp.Move),
        new DynamicVar("Cards", 1m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var owner = source.Owner;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 获得格挡
        await CreatureCmd.GainBlock(owner.Creature, source.DynamicVars.Block, cardPlay);

        // 选择手牌中的卡放回抽牌堆
        var returnCount = source.DynamicVars["Cards"].IntValue;
        var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, returnCount);
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            prefs,
            null,
            source
        );

        // 将选中的卡放回抽牌堆
        foreach (var card in selectedCards) await CardPileCmd.Add(card, (PileType)1, (CardPilePosition)1, null, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["Cards"].UpgradeValueBy(1m); // 放回数量 1 → 2
    }
}