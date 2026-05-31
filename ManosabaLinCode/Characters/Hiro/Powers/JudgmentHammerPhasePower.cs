namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class JudgmentHammerPhasePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner.Player is not { } player) return;

        var playerCounts = Owner.CombatState!.Players.Select(p =>
            PileType.Hand.GetPile(p).Cards.Count(c => c.HasComponent<BlackHandComponent>())).ToArray();
        var thisCount = PileType.Hand.GetPile(player).Cards.Count(c => c.HasComponent<BlackHandComponent>());
        if (thisCount == 0) return;
        if (thisCount == playerCounts.Max())
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, Owner, playerCounts.Sum(),
                ValueProp.Unblockable | ValueProp.Unpowered, Owner);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}
