using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Extensions;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class ElevatorTrial : ManosabaCardTemplate
{
    private const int BaseDamage = 15;
    private const int UpgradeDamage = 5;
    private const int RecursionDamage = 8;
    private int _increasedDamage;

    [SavedProperty]
    private int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
            UpdateDamage();
        }
    }

    public ElevatorTrial() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new IntVar("RecursionDamage", RecursionDamage)
    ];

    private void UpdateDamage()
    {
        DynamicVars.Damage.BaseValue = BaseDamage + UpgradeDamage * CurrentUpgradeLevel + IncreasedDamage;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        source.UpdateDamage();

        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        source.IncreasedDamage += RecursionDamage;
        source.UpdateDamage();

        var deckCard = source.DeckVersion;
        if (deckCard != null && deckCard != source)
        {
            ((ElevatorTrial)deckCard).IncreasedDamage = source.IncreasedDamage;
            ((ElevatorTrial)deckCard).UpdateDamage();
        }

        var handCards = PileType.Hand.GetPile(source.Owner)
            .Cards
            .Where(c => c != source && c.Type != CardType.Attack)
            .ToList();

        if (handCards.Count > 0)
        {
            var cardToExhaust = handCards
                .StableShuffle(source.Owner.RunState.Rng.Shuffle)
                .First();
            await CardCmd.Exhaust(choiceContext, cardToExhaust);
        }
    }

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        var resultLocation = base.GetResultLocationForCardPlayC();
        return resultLocation.pileType != PileType.Discard
            ? resultLocation
            : new CardLocation(resultLocation.player, PileType.Draw, resultLocation.position);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
        DynamicVars["RecursionDamage"].UpgradeValueBy(3m);
    }
}
