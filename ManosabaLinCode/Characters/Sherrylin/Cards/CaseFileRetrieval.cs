using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class CaseFileRetrieval() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Unpowered)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var caseFilePile = MainFile.CaseFilePile.GetPile(source.Owner);
        var blockAmount = caseFilePile.Cards.Count * source.DynamicVars.Block.BaseValue;

        if (blockAmount > 0)
            await CreatureCmd.GainBlock(source.Owner.Creature, blockAmount, ValueProp.Unpowered, null);

        // 随机给手牌中一张攻击卡添加保留
        var handAttacks = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c.Type == CardType.Attack && c != source)
            .ToList();

        if (handAttacks.Count > 0)
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;
            var target = handAttacks[rng.NextInt(handAttacks.Count)];
            target.AddKeyword(CardKeyword.Retain);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}
