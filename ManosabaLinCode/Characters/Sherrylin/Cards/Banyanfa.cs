using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class Banyanfa() : ManosabaCardTemplate(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<EmotionHelplessness>();
            yield return HoverTipFactory.FromCard<EmotionCuriosity>();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded)
                yield return CardKeyword.Retain;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var drawPile = PileType.Draw.GetPile(source.Owner);
        var total = drawPile.Cards.Count;
        if (total == 0) return;

        var retainCount = drawPile.Cards.Count(c => c.HasComponent<RetainCounterComponent>());

        CardModel? emotionCard;
        if (retainCount >= total / 2)
        {
            emotionCard = source.CombatState.CreateCard<EmotionHelplessness>(source.Owner);
        }
        else
        {
            emotionCard = source.CombatState.CreateCard<EmotionCuriosity>(source.Owner);
        }

        if (emotionCard != null)
            await CaseFilePileHelper.AddToCaseFilePile(
                emotionCard, source.Owner, CardPilePosition.Top, choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
