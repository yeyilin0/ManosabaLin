using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 蓄力引导：固有，抽取一张带保留计数组件的卡，升级抽两张
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainGuide() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Innate;
            yield return CardKeyword.Exhaust;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 从牌堆找带保留计数组件的卡
        var drawPile = PileType.Draw.GetPile(source.Owner).Cards
            .Where(c => c is RetainCounterTest || c is RetainCharge || c is RetainStrike).ToList();

        if (drawPile.Count > 0)
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;
            var card = drawPile[rng.NextInt(drawPile.Count)];
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, source);
        }

        if (IsUpgraded)
        {
            drawPile = PileType.Draw.GetPile(source.Owner).Cards
                .Where(c => c is RetainCounterTest || c is RetainCharge || c is RetainStrike).ToList();
            if (drawPile.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var card = drawPile[rng.NextInt(drawPile.Count)];
                await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, source);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
