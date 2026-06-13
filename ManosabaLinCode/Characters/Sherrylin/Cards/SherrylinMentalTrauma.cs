using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
[RegisterCharacterStarterCard(typeof(Sherrylin))]
[RegisterArchaicToothTranscendence(typeof(TheFool))]
public class SherrylinMentalTrauma : ManosabaCardTemplate
{
    public SherrylinMentalTrauma() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<EmotionPower>(
            choiceContext, Owner.Creature, 1,
            Owner.Creature, this, false);

        if (IsUpgraded)
        {
            var drawPile = PileType.Draw.GetPile(Owner);
            if (drawPile.Cards.Any())
            {
                var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 1);
                var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, drawPile.Cards, Owner, prefs);
                var card = selected.FirstOrDefault();
                if (card != null)
                    await CardPileCmd.Add(card, PileType.Exhaust);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
