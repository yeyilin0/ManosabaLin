using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class DeathRewindPower : ManosabaPowerTemplate
{
    public override PowerType Type => (PowerType)1;

    public override PowerStackType StackType => (PowerStackType)2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new("HealPercent", 0m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WithPower>()];

    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        SyncHealPercent();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is WithPower && power.Owner == Owner)
            SyncHealPercent();
        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner || Amount < 1) return true;

        // 魔女化 >= 300 时，复活能力不生效
        var withAmount = creature.GetPowerAmount<WithPower>();
        if (withAmount >= 300m)
            return true;

        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        var withAmount = creature.GetPowerAmount<WithPower>();
        var healPercent = Math.Min(100m, withAmount);
        await PowerCmd.Remove<DeathRewindPower>(creature);
        var num = Math.Max(1m, creature.MaxHp * (healPercent / 100m));
        await CreatureCmd.Heal(creature, num);

        // 魔女化 >= 200 时，战斗后移除牌组中所有死亡回溯
        if (withAmount >= 200m)
        {
            var player = creature.Player;
            if (player != null)
            {
                CombatManager.Instance.CombatEnded += OnCombatEnded;
                async void OnCombatEnded(CombatRoom room)
                {
                    CombatManager.Instance.CombatEnded -= OnCombatEnded;

                    var deckCards = PileType.Deck.GetPile(player).Cards
                        .Where(c => c is DeathRewind).ToList();
                    foreach (var c in deckCards)
                        await CardPileCmd.RemoveFromDeck(c, showPreview: false);
                }
            }
        }
    }

    private void SyncHealPercent()
    {
        if (Owner != null)
            DynamicVars["HealPercent"].BaseValue = Math.Min(100m, Owner.GetPowerAmount<WithPower>());
    }
}
