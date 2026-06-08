using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 愤怒球体：从案卷牌堆打出时挂载，生效一回合后自动消散。
/// 回合开始时施加2层橘雪莉的魔法（+2伤害），倒计时结束后源卡返回案卷牌堆。
/// </summary>
public class AngerOrb : OrbModel
{
    public CardModel? SourceCard { get; private set; }

    private int _countdown;

    public override decimal PassiveVal => _countdown;
    public override decimal EvokeVal => 2m;
    public override Color DarkenedColor => new(1f, 0.3f, 0.3f);

    public AngerOrb()
    {
        _countdown = 1;
    }

    public void Init(CardModel sourceCard)
    {
        SourceCard = sourceCard;
        _countdown = 1;
    }

    public static AngerOrb Create(CardModel sourceCard)
    {
        var orb = (AngerOrb)ModelDb.Orb<AngerOrb>().MutableClone();
        orb.Init(sourceCard);
        return orb;
    }

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        if (SourceCard?.Owner == null) return;

        var creature = SourceCard.Owner.Creature;
        if (creature == null) return;

        // 施加2层橘雪莉的魔法（+2伤害，回合结束自动消失）
        await PowerCmd.Apply<XlmPower>(
            ctx, creature, 2,
            creature, null, false);

        // 倒计时-1
        _countdown--;

        // 倒计时归零自动消散
        if (_countdown <= 0)
        {
            await OrbCmd.EvokeNext(ctx, SourceCard.Owner);
        }
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext ctx)
    {
        // 源卡返回案卷牌堆
        if (SourceCard?.Owner != null)
        {
            var copy = SourceCard.CreateClone();
            await CardPileCmd.Add(copy, MainFile.CaseFilePile, CardPilePosition.Top);
        }
        return [];
    }

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;
    public override Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx) => Task.CompletedTask;
}
