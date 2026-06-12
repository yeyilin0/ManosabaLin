using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainGuide() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Innate;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);
        var drawPile = PileType.Draw.GetPile(source.Owner).Cards
            .OfType<ComponentsCardModel>()
            .Where(c => ((IComponentsCardModel)c).HasComponent<RetainCounterComponent>())
            .Cast<CardModel>()
            .ToList();

        var rng = source.Owner.RunState.Rng.CombatCardSelection;

        if (drawPile.Count > 0)
        {
            var card = drawPile[rng.NextInt(drawPile.Count)];
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, source);
        }

        if (IsUpgraded)
        {
            var remaining = PileType.Draw.GetPile(source.Owner).Cards
                .OfType<ComponentsCardModel>()
                .Where(c => ((IComponentsCardModel)c).HasComponent<RetainCounterComponent>())
                .Cast<CardModel>()
                .ToList();

            if (remaining.Count > 0)
            {
                var card = remaining[rng.NextInt(remaining.Count)];
                await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, source);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}