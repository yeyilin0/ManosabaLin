using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Hirodeath : ManosabaCardTemplate
{
    private const int WithPowerReduction = 50;
    private const int EnergyToGive = 3;
    private const int CardsToDraw = 3;

    public Hirodeath() : base(-1, CardType.Skill, CardRarity.Ancient, TargetType.AllAllies)
    {
    }

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("WithReduction", WithPowerReduction);
            yield return new EnergyVar("EnergyGain", EnergyToGive);
            yield return new CardsVar("CardsDraw", CardsToDraw);
        }
    }

    private static IEnumerable<CardModel> GetStatusAndCurseCards(Player owner)
    {
        return owner.PlayerCombatState.AllCards
            .Where(c => (c.Type == CardType.Status || c.Type == CardType.Curse)
                        && c.Pile.Type != PileType.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        ManosabaAudio.TryPlayOneShot("hirodeath_theme.mp3".BgmAudioPath());

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var allPlayers = source.CombatState.Players;
        foreach (var player in allPlayers)
        {
            var cardsToExhaust = GetStatusAndCurseCards(player).ToList();
            foreach (var card in cardsToExhaust) await CardCmd.Exhaust(choiceContext, card);
        }

        var teammates = source.CombatState.GetTeammatesOf(source.Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);

        foreach (var teammate in teammates)
        {
            var withPower = teammate.GetPower<WithPower>();
            if (withPower != null)
            {
                var reduction = Math.Min(WithPowerReduction, withPower.Amount);
                if (reduction > 0)
                    await PowerCmd.ModifyAmount(
                        choiceContext, withPower,
                        -reduction,
                        source.Owner.Creature,
                        source,
                        false
                    );
            }

            await PlayerCmd.GainEnergy(source.DynamicVars["EnergyGain"].BaseValue, teammate.Player);
            await CardPileCmd.Draw(choiceContext, source.DynamicVars["CardsDraw"].BaseValue, teammate.Player);
        }
    }

    protected override void OnUpgrade()
    {
    }
}