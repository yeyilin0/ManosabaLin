using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
[RegisterCharacterStarterCard(typeof(Sherrylin))]
public sealed class WitchTrialEmotion() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmotionPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<EmotionPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);

        await PowerCmd.Apply<WithPower>(
            choiceContext, source.Owner.Creature, 20,
            source.Owner.Creature, source, false);

        if (IsUpgraded)
            await CardPileCmd.Draw(choiceContext, 1, source.Owner);

        var withPower = source.Owner.Creature.GetPower<WithPower>();
        if (withPower != null && withPower.Amount >= 100)
        {
            await PowerCmd.Remove(withPower);
            await CardPileCmd.RemoveFromCombat(source);
        }
        else
        {
            var hand = PileType.Hand.GetPile(source.Owner).Cards
                .Where(c => c != source).ToList();
            if (hand.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var toExhaust = hand[rng.NextInt(hand.Count)];
                await CardCmd.Exhaust(choiceContext, toExhaust);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Exhaust);
    }
}