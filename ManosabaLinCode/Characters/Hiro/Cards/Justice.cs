using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Justice : ManosabaCardTemplate
{
    public Justice() : base(4, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return EnergyHoverTip;
            yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
            yield return HoverTipFactory.FromCard<Save>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(100m),
        new PowerVar<JusticePower>(3m),
        new EnergyVar(3),
        new CardsVar(3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        ManosabaAudio.TryPlayOneShot("justice_theme.mp3".BgmAudioPath());

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 1. 自己获得 100 层魔女化
        await PowerCmd.Apply<WithPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["WithPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 2. 生成一张 Save Token 卡加入手牌
        await Save.CreateInHand(source.Owner, source.CombatState);
        await Cmd.Wait(0.1f);

        // 3. 选择三张在抽牌堆和弃牌堆的攻击牌
        var drawPile = PileType.Draw.GetPile(source.Owner);
        var discardPile = PileType.Discard.GetPile(source.Owner);
        var availableCards = drawPile.Cards.Concat(discardPile.Cards)
            .Where(c => c.Type == CardType.Attack).ToList();

        if (availableCards.Any())
        {
            var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 0, 3)
            {
                PretendCardsCanBePlayed = true
            };

            var selectedCards = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                availableCards,
                source.Owner,
                prefs
            );

            // 4. 让选中的牌本回合免费，并打出
            foreach (var card in selectedCards)
            {
                if (!card.EnergyCost.CostsX)
                    card.SetToFreeThisTurn();

                if (card is CardModel attackCard && attackCard.Type == CardType.Attack)
                {
                    Creature? target = null;
                    if (attackCard.TargetType == TargetType.AnyEnemy || attackCard.TargetType == TargetType.RandomEnemy)
                    {
                        var enemies = source.CombatState.GetOpponentsOf(source.Owner.Creature)
                            .Where(c => c.IsAlive).ToList();
                        if (enemies.Any()) target = source.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
                    }

                    if (target != null)
                    {
                        var attackCmd = await DamageCmd.Attack(attackCard.DynamicVars.Damage.BaseValue)
                            .FromCard(attackCard)
                            .Targeting(target)
                            .WithHitFx("vfx/vfx_attack_slash")
                            .Execute(choiceContext);

                        var totalDamage = attackCmd.Results
                            .SelectMany(r => r)
                            .Sum(r => r.TotalDamage + r.OverkillDamage);
                        if (totalDamage > 0)
                            await CreatureCmd.GainBlock(
                                source.Owner.Creature,
                                totalDamage,
                                ValueProp.Move,
                                cardPlay
                            );
                    }
                }
            }
        }

        // 5. 下回合获得增益
        await PowerCmd.Apply<DrawCardsNextTurnPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars.Cards.BaseValue,
            source.Owner.Creature,
            source
        );

        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars.Energy.BaseValue,
            source.Owner.Creature,
            source
        );

        await PowerCmd.Apply<JusticeNextTurnPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["JusticePower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}