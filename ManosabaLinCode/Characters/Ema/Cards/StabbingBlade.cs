using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Emalin.Components;
using MinionLib.Component.Interfaces;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class StabbingBlade() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents => [new Witchification()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        var attackCards = PileType.Hand.GetPile(owner).Cards
            .Where(c => c.Type == CardType.Attack && c.Enchantment == null)
            .ToList();

        if (attackCards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
            var selected = await CardSelectCmd.FromHand(
                choiceContext, owner, prefs,
                c => c.Type == CardType.Attack && c is IComponentsCardModel, this);

            var targetCard = selected.FirstOrDefault();
            if (targetCard != null)
            {
                ((IComponentsCardModel)targetCard).AddComponent(new Witchification());

                if (bond != null && bond.Estrangement > bond.Affinity)
                {
                    await CardCmd.AutoPlay(choiceContext, targetCard, null);
                }
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
