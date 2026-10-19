# 亚里沙金卡「自罚上瘾」——效果规格（已实现）

> 卡名：**自罚上瘾**（用户 10-02 定稿）。参照物：`JusticeEnforcer` 正义的执行者 / `DestroyEverything` 破坏侦探 /
> `AnanlinWeavingLiesSleepingPrincess` 编织谎言的沉睡公主 / `FinalJudgment` 受厌恶之人。
> 10-03 全部口径已裁定，**代码已写、编译 0 CS 错误**；等游戏关闭后 `dotnet publish` 部署。

---

## 一、四张参考卡的骨架（六条硬约束）

`CardRarity.Rare` ／ **升级只降 1 费** ／ 自带一个专属组件 ／ 幕式结算（每幕读**一条独立轴**）／
至少一幕带阈值 + **末尾用魔女化收口** ／ 动词不与该角色既有卡撞车。

| | 执行者 | 破坏侦探 | 沉睡公主 | 受厌恶之人 |
|---|---|---|---|---|
| 费/型 | 3 Power 自身 | 3 Attack 敌 | 4 Skill 自身 | 3 Skill 自身 |
| 幕 | 5 | 4 | 4 | 4 |
| 阈值 | 魔女化≥100→引擎 | 等待≥13→翻案 | 安心≥3→改写意图 | 魔女化/50·/100 |
| 升级 | 只降费 | 只降费 | 只降费 | 只降费 |

---

## 二、四条轴的真实库存 API（实读）

| 轴 | API |
|---|---|
| **原罪诅咒** | `card.HasComponent<Originalsin>()`；`Originalsin.Forgive/Punish` + `ForgiveTriggered/PunishTriggered` 静态事件 |
| **火色** | `hairpin.GiveFireColor(ctx,target,n)` / `ConsumeAllFireColor(...)`；`MaxSegments = 6`，格位定色 **1-2 浅橙 / 3-4 亮黄 / 5-6 赤红** |
| **余火** | `YalisalinFireComponentRules.HasFireComponent(card)`＝`TryGetCapability<YalisalinFireComponentCapability>`；默认挂载 7 张 |
| **魔女化** | `WithPower.Amount` |
| **点火** | `IgnitePower`（`Ignite.cs`，2 费能力·Ancient，魔女化 100 时获得）：层数 N → 随机攻击 N 次；命中**敌** → 层数一半（≥1）伤害 + 全体友方 1 格挡 + 无易伤则 1 易伤；命中**友** → 1 伤害且本能力 +2 层；**回合结束时失去一半层数**（向下取整，1 层时损失 0）。⚠️ 衰减只挂 `AfterSideTurnEnd`，`TriggerIgnite`（`internal`，紫藤亚里沙/自罚上瘾手动调用）只打不衰 |
| **火色消耗奖励** | 浅橙→`OrangeConsumeBlock` 格挡；亮黄→下回合 1 能量 + 1 费减免；赤红→`RedConsumeDamage` 伤害 + 给敌【亚里沙的魔法】 |

### ⭐ 已查实的关键事实

1. **「给一张卡挂上余火」已有现成 API**：`YalisalinFireComponentRules.TryAddFireComponent(card)` = `card.GetOrCreateCapability<YalisalinFireComponentCapability>()`
   （RitsuLib `ModelCapabilities.GetOrCreate`，**运行时对单个卡实例生效**，与静态的 `[RegisterDefaultModelCapability]` 是两回事）。
   同文件还有 `RandomCardWithoutFireComponent(owner, filter)`、`AllCombatCards(player)`（手/抽/弃），已内置排除锁定卡与已带余火的卡 ⇒ **零基建改动**。
2. **`combatState.Allies` 包含玩家自己**（项目内别处都写 `Allies.Where(c => c != Owner.Creature)` 来取"其它队友"反证）⇒
   **「点火命中友方」在单机 = 「点火命中自己」**；多人时扩大到任一友方。
3. **「点火命中友方」目前无钩子可辨识**：`IgnitePower` 命中友方走 `else` 分支，调用 `CreatureCmd.Damage(ctx, target, 1m, Unpowered, Owner, null, null)`
   ⇒ dealer 是自己、cardSource 是 null，引擎 `Hook.AfterDamageReceived` **分不出这是点火打的**。⇒ 必须在 `IgnitePower` 里开一个事件点（见 §四①）。

---

## 三、定稿效果（五幕）

> **自罚上瘾** · 3 费 · 技能 · 金 · 自身（升级 3 → 2 费）
> 消耗你所有牌中的全部【原罪诅咒】。每消耗 1 张：随机对 1 名敌人造成 **6** 点伤害。
> 随机给予 **6** 格【火色】，然后消耗这 **6** 格。
> 消耗你所有牌中全部【余火】卡。每消耗 1 张：触发 **1** 次【点火】的攻击。
> **消耗你全部的【魔女化】**：每 **50** 层获得 **1** 点能量；若消耗达 **100** 层，获得【原罪】。
> 【原罪】：每当你的【点火】攻击到友方 1 次，随机使你牌组中的 1 张牌获得【余火】。

| 幕 | 落地 |
|---|---|
| 一 原罪 | 扫 手/抽/弃 → `HasComponent<Originalsin>()` → 逐张消耗；每张 `CreatureCmd.Damage(rng 敌人, 6, Unpowered)` |
| 二 火色 | `GiveFireColor(随机敌人, 6)`（填满 2 浅橙+2 亮黄+2 赤红）→ `ConsumeAllFireColor(该敌人)`，逐格结算奖励 |
| 三 余火 | 扫 手/抽/弃 → `HasFireComponent(card)` → 逐张消耗；每张触发 1 次点火攻击 |
| 四 魔女化 | **一次清空** `WithPower`：`能量 = 当前值 / 50`（向下取整）；`当前值 ≥ 100` → 挂新「原罪」能力 |
| 五 原罪（**新**，显示名「**原罪**」） | 订阅「点火命中友方」→ `TryAddFireComponent(RandomCardWithoutFireComponent(owner))` |

**幕五与幕三是一条闭环**：原罪把余火**发**给牌 → 之后打「自罚上瘾」时幕三能**烧**更多张 → 每张各触发一次点火 → 点火命中友方 → 原罪再发一张。
这就是卡名「上瘾」的自我加速结构，也补上参考卡「末尾给永久引擎」那一格。

---

## 四、用户裁定（10-03）

- **① 触发条件改为「点火命中友方」**（不再只判"自己"）。落地用**方案 (a)**：在 `IgnitePower` 的 `else`（友方）分支里加静态事件
  （照项目已有的 `Originalsin.PunishTriggered` 写法）—— 改动 ≈2 行，是唯一可靠办法。
  ⚠️ 副作用：命中友方比命中自己**宽**（多人时任一友方都算），而每次触发都发 1 张余火、**无上限** ⇒ 长局会把牌堆铺满余火。**（要限次数就说）**
- **② 「余火卡」= 自带余火的 capability**（`HasFireComponent`）—— 与幕五发的池子同一个，闭环成立。
- **④ 魔女化「消耗」= 一次清空**：`用掉 = 当前值`，`能量 = floor(当前值 / 50)`，`当前值 ≥ 100` 给原罪；**余数一并消失**。
- **⑤ 新能力的显示名 = 「原罪」**，本地化键 `MANOSABA_LIN_POWER_YALISALIN_ORIGINALSIN_POWER`（与 Hiro 的 `ORIGINALSINJUSTICE_POWER` 是两个模型，显示名相同不影响）。

### ③ 已裁定：没有【点火】层数时第三幕**空转**（用户 10-03「接受空转」）

### 我代定的默认（不同意就说）
消耗 = **进消耗堆**（`CardCmd.Exhaust`）；消耗诅咒**不触发**宽恕/自惩；六格火色给**随机 1 名敌人**、也消耗**该敌人**的；
幕五的「牌组中的牌」= **手 / 抽 / 弃**（与幕一/幕三同池）、**排除已带余火的**；3 费 · 技能 · 金 · 自身，**升级只降费 3→2**。

---

## 五、实现落点（已完成）

1. `Characters/Yalisalin/Cards/ZiFaShangYin.cs`（新卡）—— 五幕全在 `OnPlay` 里。
2. `Characters/Yalisalin/Powers/YalisalinOriginalsinPower.cs`（**新「原罪」引擎**，`PowerStackType.Single`）。
3. `Characters/Yalisalin/Powers/IgnitePower.cs` —— 加 `public static event Action<PlayerChoiceContext, Player>? AllyHit`
   （友方分支里 `Invoke`），并把 `TriggerIgnite` 由 `private` 放宽为 `internal`（卡牌手动「触发一次点火攻击」复用同一份结算）。
4. 本地化五语言（zhs 源）：`MANOSABA_LIN_CARD_ZI_FA_SHANG_YIN.{title,description}` + `MANOSABA_LIN_POWER_YALISALIN_ORIGINALSIN_POWER.{title,description}`。
5. **未动** `Originalsin.cs`、**未动** Hairpin、**未动**火色/余火基建。
