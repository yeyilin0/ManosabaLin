using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 魔女之力：消耗一半额外牌堆卡牌，获得3层魔女仪式、1层无实体、1层追忆。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class SherrylinWitchPower() : ManosabaCardTemplate(4, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
   

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
            yield return HoverTipFactory.FromPower<IntangiblePower>();
            yield return HoverTipFactory.FromPower<NostalgiaPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RitualCeremonyPower>(3m),
        new PowerVar<IntangiblePower>(1m),
        new PowerVar<NostalgiaPower>(1m),
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 消耗一半额外牌堆卡牌
        var caseFilePile = MainFile.CaseFilePile.GetPile(source.Owner);
        var cards = caseFilePile.Cards.ToList();
        var removeCount = cards.Count / 2;

        var rng = source.Owner.RunState.Rng.CombatCardSelection;
        var shuffled = cards.OrderBy(_ => rng.NextFloat()).ToList();

        for (int i = 0; i < removeCount; i++)
        {
            await CardPileCmd.RemoveFromCombat(shuffled[i]);
        }

        // 获得3层魔女仪式
        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["RitualCeremonyPower"].BaseValue,
            source.Owner.Creature, source, false);

        // 获得1层无实体
        await PowerCmd.Apply<IntangiblePower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["IntangiblePower"].BaseValue,
            source.Owner.Creature, source, false);

        // 获得1层追忆
        await PowerCmd.Apply<NostalgiaPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["NostalgiaPower"].BaseValue,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
