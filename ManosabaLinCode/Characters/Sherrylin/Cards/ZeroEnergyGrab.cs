using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 零能撷取：将2张随机零费卡（全卡池随机）加入手卡，消耗，升级获得三张
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class ZeroEnergyGrab() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardCount", 2m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var cardCount = source.DynamicVars["CardCount"].IntValue;
        var rng = source.Owner.RunState.Rng.CombatCardSelection;

        // 从全卡池获取0费卡（包括所有角色卡池）
        var allPools = source.Owner.UnlockState.CharacterCardPools;
        var zeroCostCards = allPools
            .SelectMany(p => p.AllCards)
            .Where(c => c.EnergyCost.Canonical == 0 && c.Type != CardType.Curse && c.Type != CardType.Status)
            .Distinct()
            .ToList();

        for (int i = 0; i < cardCount && zeroCostCards.Count > 0; i++)
        {
            var idx = rng.NextInt(zeroCostCards.Count);
            var cardModel = zeroCostCards[idx];
            var newCard = source.CombatState.CreateCard(cardModel, source.Owner);
            newCard.AddKeyword(CardKeyword.Exhaust);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, source.Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["CardCount"].UpgradeValueBy(1m);
    }
}
