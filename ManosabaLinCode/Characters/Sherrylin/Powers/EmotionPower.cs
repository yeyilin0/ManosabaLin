using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class EmotionPower : ManosabaActionTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override TargetType TargetType => TargetType.Self;
    public override bool DecrementAfterAct => false;

    private int _reachThirteenCount;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type == CardType.Power && cardPlay.Card.Rarity == CardRarity.Token) return;

        Amount++;
        Flash();

        if (Amount >= 13)
        {
            Amount = 0;
            _reachThirteenCount++;

            var rng = Owner.Player.RunState.Rng.CombatCardSelection;
            var roll = rng.NextInt(6);

            var combatState = Owner.CombatState;
            if (combatState != null)
            {
                CardModel? emotionCard = roll switch
                {
                    0 => combatState.CreateCard<EmotionAnger>(Owner.Player),
                    1 => combatState.CreateCard<EmotionDisgust>(Owner.Player),
                    2 => combatState.CreateCard<EmotionSadness>(Owner.Player),
                    3 => combatState.CreateCard<EmotionFear>(Owner.Player),
                    4 => combatState.CreateCard<EmotionJoy>(Owner.Player),
                    5 => combatState.CreateCard<EmotionSurprise>(Owner.Player),
                    _ => null
                };

                if (emotionCard != null)
                    await CardPileCmd.Add(emotionCard, MainFile.CaseFilePile, CardPilePosition.Top);

                if (_reachThirteenCount >= 3)
                {
                    _reachThirteenCount = 0;
                    await PowerCmd.Apply<EmotionFusionPower>(
                        choiceContext, Owner, 1, Owner, null, false);
                }
            }
        }
    }

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = Owner.Player;
        if (player == null) return;

        var caseFileCards = MainFile.CaseFilePile.GetPile(player).Cards.ToList();
        if (caseFileCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(new LocString("powers", Id.Entry + ".selectionScreenPrompt"), 0,1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, caseFileCards, player, prefs);
        var selectedList = selected.ToList();
        if (selectedList.Count > 0)
        {
            await CardPileCmd.Add(selectedList[0], PileType.Hand, CardPilePosition.Top);
        }
    }
}
