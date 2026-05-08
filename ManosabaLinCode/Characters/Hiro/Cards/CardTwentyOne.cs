using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardTwentyOne() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const int RequiredPerjuryAmount = 1;
    private const int RequiredJusticeAmount = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(13m, ValueProp.Move),
        new PowerVar<PerjuryPower>(1m),
        new PowerVar<JusticePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerjuryPower>();
            yield return HoverTipFactory.FromPower<JusticePower>();
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable) return false;

            var perjuryPower = Owner.Creature.GetPower<PerjuryPower>();
            var perjuryAmount = perjuryPower?.Amount ?? 0;

            var justicePower = Owner.Creature.GetPower<JusticePower>();
            var justiceAmount = justicePower?.Amount ?? 0;

            return perjuryAmount >= RequiredPerjuryAmount && justiceAmount >= RequiredJusticeAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 消耗 1 层伪证
        var perjuryPower = source.Owner.Creature.GetPower<PerjuryPower>();
        if (perjuryPower != null && perjuryPower.Amount >= RequiredPerjuryAmount)
            await PowerCmd.ModifyAmount(choiceContext, perjuryPower, -RequiredPerjuryAmount, source.Owner.Creature, source, false);

        // 消耗 1 层正义
        var justicePower = source.Owner.Creature.GetPower<JusticePower>();
        if (justicePower != null && justicePower.Amount >= RequiredJusticeAmount)
            await PowerCmd.ModifyAmount(choiceContext, justicePower, -RequiredJusticeAmount, source.Owner.Creature, source, false);

        // 造成伤害
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 降低敌人力量 — 用 CrushUnderPower
        await PowerCmd.Apply<CrushUnderPower>(
            choiceContext, cardPlay.Target, 3, source.Owner.Creature, source);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var source = this;
        if (player != source.Owner) return;
        if (source.Pile.Type == PileType.Hand) return;
        await CardPileCmd.Add(source, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Damage.UpgradeValueBy(13m);
    }
}