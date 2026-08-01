using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardEightySix() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        var resultLocation = base.GetResultLocationForCardPlayC();
        return new CardLocation(resultLocation.player, PileType.None, resultLocation.position);
    }

    protected override async Task OnPlayPhased(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        switch (componentContext.Phase)
        {
            case ComponentPhase.Core:
                ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay));

                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
                return;
            case ComponentPhase.Final:
                var drawPileCards = PileType.Draw.GetPile(Owner).Cards
                    .Where(c => c is IComponentsCardModel)
                    .ToList()
                    .StableShuffle(Owner.RunState.Rng.Shuffle);

                if (drawPileCards.FirstOrDefault() is IComponentsCardModel card)
                    card.AddComponent(new GenerateComponent(this));
                return;
        }
    }
}
