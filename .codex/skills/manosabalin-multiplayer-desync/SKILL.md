---
name: manosabalin-multiplayer-desync
description: Diagnose and fix Slay the Spire 2 multiplayer StateDivergence (desync / "主机把队友踢下线") involving ManosabaLin. Use when godot.log shows "State divergence detected", "Disconnecting peer ... reason: StateDivergence", "Connection ... Timeout", or when the player reports 联机卡顿/掉线/不同步, or when writing multiplayer-safe card/power/relic effects (random picks, cost changes, counters).
---

# ManosabaLin 多人状态分歧（StateDivergence）排查

## 0. 先分清两类「掉线」

| 症状 | 日志特征 | 性质 |
| --- | --- | --- |
| **Timeout 掉线** | `Steam connection closed because of problem. Reason: 4001, Timeout` + 掉线前 `Ping` 暴涨 / `Packet Loss` > 0 | **本机主线程卡死**（特效/长时间 await）导致收不到心跳。与 `StateDivergence` 无关。先修性能。 |
| **被踢（StateDivergence）** | `Disconnecting peer <id>, reason: StateDivergence` + `[RunLobby] Player <id> disconnected ... Reason: StateDivergence` | 两端**游戏状态**不一致，主机主动断开。 |

⚠️ 两者会连成一条链：**卡顿 → Timeout 掉线 → 队友重连 → 未同步状态丢失 → 43 分钟后 StateDivergence 被踢**。修完性能别以为完事了，重连后的分歧要单独查。

## 1. 取证：RitsuLib 分歧诊断包（决定性证据）

```
%APPDATA%\SlayTheSpire2\logs\ritsulib_state_divergence_<时间>_checksum_<N>_<hash>.zip
```
解压得 4 个文件：`state-divergence-report.txt`（最有价值）、`local/remote-debug-log.records.json`（双方各 5000 条日志）、`metadata.json`。

**只读报告就够了，但有两个坑：**

1. **报告头部的「N differing field(s)」会骗人**：`savedProperties.netIdMap` 段落会打印
   `325 SavedProperty net-id slot(s) differ.` 并罗列全部槽位，即使两侧**完全相同**（实测：把 local/remote 两份槽位表 diff 出来是 0 行差异，mapHash 也相同）。
   ⇒ **别信计数，自己 diff**：定位 `LOCAL STATE DUMP` / `REMOTE STATE DUMP` 两段，用 Python `difflib.unified_diff` 直接比。这里才是真差异。
2. 网络槽位差异里 `checksum.value` / `network.remotePeer` 必然不同（本地 vs 远端视角），不是状态差异。

一行脚本直接抓真差异（`state-divergence-report.txt` 的 `LOCAL/REMOTE STATE DUMP` 段）：

```python
import io, difflib
L = io.open('state-divergence-report.txt', encoding='utf-8').readlines()
li = [k for k, t in enumerate(L) if t.startswith('LOCAL STATE DUMP')][0]
ri = [k for k, t in enumerate(L) if t.startswith('REMOTE STATE DUMP')][0]
print(''.join(difflib.unified_diff(L[li+1:ri], L[ri+1:], 'local', 'remote', n=3)))
```

实测输出（2026-10-15，`checksum 289`，主机 vs 队友）：

```
 		Serialized card: SerializableCard CARD.MANOSABA_LIN_CARD_ANANLIN_DEFEND. Upgrades: 0
 			Enchantment: SerializableEnchantment ENCHANTMENT.MANOSABA_LIN_ENCHANTMENT_AGREEMENT with amount 1 ...
-			Energy Cost: 0            ← 主机有；队友没有（队友用原费）
```

## 2. 传输模型：什么能同步、什么不能（决定修法）

| 对象 | 传输载体 | 传输内容 |
| --- | --- | --- |
| **卡牌** | `CardState`（`NetFullCombatState`/`NetDeckCard`…） | `SerializableCard`（id/upgrade/**enchantment**/**props**/floor）+ **`energyCost`**（= `EnergyCost.GetWithModifiers(All)`，仅当 ≠ `Canonical` 时带）+ affliction + keywords |
| **遗物** | `RelicState` → `SerializableRelic` | id/floor/**props（SavedProperty）** ⇒ ✅ 遗物的 `[SavedProperty]` 会被传 |
| **能力** | `PowerState` | **只有 id + amount**❗props 不传 ⇒ 能力的 `[SavedProperty]` **对活体联机同步无用**（只对存档有用） |
| **普通过程内字段** | —— | 完全不传（重连即丢） |

- `[SavedProperty]` 允许的类型（引擎 `SavedProperties.FromInternal` 白名单）：`int` / `int[]` / `Enum` / `Enum[]` / `ModelId` / `bool` / `string` / `SerializableCard` / `List<SerializableCard>`。
  ⇒ **`CardModel` 引用不能当 `[SavedProperty]`**（要存 `ModelId` 或 `SerializableCard`）。
- 写法定式（见 `EmaTrialBadge`）：
  `[SavedProperty] public int SavedX { get => _x; set { AssertMutable(); _x = value; } }`
- ⭐ **战斗中的卡是牌组卡的克隆**（本项目 `TransmigrationPairCapability` 注释已记录）：战斗中改的费用/能力**不会写回牌组卡**；反之牌组卡的状态会被克隆进战斗卡 ⇒ **打在牌组卡上的费用修饰符会跟着每次战斗的克隆一直带着走**。

## 3. 卡牌费用是校验和的一部分 ⇒ 费用不一致 = 踢人

`CardState.From`：
```csharp
int withModifiers = card.EnergyCost.GetWithModifiers(CostModifiers.All);   // Local + Global
if (withModifiers != card.EnergyCost.Canonical) result.energyCost = withModifiers;
```
`CostModifiers.All` 含 **Local（卡自身的 `_localModifiers`）+ Global（`Hook.ModifyEnergyCostInCombat`）**。

⇒ **任何「改牌费用」的效果，只要两个对端选中的牌不同、或改/不改不同，就直接触发 StateDivergence。**
而 `GetWithModifiers` 的 Global 部分只由 hooks 现算（两端同源），所以**分歧几乎总来自「改费用的那段代码选错了牌 / 只在一端跑了」**。

## 3.5 ⚠️★ 主牌组（`PileType.Deck`）**不进战斗校验和** ⇒ 常驻改费的分歧要等到下一场战斗才爆

**事实**（2026-10-20 实测，`checksum 289/434` 两份包）：
- `LOCAL/REMOTE STATE DUMP` 里每个玩家只有 **Hand / Draw / Discard / Exhaust / Play** 五段，**没有 `Pile Deck`**。
- 开战时牌组里的常驻卡（master）会被 **克隆**进抽牌堆（`PileType` 注释原话："When a new combat starts, all cards in here are cloned into your draw pile"）。
- ⇒ 任何**写在 master 上**的改动（典型：`card.EnergyCost.UpgradeBy(-1)` 这种永久降费）**在本次战斗里完全不可见**，只能等下一场战斗开打、克隆出来才第一次被校验到。

**因此这类分歧有固定形态，一眼可认：**
1. 分歧点恒为**新战斗的第一个 `After player turn start`**（两边最后一个匹配的校验和是 `Exiting xxx room`）；
2. 差异行只有 `Energy Cost:` 且**永远在对端（非作弊方）多一行/值更小**；
3. RNG 全部状态、遗物/能力/附魔/组件**完全一致**（别再查这些，白费工）；
4. 两边差异卡的数量 == 那个"一次改 N 张"效果的 N（如 `Witchfactorechantcard` 的 `stacks = With/50`）；
5. 差异卡面恰好带那个效果才会上的**审判附魔**（且这些卡往往**没有 mod 关键词** ⇒ 不可能是 `EmaTrialBadge.EnchantAllTrialCards` 给的）。

**排查顺序**（别倒着来）：
1. 先 `diff` 两份 `*_dump.txt`，确认差异只有费用行 → 判为 master 常驻改动（本章）。
2. 在 `ManosabaLinCode` 里 grep `PileType.Deck`（= 操作 master 的全部入口）与 `UpgradeBy(` / `SetThisCombat(` 的交集，收敛到唯一负责的文件。
3. 再看该效果的**选牌是否依赖不可校验的量**：`PileType.Deck.GetPile(player).Cards` 的**列表顺序**、执行时点与战斗结束（`IsEnding`）的相对位置。两者都不在校验和里 ⇒ 天然危险。

**已识别的高危唯一入口**：`Characters/Ema/Cards/Witchfactorechantcard.cs`——从 `PileType.Deck` 挑 `unenchanted` 后 `OrderBy(_ => rng.NextFloat()).Take(stacks)`，再 `CardCmd.Enchant(...)` + `EnergyCost.UpgradeBy(-1)`。

### 🔴🔴 已 100% 锁定的部分 + 尚未锁定的最后一环（2026-10-20）

**具体位置：`Characters/Ema/Cards/Witchfactorechantcard.cs`**
```csharp
var deckPile = PileType.Deck.GetPile(owner);        // 第 63 行：取主牌组（房间之间的卡）
...
foreach (var card in toEnchant)
{
    var enchant = rng.NextItem(TrialEnchants);
    CardCmd.Enchant(enchant.ToMutable(), card, 1m);
    card.EnergyCost.UpgradeBy(-1);                  // 第 91 行：★ 唯一的永久降费
}
```
它是艾玛体系里**唯一**会永久改主牌组卡费用的地方（其余 `Emachouma`/`Hirochouma`/`Xuelichouma` 的 `setCostToZero` 出自筹码遗物，本 run 没持有；`Emadeath`/`Lamort`/`BondSettlement`/`SmallKey` 改的都是战斗内的克隆卡，战斗一结束就随卡消失）。
- 引擎确实支持在 `PileType.Deck` 上附魔：`CardCmd.Enchant` 末尾专门 `if (pile != null && pile.Type == PileType.Deck)` 记历史；`CardCmd.Downgrade` 同理。⇒ 在 Deck 上动手脚是正规操作，不是野路子。

**100% 证实的事**：差异就是这行产生的 —— 差异卡必带它才会上的审判附魔、张数 == 它的 `stacks`(With/50)、且**只出现在卡主自己那台机器上**。

**尚未锁定的最后一环：为什么只有一端生效。** 已排除（都查过，别再重复）：
- ❌ 序列化往返抹掉（见下面 A）—— **`FromSerializable` 只在**载入 run** 时被调用**（全量反编译只有 4 个调用点：两个 `StartRun` + `LoadRun` 链），房间切换不调用；`Clone(newCard)` **复制 `_base`**，克隆保留改动；#434 分歧窗口内两端日志**没有任何 save/load 记录**。
- ❌ 选牌顺序 / 挑的卡不同：两端 RNG `CombatCardSelection` 计数器与四字状态**一字不差**，而旧代码 RNG 消耗 = `|unenchanted|` 次 `NextFloat` + `stacks` 次 `NextItem` ⇒ 候选集大小与 `stacks` 都相同。
- ❌ 卡牌可变性：`EnergyCost.UpgradeBy` 首行 `_card.AssertMutable()`（`!IsMutable` 抛 `CanonicalModelException`），但主牌组的卡是 mutable 的，且两端日志在 `WITCHFACTORECHANTCARD` 执行前后**没有任何异常**。
- ❌ 被降级抹掉：`EnergyCost.ResetForDowngrade()` 会把 `_base` 重置回 `Canonical`，但引擎里 `CardCmd.Downgrade` 只有 4 个调用点且本项目**无人调用**。
- ❌ 全局钩子（`Hook.ModifyEnergyCostInCombat`）：两端都没有改费的能力/遗物。

**⇒ 下一次取证靠插桩**（已加）：`-1` 前后各取一次 `EnergyCost.GetWithModifiers(CostModifiers.All)`，逐卡打 `before/after`。
谁那侧出现 `before == after`（没变小），就是那一侧写入失败，直接定位，不用再猜。

#### A. `SerializableCard` 不存费用 —— 是真事实，但**不是** #289/#434 的触发点
（保留是因为它本身是个大坑，且同样会让永久降费在**存档读档**后消失）

**`SerializableCard` 根本没有费用字段，`CardModel.FromSerializable` 也从不恢复它。**

```csharp
// MegaCrit.Sts2.Core.Saves.Runs.SerializableCard  —— 全字段只有 5 个：
// Id / CurrentUpgradeLevel / Enchantment / Props / FloorAddedToDeck      ← 没有 energyCost

// MegaCrit.Sts2.Core.Models.CardModel.FromSerializable
CardModel cardModel = SaveUtil.CardOrDeprecated(save.Id).ToMutable();   // ← 从原型重建：_base = 原型 canonical
save.Props?.Fill(cardModel);
cardModel.AfterDeserialized();
if (save.Enchantment != null) {
    cardModel.EnchantInternal(...);
    cardModel.Enchantment.ModifyCard();     // ★ 引擎会为「本体改动型附魔」重跑 ModifyCard
    cardModel.FinalizeUpgradeInternal();
}
for (int i = 0; i < save.CurrentUpgradeLevel; i++) {   // ← 只按升级层数重放 UpgradeInternal()
    cardModel.UpgradeInternal(); cardModel.FinalizeUpgradeInternal();
}
```

⇒ **任何不是通过「升级」写进 `EnergyCost._base` 的改动（`EnergyCost.UpgradeBy(...)` 直改），在卡片被
「序列化 → 反序列化重建」时会被**静默抹掉**。**

⚠️ 但**这条不是 #289/#434 的触发点**（2026-10-20 自查推翻，见上）：`FromSerializable` 只在载入 run 时调用，
房间切换不调用；`Clone` 保留 `_base`；分歧窗口内没有 save/load。⇒ 它解释不了「中途房间切换后卡没了 -1」。

⚠️ 它在**单人模式**下仍是真 bug：存档退出再读档，永久降费一样会消失。别当成联机特有问题。

**记账方法（判「挑的卡不同」还是「数量不对称」，必做）**：取每卡的**真实基础费用**（构造函数 `base(N, ...)`）
与 **`OnUpgrade` 里真正的费用 Delta**，算出「本应打印的 Energy Cost」，再和实际打印值比。
⚠️ 直接 grep `EnergyCost.UpgradeBy(` 会被 `OnPlay` 里改**别人**费用的那行污染（例如
`Witchfactorechantcard.cs` 的 `card.EnergyCost.UpgradeBy(-1)`），必须只看 `OnUpgrade` 块。

**正解（引擎认可的唯一做法）**：把「卡本体改动」放进**附魔的 `OnEnchant()`**，由
`EnchantmentModel.ModifyCard()` 承载 —— 引擎在每次反序列化后**自动重跑**它（见上面的 `FromSerializable`）。
原版就是这么做的：`TezcatarasEmber` / `MockFreeEnchantment` 在 `OnEnchant()` 里
`EnergyCost.UpgradeBy(-当前费用)`（降到 0，天然幂等）；`Goopy` 等在里面 `AddKeyword`。

⚠️ 但直接搬 `-1` 有两个坑，动手前必须算清：
1. **两条路径要算出同一个值**。`FromSerializable` 里 `ModifyCard()` 在**升级重放之前**跑。
   设 prototype=C、升级费用 Delta=U：活体路径 `C+U → -1 = C+U-1`；重建路径 `C → -1 = C-1 → +U = C+U-1`。
   **用增量（`-1`）形式两条路径一致；用绝对值形式反而不一致**（重建时还不知道 U）。
2. **幂等性**。增量形式不幂等，`ModifyCard()` 多跑一次就多减 1。要么学原版用
   `UpgradeBy(-当前费用)` 的绝对形式（但那样就不是「-1」而是「降到 0」），要么另存标记。

⇒ 这属于**机制语义变更**，先问用户再动。

**降低风险（不改动语义，2026-10-20 已做）**：
```csharp
// 先把「牌组列表顺序」这个不可校验的自由度消掉：稳定排序后再随机挑
var ordered = unenchanted
    .OrderBy(static c => c.Id.Entry, StringComparer.Ordinal)
    .ThenBy(static c => c.CurrentUpgradeLevel)
    .ToList();
var toEnchant = ordered.OrderBy(_ => rng.NextFloat()).Take(stacks).ToList();
...
Log.Info($"[<效果名>] ... targetCount={ordered.Count} enchanted=[{string.Join(", ", toEnchant.Select(static c => c.Id.Entry))}]");
```
`IN/OUT` 两行取证日志也很关键：下一份分歧包里 diff 两端 `local/remote-debug-log.records.json` 的这几行，能直接判定
`with`/`stacks`（数量是否对称）与 `picks`（挑的是不是同一批卡），不用再猜。

> ⚠️ **2026-10-20 实名：上面这条「稳定排序」定式只治标**，它消掉的「列表顺序」自由度**不是**本次分歧的原因
> （候选集大小与 `stacks` 已由 RNG 计数器证为相同）。
> 真正能一次命中「数量不对称」的是逐卡打点（见上）：`before/after` 谁没变小，就是那一侧写入失败。
> 若证到是「写入失败」，正解是把卡本体改动挪进附魔的 `OnEnchant()`（由 `ModifyCard()` 承载，
> 反序列化后引擎自动重跑）——属**语义变更，须先问用户**。

## 4. 已确认的「导致分歧」代码反模式

### ⚠️ 反模式 1：用 `CanPlay()` 给「改费用 / 免费」类效果筛候选（本项目真实事故）

`CanPlay()` 把 **`EnergyCostTooHigh` / `StarCostTooHigh`** 一起算进来，而能量是随行动推进**瞬时变化**的 ⇒ 同一 `OnPlay` 在两端可能得到**不同的候选集**；再叠 `Rng...NextItem()` 抽牌，就会一端把 A 设为 0 费、另一端设 B（或谁都没设）。

事故（`SmallKey`「小钥匙」）：卡面（本地化=规范）写的是
> 抽{Cards}张卡牌。手牌中**第一张**带【审判】附魔的牌**费用-1**

代码却是 `FirstOrDefault(c => ... && c.CanPlay())`，既**违背卡面**又是分歧源。

**修法（正解口径，参照 `YalisalinFireComponentResolver`）**：
```csharp
// 只屏蔽资源类原因，保留两端必然一致的否决（Unplayable 关键词 / 目标非法 / Hook 拦截）
if (card.Keywords.Contains(CardKeyword.Unplayable)) return false;
if (card.Owner?.PlayerCombatState == null) return false;
if (card.CanPlay(out var reason, out _)) return true;
var nonResource = reason & ~UnplayableReason.EnergyCostTooHigh & ~UnplayableReason.StarCostTooHigh;
return nonResource == UnplayableReason.None;
```
现成实现：`ManosabaLin.Characters.Emalin.EmalinCombatHelper.IsEligibleForCostEffect(CardModel?)`。
已接入：`SmallKey`（直接删掉 CanPlay）、`Agreement` ×5、`EnchantmentConvergencePower` ×5、`JointJudgmentPower` ×5。

⚠️ **别用它去改「按设计就该现在打得起的牌」**：`Dismantledparts`（拆解零件）那种「循环打出能付得起的攻击牌」的地方，`CanPlay()` 是语义本身，保留。
⚠️ 拿 `CanPlay()` 做**纯视觉**（发光色、卡面样式）是安全的 —— 例如 `MainFile.cs` 的 glow、`BondCardVisualRules`。

### ⚠️ 反模式 2：决定玩法结果的计数用普通私有字段（重连即丢）

`EmaTrialBadge._agreeCount/_doubtCount/_rebuttalCount` 曾经是普通字段，而它们
①决定 ×2/×3/×4/×5 阈值（×5 就是「随机 1 张手牌免费」，直接改费用）；
②会被 `SyncCountersToEnchantments()` 写进 `Enchantment.Amount`（**同步字段**）⇒ 一端不一致就刻进同步状态。
遗物 props 会随 `RelicState` 传输 ⇒ 改成 `[SavedProperty]` 后重连能对齐。**已修**（`SavedAgreeCount/SavedDoubtCount/SavedRebuttalCount`）。

判据：**凡是「决定阈值/分支/选牌」的数，必须 `[SavedProperty]`**（卡与遗物上）；能力上的只能靠两端重放，别依赖它跨重连。

### ⚠️ 反模式 3：`AutoPlay` 的语义（不必额外防）

`CardCmd.AutoPlay(choiceContext, card, null)`：`target == null` 时用 `card.Owner.RunState.Rng.CombatTargets` 抽目标（✅ 确定性 RNG，可用）；**不花费用**（`EnergySpent = 0`，不调 `SpendResources`）⇒ 天然免费，不需要 `SetToFreeThisTurn`。

## 5. 标准排查流程

1. `grep -n "State divergence\|Disconnecting peer\|Reason: Timeout" godot.log` → 定时间线（谁先、隔多久）。
2. 有诊断包就按 §1 diff `LOCAL/REMOTE STATE DUMP`，拿到**唯一真差异**（通常是某张卡的 `Energy Cost` / `Enchantment amount` / props）。
3. 差异是费用 ⇒ 按 §3 找「谁改的这张牌的费用」：`grep -rn "SetToFreeThisTurn\|SetFreeIgnoringCardPlayConditions\|SetThisCombat\|AddUntilPlayed\|UpgradeBy" ManosabaLinCode/`，逐个看候选集是否依赖瞬时量（`CanPlay()`、能量、`Owner.Relics` 里的本地态）。
4. 差异是计数/阈值 ⇒ 查它是不是普通字段。
5. 改完 `dotnet build ManosabaLin.csproj -c Debug`（root 目录）确认 0 error；发布走 `dotnet publish ManosabaLin.csproj -c Release`（见 `manosabalin-*` 发布铁律，改过 `.gd`/本地化要重导 PCK）。

## 6. 顺带记住的两个环境事实

- ⚠️ **验证 DLL 里到底有没有你的新字符串**：.NET 的**用户字符串字面量是 UTF-16LE**（存在 `#US` 堆），
  而**类型/成员名是 UTF-8**（`#Strings` 堆）。⇒ `grep -a "日志文案" DLL` 永远 0 命中，会误判成「没进包」。
  正确做法（Python）：
  ```python
  data = open(dll, 'rb').read()
  data.count('你的日志文案'.encode('utf-16-le'))   # 用户字符串
  data.count('YourMethodName'.encode('utf-8'))    # 成员名
  ```
- ⚠️ **验证 PCK 里是哪一版 `.gd`**：PCK 里的脚本是编译过的 `.gdc`，文本 grep 搜不到常量名。
  用 Godot 自己内省（模板：`.local/godot_probe/verify_pck_scripts.gd`，支持 `PCK_PATH` 环境变量指定包）：
  ```gdscript
  ProjectSettings.load_resource_pack(pck, false)
  var s: GDScript = load("res://ManosabaLin/scenes/.../xxx.gd")
  print(s.get_script_constant_map())   # 断言新常量在不在
  ```
  （`mods/ManosabaLin/` 那份是死重，游戏只读**创意工坊**那份 ⇒ 两个包都要验。）
- 反编译引擎源码目录（按命名空间分文件，检索比 dll 快）：`D:\ManosabaLin\.local\probe_engine\decomp\<Namespace>\<Type>.cs`
  （`.local/` 被 gitignore ⇒ Grep/Glob 必须显式传 `path=`）
- 日志：`C:\Users\Lenovo\Desktop\godot.log`（用户拷出来的会话日志）、
  `%APPDATA%\SlayTheSpire2\logs\`（游戏自己的 godot.log + 分歧包）。
