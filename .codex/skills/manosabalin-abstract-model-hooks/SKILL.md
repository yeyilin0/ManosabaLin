---
name: manosabalin-abstract-model-hooks
description: Choose and implement common AbstractModel hooks for ManosabaLin cards, relics, powers, monsters, and RitsuLib model capabilities. Use when deciding between Before/After/Modify/TryModify hooks, owner hook capabilities, combat hook participation, clone lifecycle, or command-safe gameplay timing.
---

# ManosabaLin AbstractModel Hooks

## First Rule

Read the actual signatures before editing. 本项目可用的引擎反编译源码在 `D:\ManosabaLin\.local\probe_engine\decomp\`（`.local/` 被 gitignore ⇒ 检索工具必须显式传 `path=`）：

- Base game: `...\decomp\MegaCrit.Sts2.Core.Models\AbstractModel.cs`
- Cards: `...\decomp\MegaCrit.Sts2.Core.Models\CardModel.cs`
- Relics: `...\decomp\MegaCrit.Sts2.Core.Models\RelicModel.cs`
- Powers: `...\decomp\MegaCrit.Sts2.Core.Models\PowerModel.cs`
- Monsters: `...\decomp\MegaCrit.Sts2.Core.Models\MonsterModel.cs`
- Rooms / 奖励: `...\decomp\MegaCrit.Sts2.Core.Rooms\CombatRoom.cs`
- RitsuLib: `D:\ManosabaLin\.local\external\STS2-RitsuLib\src\`（含 `Models\HookedSingletonModel.cs`）

Do not guess nullability, `PlayerChoiceContext`, or return types.

临时查单个类型也可以直接反编译（产物落到 `.local/` 便于检索）：

```bash
D="D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64"
/c/Users/Lenovo/.dotnet/tools/ilspycmd -t MegaCrit.Sts2.Core.Models.CardModel "$D/sts2.dll" > .local/ilspy_cardmodel.cs
```

## Hook Selection

- Use `Before...` hooks to validate, prepare, or mutate state before the game action.
- Use `After...` hooks for side effects after the action succeeds.
- Use `Modify...` hooks for pure value changes; return only the delta or replacement shape the base method expects.
- Use `TryModify...` hooks when the hook needs to report whether it changed an object/list/value.
- Use `AfterModifying...` hooks to perform follow-up side effects after a modifying hook has been selected.

Common hooks:

- Combat lifecycle: `BeforeCombatStart`, `BeforeCombatStartLate`, `AfterCombatEnd`, `AfterCombatVictory`, `AfterCreatureAddedToCombat`.
- Turn lifecycle: `BeforeSideTurnStart`, `AfterSideTurnStart`, `AfterPlayerTurnStart`, `BeforeSideTurnEnd`, `AfterSideTurnEnd`, late/early variants.
- Card flow: `AfterCardEnteredCombat`, `AfterCardGeneratedForCombat`, `AfterCardChangedPiles`, `AfterCardDrawn`, `AfterCardDiscarded`, `AfterCardExhausted`.
- Card play: `BeforeCardPlayed`, `AfterCardPlayed`, `AfterCardPlayedLate`, `ModifyCardPlayCount`, `ModifyCardPlayResultPileTypeAndPosition`.
- Damage/block: `BeforeAttack`, `AfterAttack`, `ModifyAttackHitCount`, `ModifyDamageAdditive`, `ModifyDamageMultiplicative`, `BeforeDamageReceived`, `AfterDamageReceived`, `ModifyBlockAdditive`, `AfterBlockGained`.
- Powers: `BeforePowerAmountChanged`, `AfterPowerAmountChanged`, `ModifyPowerAmountGivenAdditive`, `TryModifyPowerAmountReceived`.
- Economy/rewards/map: `AfterGoldGained`, `ModifyGoldGained`, `TryModifyRewards`, `AfterRewardTaken`, `ModifyGeneratedMap`, `AfterRoomEntered`.

## Reward Offer Hooks & Save Restore (⚠️ read before using `BeforeCombatRewardOffered`)

`CombatRoom.OfferRoomEndRewards()` is reached from **two very different contexts**:

| Context | Call path | Room node mode | State |
|---|---|---|---|
| 正常打完战斗 | `CombatManager.CombatWon` → `NCombatUi.OnCombatWon` → `ShowRewards` | `CombatRoomMode.ActiveCombat` | UI 就绪、画面正常 |
| **读档恢复**（战斗已结束、只是重新进房间） | `RunManager.EnterRoomInternal` → `CombatRoom.EnterInternal` → `StartPreFinishedCombat` | `CombatRoomMode.FinishedCombat` | 画面已 FadeOut 到全黑、`FadeIn` 要等整条链返回后才执行 |

`OfferRoomEndRewards()` 内部顺序是：`RewardsCmd.GenerateForRoomEnd` → **`await Hook.BeforeCombatRewardOffered(...)`** → `TaskHelper.RunSafely(reward.Offer())`。

⇒ 在 `BeforeCombatRewardOffered` 里 `await` 任何**交互式屏幕**（尤其 `CardSelectCmd.*` 模态选卡），**正常战斗没问题，但读档恢复会把整次加载挂死在黑屏上**（奖励屏都还没出现）；玩家只能强退，强退取消会打断房间恢复链，最终在 `RunManager.EnterRoomInternal` 抛 `NullReferenceException`。

**用法**：hook 里必须按「读档恢复」跳过，判据是引擎专门为这个场景提供的房间节点模式：

```csharp
if (NCombatRoom.Instance?.Mode == CombatRoomMode.FinishedCombat) return false;
```

（`CombatRoomMode.FinishedCombat` 文档原文：*"Used when loading a save after combat ended but before leaving the room."*；正常战斗中节点模式恒为 `ActiveCombat`，`NCombatRoom.Mode` 只在 `NCombatRoom.Create` 里赋值。）

注意 `CombatRoom.IsPreFinished` **不能**用来区分——`CombatManager.EndCombatInternal` 在战斗一结束就 `MarkPreFinished()`，两条路径都是 `true`。

若要做「每个首领只发一次」的幂等，别用机器本地 `HashSet`（联机两端会判断分叉而卡同步）；本奖励是靠 `PlayerChoiceSynchronizer` 同步模态选卡的。

## Model-Specific Notes

- `CardModel` hooks for the card itself include `AfterCreated`, `AfterTransformedFrom`, `AfterTransformedTo`, `OnEnqueuePlayVfx`, plus `OnPlay`/`OnUpgrade` through `ManosabaCardTemplate`.
- `RelicModel` has `AfterObtained`, `AfterRemoved`, `IsAllowed`, `IsAllowedAtNeow`, `ShowCounter`, and `DisplayAmount`.
- `PowerModel` has `BeforeApplied`, `AfterApplied`, `AfterRemoved`, `ShouldPowerBeRemovedAfterOwnerDeath`, `PowerType`, `PowerStackType`, and `AllowNegative`.
- `MonsterModel` has `AfterAddedToRoom`, `BeforeRemovedFromRoom`, `AfterDeath`, and move-state machinery.

## Combat Hook Participation

- `AbstractModel.ShouldReceiveCombatHooks` controls whether combat hooks are called.
- Cards in combat piles, powers, relics, and active combat models normally receive relevant hooks.
- Disconnected canonical models should not be treated as mutable runtime state.
- Use `AssertMutable()` only to guard against misuse; do not paper over canonical/mutable mistakes by cloning at the wrong layer.

## RitsuLib Capability Hooks

- `OwnerHookCapability<TModel>` lets a capability receive the owning model's vanilla hooks.
- `CardCapability` receives card lifecycle helpers such as owner card upgraded/downgraded/transformed.
- `IModelCapabilityHookListener.OwnerHookOrder` controls ordering: negative before owner, zero/positive after owner.
- Gameplay-affecting multiplayer logic should use awaited vanilla hooks or RitsuLib owner-hook capabilities, not fire-and-forget side channels.

## ⚠️★ 联机：hook 里改「回合 / 队列状态」必须走 `ActionQueue`

**症状**：多人联机时某机制触发后「后续出牌的人掉线」（`StateDivergenceException` / checksum mismatch）。

**病根（两层，缺一不可）**：

1. **hook 的执行时机不是确定性的**：`PowerCmd.Apply` 里 `Hook.AfterPowerAmountChanged` 是在
   `await Cmd.CustomScaledWait(0.1f, 0.25f)` **之后**才触发的，而这个时长取自**玩家本机设置**
   `SaveManager.Instance.PrefsSave.FastMode`（`Instant` 完全不等 / `Fast` 0.1s / `Normal` 0.25s）
   ⇒ 同一个逻辑事件在各端的恢复时刻最多差 0.25s。
2. **上游常常还是 fire-and-forget**：同步 hook（如 `ModifyHpLostBeforeOsty` 返回 `Decimal`）里没法
   `await`，于是写成 `_ = PowerCmd.Apply<X>(...)` ⇒ 整条 apply 链**彻底脱离 ActionQueue 执行栈**。

⇒ 在这个时机里**直接改会影响同步的状态**（`CombatManager` 的 ready 集合、牌堆、回合状态），
会出现「一端已判定回合结束并开始收尾，另一端队列里还有出牌在结算」 ⇒ 分歧掉线。

**正解 = 把这件事变成 `GameAction`，用队列入队，让 host 权威排序**：

```csharp
var combatState = player.Creature.CombatState;
if (combatState == null || CombatManager.Instance.IsOverOrEnding) return Task.CompletedTask;

// ⭐ 每个端**只为本机玩家**入队。远程玩家的动作只能由他自己的端发起；
//    host 代发会让 EnqueueAction 的 ownerId 错位（RequestEnqueue 在 host 上固定用 host 的 NetId）。
var me = LocalContext.GetMe(combatState);
if (me == null || !me.Creature.IsAlive) return Task.CompletedTask;
if (CombatManager.Instance.IsPlayerReadyToEndTurn(me)) return Task.CompletedTask;

RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
    new ForceEndPlayerTurnAction(me, me.PlayerCombatState.TurnNumber));
```

- 「**任一玩家触发 → 影响所有玩家**」的机制：因为 hook 在所有端都执行，只要让**每个端各自决定是否为本机玩家入队**，
  就等于全员都入队了 —— 队列顺序由 host 统一广播，两端一致。
- `RequestEnqueue` 语义：host 立即入队 + 广播；client 只发消息，由 host 入队后广播回来。
  ⇒ **不要在所有端都无条件调用同一个"代表别人"的 action**（会重复入队）。
- 先例：`AamPower.ChooseMoveAndTarget` / `LymPower` —— 都先判 `LymPower.IsLocalPlayer(player)` 才 `RequestEnqueue`。

**动作本体怎么写**：

- 引擎的 `EndPlayerTurnAction` **硬编码 `canBackOut: true`**：`NEndTurnButton.AfterPlayerEndedTurn` 里
  `PlayerCanTakeAction()` 只查 `IsAlive` + `PlayersTakingExtraTurn`（**不查** `PlayerActionsDisabled`）
  ⇒ 按钮会变成可点的「撤销结束回合」。**强制类机制（惩罚）必须自定义 action 传 `canBackOut: false`**，
  否则玩家撤销后能继续出牌、机制失效。
- 新动作要存 `turnNumber` 并在 `ExecuteAction` 里比对（与 `EndPlayerTurnAction` 同构），
  避免延迟入队的 action 落到下一回合。
- 双子文件样板：`XxxAction : GameAction`（`ExecuteAction` / `ToNetAction` / `OwnerId` / `ActionType`）
  + `NetXxxAction : INetAction`（`Serialize` / `Deserialize` / `ToGameAction(Player)`）。
  照抄 `Characters/Common/GameActions/AamPowerChosenAction.cs` + `NetAamPowerChosenAction.cs`。
- 引擎 `ActionTypes.Initialize()` = `INetActionSubtypes.All` + `ReflectionHelper.GetSubtypesInMods<INetAction>()`
  ⇒ **mod 里的 `INetAction` 会被自动收录**，不用手写注册。
  ⚠️ ID 由 `ContentSorter` **按类型名确定性排序**分配 ⇒ 新增网络类型会改变后续类型的 ID，
  **联机双方必须使用同一个 mod 版本**。
- `PlayerCmd.EndTurn(player, canBackOut)` 本身**只是"标记该玩家 ready"**；真正的回合结束由
  `CombatManager` 在「全员 ready」后推进（`AfterAllPlayersReadyToEndTurn`）。
  它内部还会 `OnEndedTurnLocally()` → `PlayerActionsDisabled = true`，而**这一步只在本地端生效**（`LocalContext.IsMe`）
  —— 这正是「绕过队列直接调用」会掉线的直接原因。

**已知同类隐患**（同一根因，尚未处理）：`ThirteenWaterPlayerIntelPower.AfterPowerAmountChanged` 在
`Amount >= 13` 时给**所有玩家**发卡（`CardPileCmd.AddGeneratedCardToCombat` + 牌堆 `Random` 位置），
也跑在同一个非确定时机里。

## ⚠️★ 联机：改「牌的费用」的代码必须两端选出同一张牌

**症状**：`State divergence detected! ... Context: After player turn start`，随后主机
`Disconnecting peer <id>, reason: StateDivergence` 把队友踢掉（2026-10-15 实测）。

**病根**：`CardState.From` 会把 `card.EnergyCost.GetWithModifiers(CostModifiers.All)` 写进
`CardState.energyCost`（仅当 ≠ `Canonical`）⇒ **牌的费用是校验和的一部分**。
任何「免费 / 降费」效果只要在两端的**候选集**或**选中结果**上不一致，就直接分歧。

**最典型的错**：用 `CanPlay()` 给候选集过滤。它把 `EnergyCostTooHigh / StarCostTooHigh` 一起算进来，
而能量是随行动推进**瞬时变化**的 ⇒ 同一 `OnPlay` 两端算出不同候选集，再叠 `Rng...NextItem()`
⇒ 一端把 A 设为 0 费、另一端设 B（或谁都没设）。

**正解**：用 `EmalinCombatHelper.IsEligibleForCostEffect(card)` —— 屏蔽资源类原因，只保留
Unplayable 关键词 / 目标非法 / Hook 拦截这类两端一致的否决（口径同 `YalisalinFireComponentResolver`）。
⚠️ 例外：`Dismantledparts` 那种「循环打出**现在付得起**的攻击牌」的语义本身就该用 `CanPlay()`，保留；
纯视觉用途（发光色、卡面样式）用 `CanPlay()` 也安全。

**另外两条**：
- **决定阈值/分支/选牌的计数必须 `[SavedProperty]`**（卡、遗物上有效；**能力上的 `SavedProperty` 不参与活体同步**，
  `PowerState` 只传 id + amount）。
- **打完卡结算完的长卡顿会导致队友 `Reason: 4001, Timeout` 掉线**（不是分歧），修性能；但掉线重连后
  未同步状态会丢，常接着再来一次 StateDivergence —— 两件事都要处理。

📖 完整的取证流程（RitsuLib 分歧包位置、`LOCAL/REMOTE STATE DUMP` 自己 diff、传输模型对照表）：
技能 **`manosabalin-multiplayer-desync`**。

## ⚠️ 自动打出（`CardCmd.AutoPlay`）与 `PlayerChoiceContext` 模型栈

`CardModel.OnPlayWrapper` 的形状是 **`choiceContext.PushModel(this)`（方法第 2 行）→ … → `choiceContext.PopModel(this)`（方法最后一行）**，中间有若干 **early `return`**（`combatState == null`、`Owner.Creature.IsDead` ×4）。

⇒ 只要在 `PushModel` 之后提前 return，`PopModel` 就被跳过；而 `PlayerChoiceContext.PopModel` 在「栈顶不是该 model」时**只 `Log.Error`、不弹栈**（非破坏性）⇒ 该 context 的栈**永久错位**，之后每一次 pop 都会继续报错：

```
Tried to pop model <X> from stack of player choice context ... but <Y> was on the top of the stack instead! (Stack size: N)
  at PlayerChoiceContext.PopModel
  at Hook+<AfterCardPlayed>d__16.MoveNext_Patch1
```

`Hook.AfterCardPlayed` / `Hook.BeforeCardPlayed` 同样是 `PushModel → await model.Xxx() → PopModel`，且**没有 try/finally**（hook 里抛异常也会漏 pop）。

### 最易踩的坑：把「已移出战斗」的卡交给 `CardCmd.AutoPlay`

1. 先**快照**一份候选卡列表，再逐张 `AutoPlay`（本项目《轮回》管线就是这个写法）。
2. 第 1 张结算时触发了「把战斗内某些卡全部移出战斗」的效果（先例：`SamePlaceTrace.RemoveAllSamePlaceTrace` 把所有同一处痕迹移出，连快照里还没轮到的那张也移出）。
3. 循环继续把已被移出的卡交给 `AutoPlay` ⇒ 其内部 `CardPileCmd.Add(card, PileType.Play)` 被 `Add` 开头的守卫 `card3.HasBeenRemovedFromState || creature.IsDead || (card3.IsInCombat && creature.CombatState == null)` 拦下，**静默返回 `success = false`**（不抛异常、不打日志）⇒ 该卡 `Pile` 仍非战斗牌堆 ⇒ `CardModel.CombatState == null`。
4. `OnPlayWrapper` 命中 `if (combatState == null) return;` ⇒ 模型永久残留在栈上。

**规则**：快照式循环里，**每一次**交给 `CardCmd.AutoPlay` 之前都要复核卡牌仍在战斗牌堆内。统一判据（先例 `TransmigrationRules.CanStillAutoPlay`）：

```csharp
internal static bool CanStillAutoPlay(CardModel card)
{
    if (CombatManager.Instance.IsOverOrEnding) return false;
    if (card.HasBeenRemovedFromState) return false;
    if (card.Pile is not { IsCombatPile: true }) return false;
    var owner = card.Owner;
    return owner != null && !owner.Creature.IsDead;
}
```

复核与 `AutoPlay` 之间**不能有 `await`**（否则中间可能又被移出）。同款入口：`TransmigrationRules.TriggerTransmigrationEffect`、`Transmigration3Rules.TriggerTransmigration3Effect`、`HextechTransmigrationEcho.PlayRandomTransmigrationCards`。

### 另一面：`Pile == null` 的「新生成牌」不能先结算资源

`combatState.CreateCard(template, owner)` 造出来的牌**不属于任何牌堆** ⇒ `CardModel.CombatState == null`（该属性直接由 `Pile.IsCombatPile` 推出）。此时：

- `card.SpendResources()` → `SpendEnergy` → `Hook.AfterEnergySpent(CombatState /* null */, …)` → `Hook.IterateCombatHookListeners` 对 null 解引用 ⇒ **`NullReferenceException`**（外层只看到 `GameAction … completed with exception (Object reference not set…)`，整张牌的效果中断）；
- 相反地，`IsInCombat => Pile?.IsCombatPile ?? false` 此刻是 **false**，所以 `CardPileCmd.Add(card, PileType.Play)` **不会被守卫拦下**，能正常把它补进 Play 堆。

**规则**：自己实现「自动打出」时，把原版 `CardCmd.AutoPlay` 里那句 `if (card.Pile == null) await CardPileCmd.Add(card, PileType.Play);` **提到任何资源结算之前**，然后复核 `card.CombatState != null` 再继续（Add 仍可能因 `HasBeenRemovedFromState`/`IsDead` 静默失败）。

先例 bug（2026-10-06）：`YalisalinFireComponentResolver.PlayCardWithSuppressedFireComponent` 把 Add 写在 `SpendResources()` **之后** ⇒ 只要「高塔」用 `YalisalinTowerPower.ModifyFireComponentChoiceOptions` 生成的其他角色牌被选为连接牌，打出带【余火】的牌时必崩（godot.log 里 4 组 NRE，栈顶 `IterateCombatHookListeners`）。

## Safety Rules

- Use command APIs for gameplay effects; avoid direct mutation when a command exists.
- Check owner/side/target guards explicitly.
- If adding events or delegate fields to an `AbstractModel`, inspect `AfterCloned`; shallow-copied delegates must not leak to clones.
- Validate with `dotnet publish ManosabaLin.csproj -c Release`（在仓库根目录执行；游戏运行时会锁 DLL ⇒ 先关游戏）。
