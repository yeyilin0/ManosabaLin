using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class ElevatorTrial : ManosabaCardTemplate
{
    private const int BaseDamage = 15;
    private const int RecursionDamage = 8;

    [SavedProperty]
    private int _increasedDamage;
    private bool _assertedMutable;

    private int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
        }
    }

    public ElevatorTrial() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new IntVar("RecursionDamage", RecursionDamage)
    ];

    private void UpdateDamage()
    {
        DynamicVars.Damage.BaseValue = BaseDamage + IncreasedDamage;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target;

        // 造成伤害
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 递增伤害：本局内每次打出 +8
        var currentBonus = source.IncreasedDamage + RecursionDamage;
        source.IncreasedDamage = currentBonus;
        source.UpdateDamage();

        // 同步到牌组版本
        var deckCard = source.DeckVersion;
        if (deckCard != null && deckCard != source)
        {
            ((ElevatorTrial)deckCard).IncreasedDamage = currentBonus;
            ((ElevatorTrial)deckCard).UpdateDamage();
        }

        // 消耗手牌中1张随机非攻击牌
        var handCards = PileType.Hand.GetPile(source.Owner)
            .Cards
            .Where(c => c != source && c.Type != CardType.Attack)
            .ToList();

        if (handCards.Count > 0)
        {
            var cardToExhaust = handCards
                .StableShuffle(source.Owner.RunState.Rng.Shuffle)
                .First();
            await CardCmd.Exhaust(choiceContext, cardToExhaust);
        }
    }

    // 打出后洗回抽牌堆，而非进入弃牌堆
    protected override PileType GetResultPileTypeForCardPlay()
    {
        var resultPileType = base.GetResultPileTypeForCardPlay();
        return resultPileType != PileType.Discard ? resultPileType : PileType.Draw;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);       // 15 → 20
        DynamicVars["RecursionDamage"].UpgradeValueBy(3m); // 8 → 11
    }
}
