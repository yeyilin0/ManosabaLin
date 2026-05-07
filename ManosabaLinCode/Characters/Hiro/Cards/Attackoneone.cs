using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Attackoneone : ManosabaCardTemplate
{
    private const int BaseDamage = 7;
    private const int BasePerjury = 1;
    private int _increasedDamage;
    private int _increasedPerjury;

    public Attackoneone() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PerjuryPower>(); }
    }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { TransmigrationRules.TransmigrationKeywordId };

    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set { AssertMutable(); _increasedDamage = value; UpdateDamage(); }
    }

    [SavedProperty]
    public int IncreasedPerjury
    {
        get => _increasedPerjury;
        set { AssertMutable(); _increasedPerjury = value; }
    }

    public int CurrentDamage
    {
        get => BaseDamage + IncreasedDamage;
        set { AssertMutable(); DynamicVars.Damage.BaseValue = value; }
    }

    public int CurrentPerjury => BasePerjury + IncreasedPerjury;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(CurrentDamage, ValueProp.Move),
        new IntVar("Increase", 3),
        new IntVar("PerjuryIncrease", 1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 随机给一张手牌加轮回
        var handCards = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c != source).ToList();

        if (handCards.Count > 0)
        {
            var chosen = handCards[new System.Random().Next(handCards.Count)];
            chosen.AddModKeyword(TransmigrationRules.TransmigrationKeywordId);
        }

        await PowerCmd.Apply<PerjuryPower>(
            choiceContext, source.Owner.Creature, CurrentPerjury,
            source.Owner.Creature, source, false
        );

        var damageIncrease = source.DynamicVars["Increase"].IntValue;
        var perjuryIncrease = source.DynamicVars["PerjuryIncrease"].IntValue;
        source.BuffFromPlay(damageIncrease, perjuryIncrease);

        if (source.DeckVersion is Attackoneone deckVersion)
            deckVersion.BuffFromPlay(damageIncrease, perjuryIncrease);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Increase"].UpgradeValueBy(1);
        DynamicVars["PerjuryIncrease"].UpgradeValueBy(1);
    }

    public void BuffFromPlay(int extraDamage, int extraPerjury)
    {
        IncreasedDamage += extraDamage;
        IncreasedPerjury += extraPerjury;
        UpdateDamage();
    }

    public void UpdateDamage() => CurrentDamage = BaseDamage + IncreasedDamage;
}