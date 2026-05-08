using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Three : ManosabaCardTemplate
{
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Skill;
    private const CardRarity Rarity = CardRarity.Uncommon;
    private const TargetType TargetType = MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyPlayer;

    private const int WithPowerReduction = 20;
    private const int CardsToDraw = 1;

    public Three() : base(EnergyCost, Type, Rarity, TargetType)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WithPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>("Reduction", WithPowerReduction),
        new CardsVar("Draw", CardsToDraw)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 1. 降低目标 20 层魔女化
        var targetWithPower = target.GetPower<WithPower>();
        if (targetWithPower != null)
        {
            var reductionAmount = Math.Min(source.DynamicVars["Reduction"].IntValue, (int)targetWithPower.Amount);
            if (reductionAmount > 0)
                await PowerCmd.ModifyAmount(
                    choiceContext, targetWithPower,
                    -reductionAmount,
                    source.Owner.Creature,
                    source,
                    false
                );
        }

        await Cmd.Wait(0.2f);

        // 2. 自己抽牌
        await CardPileCmd.Draw(
            choiceContext,
            source.DynamicVars["Draw"].IntValue,
            source.Owner,
            false
        );

        // 3. 目标抽牌
        if (target.IsPlayer)
        {
            var targetPlayer = source.CombatState.Players.FirstOrDefault(p => p.Creature == target);
            if (targetPlayer != null)
                await CardPileCmd.Draw(
                    choiceContext,
                    source.DynamicVars["Draw"].IntValue,
                    targetPlayer,
                    false
                );
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1m);
    }
}