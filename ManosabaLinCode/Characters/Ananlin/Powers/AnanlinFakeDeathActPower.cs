using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinFakeDeathActPower : ManosabaPowerTemplate
{
    [SavedProperty] public int LostPeace { get; set; }
    [SavedProperty] public bool RewardMarginPagesOnTrigger { get; set; }
    [SavedProperty] public bool Triggered { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<BlankPage>(),
        HoverTipFactory.FromCard<MarginPage>()
    ];

    public override bool ShouldDie(Creature creature)
    {
        return creature != Owner || Triggered;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner || Triggered) return;

        Triggered = true;
        Flash();
        if (Owner.CurrentHp < 1)
            await CreatureCmd.SetCurrentHp(Owner, 1m);

        await AddRewardPages(Math.Max(0, LostPeace), RewardMarginPagesOnTrigger);
        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Triggered || !participants.Contains(Owner)) return;

        await AddRewardPages(1, marginPages: true);
        await PowerCmd.Remove(this);
    }

    private async Task AddRewardPages(int count, bool marginPages)
    {
        if (count <= 0) return;
        if (Owner.Player is not { } player) return;
        if (Owner.CombatState is not { } combatState) return;
        var blessedObject = player.Relics.OfType<BlessedObject>().FirstOrDefault();

        for (var i = 0; i < count; i++)
        {
            CardModel page = marginPages
                ? combatState.CreateCard<MarginPage>(player)
                : blessedObject is not null
                    ? blessedObject.CreateBlankPageOrReplacement(combatState, upgraded: false, player)
                    : combatState.CreateCard<BlankPage>(player);
            await CardPileCmd.AddGeneratedCardToCombat(page, PileType.Hand, player);
        }
    }
}
