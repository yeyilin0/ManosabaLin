using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardEightyThree : ManosabaCardTemplate
{
    private const string DrawCountKey = "DrawCount";

    public CardEightyThree()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 固定基础值 1
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar(DrawCountKey, 1)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { TransmigrationRules.TransmigrationKeywordId.GetModCardKeyword() };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 检查是否有嫌疑
        var suspect = owner.Creature.Powers.OfType<SuspectPower>().FirstOrDefault();
        if (suspect == null || suspect.Amount < 1)
            return;

        // 消耗一层嫌疑
        if (suspect.Amount == 1)
            await PowerCmd.Remove(suspect);
        else
            await PowerCmd.Apply<SuspectPower>(choiceContext, owner.Creature, -1, owner.Creature, source, false);

        var drawCount = source.DynamicVars[DrawCountKey].IntValue;

        // 从抽牌堆中找出所有带轮回的卡
        List<CardModel> rebirthCards = new();
        foreach (var c in PileType.Draw.GetPile(owner).Cards)
        foreach (var kw in c.Keywords)
            if (kw.ToString() == TransmigrationRules.TransmigrationKeywordId)
            {
                rebirthCards.Add(c);
                break;
            }

        // 随机抽取指定数量
        var actualDraw = Math.Min(drawCount, rebirthCards.Count);
        for (var i = 0; i < actualDraw; i++)
        {
            var card = owner.RunState.Rng.CombatCardSelection.NextItem(rebirthCards);
            if (card != null)
            {
                rebirthCards.Remove(card);
                await CardPileCmd.Add(card, (PileType)2, (CardPilePosition)1, null, false);
                CardCmd.Preview(card);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[DrawCountKey].UpgradeValueBy(1);
    }
}