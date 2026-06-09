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

/// <summary>
/// 魔女审判：获得一层情绪，获得20魔女化，抽一随机移除一张手卡，打出后若你的魔女化为100则清空你的魔女化然后本战斗移除本卡，升级后获得消耗
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
[RegisterCharacterStarterCard(typeof(Sherrylin))]
public sealed class WitchTrialEmotion() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded)
                yield return CardKeyword.Exhaust;
        }
    }

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

        // 获得1层情绪
        await PowerCmd.Apply<EmotionPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);

        // 获得20魔女化
        await PowerCmd.Apply<WithPower>(
            choiceContext, source.Owner.Creature, 20,
            source.Owner.Creature, source, false);

        // 抽1张
        await CardPileCmd.Draw(choiceContext, 1, source.Owner);

        // 随机移除1张手牌
        var hand = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c != source).ToList();
        if (hand.Count > 0)
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;
            var toRemove = hand[rng.NextInt(hand.Count)];
            await CardPileCmd.RemoveFromCombat(toRemove);
        }

        // 检查魔女化是否为100
        var withPower = source.Owner.Creature.GetPower<WithPower>();
        if (withPower != null && withPower.Amount >= 100)
        {
            await PowerCmd.Remove(withPower);
            await CardPileCmd.RemoveFromCombat(source);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
