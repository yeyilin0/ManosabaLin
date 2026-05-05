using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardTwentyEight() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(IsUpgraded ? 10m : 8m, ValueProp.Move),
        new CardsVar(1),
        new PowerVar<PerjuryPower>(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 第一步：获得格挡
        await CreatureCmd.GainBlock(source.Owner.Creature, source.DynamicVars.Block, cardPlay);

        // 第二步：从抽牌堆选择 1 张牌加入手牌（Hologram 风格）
        CardSelectorPrefs prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 1);
        CardModel card = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            PileType.Draw.GetPile(source.Owner).Cards,
            source.Owner,
            prefs
        )).FirstOrDefault();

        if (card != null)
        {
            CardPileAddResult cardPileAddResult = await CardPileCmd.Add(card, PileType.Hand);
        }

        // 第三步：获得 3 层伪证
        await PowerCmd.Apply<PerjuryPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["PerjuryPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(8m);
    }
}