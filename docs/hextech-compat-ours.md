# 与海克斯符文联动 · 本项目施工单

> **配对文件**：`docs/hextech-compat-hextech.md`（给海克斯作者的需求，可直接当 Issue 发）。
> **另一份施工单**：`docs/hextech-universal-runes-design.md`（**通用符文**：一角色一个，真·通用 / 纯原版机制；**只出设计、未落地**）。
> **通用参考**：`docs/hextech-compat-design.md`（跨 mod 联动的五层套路 L0~L4 + `[ModInterop]` / `[AssemblyInterop]` 硬规则）。
> **权威依据**：**海克斯作者发布的《外部模组对接指南》**（软依赖 `HextechRunesInterop` / 硬依赖 `HextechRunesApi`）。
> 本文档凡是与它冲突的，一律以它为准。
> **对方基准**：HextechRunes `0.9.6` 之后（本机 `lib/0.111.0/HextechRunes.dll`，全量反编译 `.local/hex_src/`）。
>
> **路线（已按官方指南改定）**：**软依赖 `HextechRunesInterop`；零 Harmony patch；不硬引用 dll；不建子程序集。**
> 我方符文 = 我们自己的 `RelicModel` 子类，图标/文案/稀有度全在 ManosabaLin 自己的 PCK 里；
> 海克斯只负责**识别 + 载入**（品级分层、图鉴分组、配置菜单、抽取池）。

---

## 0. 官方对接指南的硬性约束（红线，改代码前先读这节）

| # | 约束 | 依据 |
| --- | --- | --- |
| 1 | **`HextechCatalog` 等 `internal` 类不属于对外契约，随时可能重构，禁止对它们打 Harmony 补丁。**缺能力请开 issue | 指南「软依赖接入」开头 |
| 2 | manifest 里**不要**写 `dependencies: ["HextechRunes"]`（否则没装的玩家加载不了我们的 mod） | 指南「注册时机」 |
| 3 | 按**程序集名** `HextechRunes` 检测，**不要按 manifest id**（本体按游戏版本加载不同变体 DLL；二创版也可能用同一程序集名） | 指南「注册时机」 |
| 4 | 模组之间初始化顺序不固定 ⇒ **先查一次已加载程序集，没找到就订阅 `AppDomain.CurrentDomain.AssemblyLoad`**，载入后立即注册 | 指南「注册时机」 |
| 5 | 注册必须在**模组初始化阶段**完成：模型池要在**共享遗物池首次枚举前**登记；带 `[SavedProperty]` 的符文还要赶在**存档序列化缓存初始化之前**。窗口关闭后再调用抛 `InvalidOperationException`，且**不留半登记状态** | 指南「注册时机」 |
| 6 | 符文必须是**具体、非泛型**的 `RelicModel` 子类；`Rarity` **必须** `RelicRarity.Starter` | 指南「符文类的要求」 |
| 7 | 图标**由我们自己提供**（`PackedIconPath` / `PackedIconOutlinePath` / `BigIconPath`），不依赖海克斯的资源路径 | 指南「符文类的要求」 |
| 8 | `HextechRelicBase` 提供的扩展（阵营回合钩子、`*Compat` 伤害修正、生成卡牌辅助）**不对外部符文开放** ⇒ 外部符文只用**原版 `RelicModel` 的钩子** | 指南「符文类的要求」 |
| 9 | `isAvailableForPlayer` 委托**联机两端都会执行，必须得出相同结果**；只能读同步状态（`Player` 的角色/遗物/牌组/`RunState`），**不要读本地设置、UI 或随机数** | 指南「isAvailableForPlayer」 |
| 10 | 参数无效抛 `ArgumentException`（反射调用时包在 `TargetInvocationException` 里）；**所有校验都在产生任何副作用之前完成** | 指南「RegisterPlayerRune」参数表 |
| 11 | 同一个类型重复注册 ⇒ **以第一次登记的元数据、资源归属和过滤委托为准**，之后只在日志里警告 | 指南「RegisterPlayerRune」 |

**术语校准（原先我们搞错的地方）**：

- 软依赖路线下，**符文根本不需要继承 `HextechRelicBase`** —— 任意 `RelicModel` 子类即可。
  `HextechRelicBase` 只在**硬依赖**（`HextechRunesApi`）路线才强制。
- 「符文的资源归属」由**我们自己的** `PackedIconPath` 决定，不是海克斯的
  `res://{assetModId}/images/relics/{小驼峰}.png` 命名链（那条链只在硬依赖路线走）。
- 来源标签 / 分组标题的本地化表是 **`relic_collection`**（文件 `relic_collection.json`），
  不是 `relics`；符文**自身**的名称/描述才走 `relics` 表。
- 「第三方符文要不要自己进遗物池」有了官方答案：**海克斯会把它加进原版 `SharedRelicPool`**
  （为了拿到模型身份 + 图鉴显示），再靠「只有 `Starter` 能保证原版别的抽取路径抽不到」来兜底。
  所以我们**不需要**为联动去动池注册；我们自己那份 `[RegisterRelic(typeof(LinRelicPool))]` 只服务于
  「没装海克斯时也能玩」。

---

## 1. 结论

1. **零 patch。** 官方 `HextechRunesInterop` 覆盖了我们全部诉求（注册符文 / 品级 / 标志 / 角色池 / 标签 /
   可用性过滤 / 来源标签 / 配置菜单分组标题 / 额外阶段提示）⇒ 上一版文档里那 18 个 `HextechCatalog`
   postfix **全部作废**（并且现在是被明令禁止的做法，见 §0-1）。
2. **不硬引用、不建子程序集。** 用**反射**调 `HextechRunesInterop` 的 public static 成员
   （或 RitsuLib `[ModInterop]`，但对海克斯**不推荐**，理由见 §2.4）。产物 DLL 里**不得**出现对
   `HextechRunes` 的任何编译期引用。
3. **新增一个「注册桥」**：`ManosabaLinCode/Compat/Hextech/HextechRuneRegistrar.cs`（+ 门控
   `HextechCompat.cs` + 元数据表 `HextechRuneCatalog.cs`）。这是本次联动**唯一**要写的新代码。
4. **我们的符文本来就是合规的**：`ManosabaRelicTemplate : ModRelicTemplate : RelicModel`，
   `Rarity` 自己覆写 ⇒ 只需保证**每个海克斯符文都声明 `RelicRarity.Starter`**（§0-6）。
5. **硬依赖路线（`HextechRunesApi`）我们不走** —— 我们不是「与海克斯一起发布」的拓展包，
   必须做到「对方没装也能跑」。
6. **注册桥已落地**（2026-09-28）：`Compat/Hextech/` 三个文件 + `MainFile.Initialize()` 接线，见 §2.9；
   6 个联动遗物本身（离线那条线）见 §2.8。两条线现在都在了。

---

## 2. 施工单

### 2.1 我方符文自己登记（不做任何海克斯相关继承）

- 符文 = 现有 `ManosabaRelicTemplate` 子类，按现有方式 `[RegisterRelic(typeof(LinRelicPool))]`
  （`LinRelicPool` 用 `[RegisterSharedRelicPool]`，见 §2.7）。
- 图标走 `ManosabaRelicTemplate.AssetProfile`（`res://ManosabaLin/images/relics/<类名小写>.png`，
  再问一次 `ResourceLoader.Exists` 回退 `relic.png`）；文案走原版 `relics` 表
  （`Id.Entry + ".title"/".description"/".flavor"`）；稀有度 `override RelicRarity => RelicRarity.Starter`
  ⇒ **全部不需要海克斯参与**。
- ⚠️ **不要**调 `ModHelper.AddModelToPool(typeof(SharedRelicPool), 我方符文)`：
  海克斯自己会把它加进 `SharedRelicPool`，我们重复加只会让自己更早被原版别的抽取路径看见。
- 本组 6 个联动遗物同时是**海克斯符文**（`Starter`），所以它们**两份身份共存**：
  ① 我方 `LinRelicPool` 里的普通遗物（离线可玩）；② 海克斯注册表里的符文（装了才生效）。

### 2.2 门控 `Compat/Hextech/HextechCompat.cs`

```csharp
internal static class HextechCompat
{
    public const string AssemblyName = "HextechRunes";       // ⚠️ 按程序集名，不按 manifest id
    public const string InteropTypeName = "HextechRunes.HextechRunesInterop";
    public const int MinimumApiVersion = 1;

    public static Assembly? Assembly { get; private set; }
    public static Type? InteropType { get; private set; }
    public static int ApiVersion { get; private set; }        // 0 = 不可用

    /// <summary>在 MainFile.Initialize() 里调用一次；未找到程序集时订阅 AssemblyLoad 等它。</summary>
    public static void Initialize() { /* 见 §2.4 骨架 */ }

    public static bool IsReady => Assembly != null && InteropType != null && ApiVersion >= MinimumApiVersion;
}
```

- **能力探测优先于版本号**：`ApiVersion` 只用来做「太旧就整体跳过」的门槛；具体能力用
  「目标方法/属性解析得到没有」判断（`GetMethod(name, flags, [参数类型...])` 返回 null ⇒ 该能力不可用，
  单独降级，不要整体放弃）。
- 复用现有基建：`Compat/Core/OptionalModCompatRegistry.IsModLoaded(string modId)` 是**按 manifest id**
  判的（`OptionalModCompatRegistry.cs:17`）⇒ **不能直接拿来做这次的检测**，需要按程序集名另写一个
  `IsAssemblyLoaded(string assemblyName)`。两者可以并存。
- `ApiVersion` 反射取不到 ⇒ 对方版本 < 0.9.6 ⇒ 打一条 Info 后**整体跳过**（指南「版本检查」）。

### 2.3 我方侧元数据表 `Compat/Hextech/HextechRuneCatalog.cs`

一张纯数据的表，**直接就是 `RegisterPlayerRune` 的入参**（不需要再转换对方类型，因为软依赖签名收的是
**字符串**枚举名）：

```csharp
internal sealed record HextechRuneSpec(
    Type RuneType,
    string Rarity,                   // "Silver" / "Gold" / "Prismatic"（不区分大小写；⚠️ 不接受数字）
    string? Flags,                   // 逗号分隔：Disabled / FirstActExcluded / ThirdActExcluded
                                     //           / SelectionExcluded / AttributeConversionExclusive
    string? CharacterPool,           // Ironclad/Silent/Regent/Defect/Necrobinder；通用 = null（见 §5-1）
    int CharacterOrder,
    string TagKey,                   // "MANOSABA_LIN"（自造，见 §2.5）
    string AssetModId,               // "ManosabaLin"
    Func<Player, bool>? Availability // 见 §0-9 确定性要求
);

internal static class HextechRuneCatalog
{
    public static IReadOnlyList<HextechRuneSpec> All { get; }
    public static bool Contains(Type t);
    public static bool TryGet(Type t, out HextechRuneSpec spec);
}
```

- 规则（品级 / 幕数 / 可用角色）**全记在我们自己这边**，注册时一次交出去；后续不读对方任何状态。
- `Availability` 必须是**确定性纯函数**（只看 `player.Character` / `player.Relics` / `RunState`）。

### 2.4 注册桥 `Compat/Hextech/HextechRuneRegistrar.cs` ★核心

**用反射还是 `[ModInterop]`？**

- **本项目选纯反射。** 理由：① 指南要求「按程序集名检测」，而 RitsuLib 的
  `[ModInterop(modId, type)]` 是**按 manifest id** 解析目标程序集的（`ModInteropAttributes.cs:18`）
  —— 二创版 manifest id 不同就会失效；② 我们要先读 `ApiVersion` 再决定要不要注册，纯反射更直接；
  ③ 指南给的就是反射示例，照抄即可。
- 若将来想用编译期校验，可另加 `[AssemblyInterop("HextechRunes.HextechRunesInterop, HextechRunes")]`
  空壳（assembly-qualified 形式与「按程序集名」的要求一致），但**代理方法参数列表必须与指南签名逐字一致**，
  且它不解决 `ApiVersion` 门槛，收益有限。

**注册时机（照抄指南，`MainFile.Initialize()` 里调 `HextechCompat.Initialize()`）**：

```csharp
public static void Initialize()
{
    var loaded = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == AssemblyName);
    if (loaded != null) { TryRegister(loaded); return; }
    AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;   // 载入后自解绑再注册
}
```

**一次注册要做的 4 件事**（顺序：先 `RegisterPlayerRune`，再两个界面文字）：

```csharp
var register = interop.GetMethod("RegisterPlayerRune",
    BindingFlags.Public | BindingFlags.Static, null,
    [typeof(Type), typeof(string), typeof(string), typeof(string), typeof(int),
     typeof(string), typeof(string), typeof(Func<Player, bool>)], null);
// ↑ 参数列表必须与指南完全相同；返回 null ⇒ 对方版本太旧，整体跳过
```

| 步骤 | 调用 | 说明 |
| --- | --- | --- |
| 1 | `RegisterPlayerRune(type, rarity, flags, characterPool, characterOrder, tagKey, assetModId, availability)` | 每个符文一次；参数无效抛 `ArgumentException`（反射时是 `TargetInvocationException.InnerException`）⇒ **捕获并 Warn，不要让它打断整个 mod 初始化** |
| 2 | `SetPlayerRunePoolLabel(type, "MANOSABA_LIN")` | 三选一界面「来源标签」；文字取 `relic_collection` 表的 `HEXTECH_POOL.MANOSABA_LIN` |
| 3 | `RegisterConfigSectionTitle("ManosabaLin", "MANOSABA_LIN_HEXTECH_SECTION")` | 配置菜单里我们这组内容的小标题；`titleKey` 是 `relic_collection` 表的**完整键** |
| 4 | 打一条汇总日志 | 注册了几个、`ApiVersion` 多少、失败的逐个列出 |

- ⚠️ 串行化保护：`Initialize()` / `OnAssemblyLoad` / 注册体都要能被重复调用而**不重复注册**
  （对方以第一次为准，但我们会白刷警告）。
- ⚠️ **不要**在注册体里做任何需要「游戏已初始化」的事；也不要写 `[SavedProperty]` 载体登记
  （那是硬依赖 `RegisterSavedPropertyCarrier` 的活；软依赖下我们自己不给符文加 `[SavedProperty]`
  ⇒ 符文必须**无持久字段**，需要跨战斗状态就往 `Power` / `RelicModel` 原有机制上放）。

### 2.5 界面文字与本地化

| 界面位置 | 我方设置 | 本地化键 | 未设置时 |
| --- | --- | --- | --- |
| 来源标签 | `SetPlayerRunePoolLabel(type, "MANOSABA_LIN")` | `relic_collection` 表的 `HEXTECH_POOL.MANOSABA_LIN` | 通用显示「通用」 |
| 海克斯标签 | `RegisterPlayerRune(..., tagKey: "MANOSABA_LIN")` | `relic_collection` 表的 `HEXTECH_TAG.MANOSABA_LIN` | `COMPREHENSIVE`（综合） |
| 配置菜单小标题 | `RegisterConfigSectionTitle("ManosabaLin", "MANOSABA_LIN_HEXTECH_SECTION")` | `MANOSABA_LIN_HEXTECH_SECTION` 本身 | 「外部模组：」 |

- **可以直接用海克斯已有键**，省得自己写：来源标签 `GENERIC / IRONCLAD / SILENT / REGENT / DEFECT / NECROBINDER`；
  海克斯标签 `COMPREHENSIVE OUTPUT SURVIVAL RESOURCE ORB SUMMON RANDOM SWORDCRAFT DOOM POISON ECONOMY
  STATUS STARLIGHT STACKING MULTIPLAYER BLOODLETTING SHIV EXHAUST VOID TRICK DRAW COLORLESS`。
  ⚠️ **海克斯标签还决定三选一加权**：玩家已拥有的同标签海克斯越多，同标签候选权重越高
  ⇒ 自造 `MANOSABA_LIN` 标签会让我们的符文**只在自己人之间互相加权**，这正是我们要的。
- **自造键的落地位置**：`ManosabaLin/localization/{zhs,eng,jpn,kor,rus}/relic_collection.json`。
  ⚠️ 游戏**只合并与原版同名的本地化表** ⇒ **文件名必须是 `relic_collection.json`**；语言目录用游戏语言
  代码；其余语言缺失时**回退 `eng`**。键不存在**不报错**，界面直接显示键名（看到键名 = 本地化没生效）。
- 新表按 `manosaba-lin-localization` 技能：zhs 先定稿 → 同步 4 语言 → `json.loads` 校验 + CRLF 计数；
  新表**无 BOM、LF、2 空格缩进、末尾换行**（对齐 `static_hover_tips.json`）。
- 符文**自身**的名称/描述/风味仍走 `relics` 表（`MANOSABA_LIN_RELIC_<SNAKE>.{title,description,flavor}`）
  —— 与海克斯无关。

### 2.6 排障与联机

- 日志前缀固定为 **`[HextechRunes][ExternalContent]`**（对方侧）；我们自己的日志带
  `[ManosabaLin][Hextech]`，两边都要能一眼对上。
- 符文没出现时的自查顺序：① 配置菜单里是否已开启；② 品级 / 标志是否把它排除在当前幕之外；
  ③ `Availability` 是否返回了 `true`；④ 我们的注册是否真的发生在**注册窗口关闭之前**。
- **联机**：两端模组列表必须一致，**包括海克斯的版本**；`isAvailableForPlayer` 与
  `RegisterExtraActProvider` 的委托只允许依赖同步状态。
- ⚠️ 新增/改名/删除 `[SavedProperty]` 会改变存档同步的字段布局 ⇒ 新旧版本之间**无法联机**。
  我们软依赖路线下不给符文加持久字段，天然规避。

### 2.7 参数约定（我方取值）

| 参数 | 取值 | 理由 |
| --- | --- | --- |
| 符文基类 | `ManosabaRelicTemplate`（已 `: RelicModel`） | 指南只要求 `RelicModel` 子类 |
| `Rarity` | `RelicRarity.Starter`（**强制**） | 只有 Starter 能保证原版其他抽取路径抽不到外部符文 |
| `rarity` 品级 | Silver / Gold / Prismatic | 照海克斯三档；**不接受数字** |
| `flags` | 语义上**不要**用 `Disabled`。要「图鉴可见但不进随机选择」→ `SelectionExcluded`；「第一幕不出」→ `FirstActExcluded`；「第三幕不出」→ `ThirdActExcluded` | 对应 `PlayerRuneMetadataCatalog` 的 `IsVisible / IsSelectable / IsConfigurable` |
| `characterPool` | **`null`（通用）** | 对方枚举只有原版 5 个角色，**没有 mod 角色**（见 §5-1） |
| `tagKey` | `"MANOSABA_LIN"`（自造） | 默认 `COMPREHENSIVE` 会和对方「综合」符文挤同一个加权/计数桶 |
| `assetModId` | `"ManosabaLin"` | 配置菜单「来源」显示；**建议填** |
| 遗物池（我方） | `LinRelicPool`（`[RegisterSharedRelicPool]`） | 服务「没装海克斯时也能玩」那条线 |

---

## 3. 验证清单

1. **编译**：`dotnet -t:Compile` 0 错误；⚠️ 产物 DLL **不得**出现对 `HextechRunes` 的编译期引用
   （反射 + `AppDomain` 都是运行时的；这本身就是「没硬引用」的验收）。
2. **未装分支**（无头 harness 天然没有海克斯）：锁死「`HextechCompat.IsReady == false` ⇒
   注册整体跳过、无异常、我方符文的**离线行为**（`LinRelicPool` 那条线）完全不变」。
3. **装了分支**（本机已装）：
   ① 图鉴里出现「海克斯」分类且我方符文在列；
   ② 符文三选一 / 发放流程能抽到我方符文，卡片下方三枚小标签（品级 / 来源 / 标签）正确；
   ③ 配置菜单里能看到我们的分组小标题，且单独开关生效；
   ④ 图标是我方图，**不是** `missing_power.png`；
   ⑤ **原版遗物奖励不出现我方符文**（靠 `Starter` 兜底，必须实测）；
   ⑥ 海克斯「复视」不复制我方符文（指南已声明，回归确认即可）。
4. **启动自检**：把「程序集检测到没有 / `ApiVersion` 多少 / 每个方法的解析结果 / 每个符文的注册结果」
   全部打日志；⚠️ **静默跳过是最贵的坑**，任何一步失败都必须显式报出来。
5. **时序**：故意把注册挪到「晚于游戏初始化」的位置跑一次，确认会抛
   `InvalidOperationException` 而不是**半登记**（这验证我们真的卡在窗口内）。
6. 产物核验：`ilspycmd -t` 看注册桥类型在导出 DLL 里；⚠️ `grep -a` 对 .NET DLL **永远 0 命中**（UTF-16）。
7. ⚠️ 本机游戏目录 DLL 可能被并发会话覆盖 ⇒ 跑测试/发布前先 `cp` 备份、跑完 `cp -f` 还原。

### 2.8 6 个联动遗物：已落地（2026-09-28，`-t:Compile` 0 错误）

> 📌 2026-09-29 又追加了 **3 个安安专属**联动遗物（书页 / 安心 / 缄默·洗脑）⇒ 见 **§2.10**，本节只讲最初这 6 个。

> ⚠️ **本节记录的是「离线那条线」** —— `LinRelicPool` 里的普通 Starter 遗物。
> §2.2 / §2.3 / §2.4 的**海克斯注册桥仍未开工**，所以现在不论装不装海克斯，这 6 个遗物都只有
> 我方普通遗物这一份身份，还没有「海克斯符文」的第二身份。

**共享遗物池**：`ManosabaLinCode/Characters/Common/LinRelicPool.cs`
（`[RegisterSharedRelicPool]` + `TypeListRelicPoolModel`，同构先例 `LinCardPool`）
⇒ 进 `ModelDb.AllSharedRelicPools`，成为 5 个角色池之外的「Lin 池」。
⚠️ 只被 `[RegisterRelic]` 注入的池**不会**自动进 `ModelDb.AllRelicPools`，所以必须用
`[RegisterSharedRelicPool]` 而不是裸 `TypeListRelicPoolModel`。

> ⚠️ **2026-10-15（用户裁定）**：12 个联动遗物**保留注册在 `LinRelicPool`**
> （曾短暂改挂到各自角色的专属池，随后按用户指令改回）。
> 「角色专属」**不由引擎的角色遗物池表达**，而由**海克斯侧**决定：
> ① `SetPlayerRuneCharacter` 的绑定（150% 加权；判定口径也以它为准）；
> ② 本 mod 的展示层补丁 `ManosabaLinCode/Compat/Hextech/HextechCharacterGroupsPatch.cs` ——
> 把「已绑定我方角色」的符文从图鉴的「通用」档移入专属档（档位键
> `HEXTECH_CHARACTER.MANOSABA_LIN_<角色>`，文案在我们 5 语的 `relic_collection.json` 里）。
> 补丁只动 `GetCharacterRuneGroups` / `GetGenericVisibleRuneTypes` 两个**纯展示**方法，带形状校验与失败降级。

**6 个遗物**（全部 `ManosabaRelicTemplate` + `[RegisterRelic(typeof(LinRelicPool))]` + `Rarity => RelicRarity.Starter`；
目录 `ManosabaLinCode/Characters/Common/LinRelics/`）：

| # | 类 | 效果 | 关键落点 |
| --- | --- | --- | --- |
| 1 | `HextechDeckCleanser` | 每回合**自动抽牌前**可把四堆里任意张牌扔进弃牌堆 | `BeforeHandDraw` + `CardSelectCmd.FromSimpleGrid`（自带同步） |
| 2 | `HextechEmotionOverflow` | 球位满时按三类分派：**持续型脱离球位、改挂血条下方**（还是那颗球、效果照旧）、反伤型/延迟型**被挤出时立刻激发**（**2026-09-29 定稿，见下行**） | `OrbCmd.Channel` prefix + `AfterOrbEvoked` + `IEmotionOrb` + 挤出标记 + `HangingEmotionOrbs`（`ModHelper.SubscribeForCombatStateHooks`） |
| 3 | `HextechWitchificationCore` | 【魔女化计数】每 +1 就 +10 层【魔女化】（由「保留 → 下回合开始 +1 计数」驱动）；**旧版 300→100 削层折返已删（2026-10-01）** | `Witchification.AddWitchificationCount` 回调 + `Witchification` 的保留三件套 |
| 4 | `HextechTransmigrationEcho` | 打出【轮回】牌改为自动打抽牌堆任意 2 张【轮回】 | `TransmigrationSingleton.AfterCardPlayed` prefix 接管 |
| 5 | `HextechPerjuryPayment` | 能量不足时用 3×缺口【伪证】顶替；1 层【正义】→4 层【伪证】 | `HasEnoughResourcesFor` postfix + `SpendResources` prefix |
| 6 | `HextechJusticeRegen` | 回合结束【正义】>【再生】则补齐【再生】；【正义】不再回血 | `AfterSideTurnEnd` + 拦 `JusticePower.AfterSideTurnEnd` 的 Heal |

> ⚠️ **离线时这 6 个遗物「局内拿不到」是刻意的，不是 bug（用户 2026-09-29 裁定「跟随海克斯，不用你管」）**
> —— **不要**为了让它们能被抽到去改 `Rarity`。完整链路（`RelicGrabBag.Populate`）：
> ```csharp
> list = ModelDb.RelicPool<SharedRelicPool>().GetUnlockedRelics(...);   // 只认**原版** SharedRelicPool
> list.AddRange(player.Character.RelicPool.GetUnlockedRelics(...));     // + 当前角色的池（LinRelicPool 不在其中）
> list.RemoveAll(r => !_rarities.Contains(r.Rarity));                   // _rarities = Common/Uncommon/Rare/Shop
> ```
> 两道闸：`LinRelicPool` 不在抓取袋的两个来源里；且 `Rarity` 必须 Starter ⇒ 必然被 `RemoveAll` 丢掉
> （`RelicFactory.RollRarity` 也只掷 Common/Uncommon/Rare）。
> ⇒ **获取渠道由海克斯符文系统负责**（装了才发），我方这条离线身份只保证「拆了海克斯也不报错」。

**⚠️ 持续型情绪「脱离球位、挂到血条下方」的实现（2026-09-29 定稿；推翻 09-28 晚的「能力接管」版，
也推翻 09-29 早些时候「不挤出、球留在球位」的折中版）**：

用户两轮裁定原话：「原本…效果实现**就不需要能力**…**卡牌挂在血条下面不就等于充能球换个地方挂着**」
→「**脱离默认球位**，改为挂在血条下面」。
⇒ **绝不允许**再造能力（Power）来接管效果，也**不允许**把效果逻辑搬到别处重写一遍。
正确做法 = **让那颗球真的离开球位，但仍然由它自己继续生效**：

- ⭐ **关键引擎出口**：`ModHelper.SubscribeForCombatStateHooks(id, CombatHookSubscriptionDelegate)`。
  `CombatState.IterateHookListeners()` 最后会 `foreach (var m in ModHelper.IterateAllCombatStateSubscribers(this)) yield return m;`
  ⇒ 模组可以把任意 `AbstractModel` 注册成该战斗的钩子监听者。
  **所以「换个地方挂着的还是那颗球、效果一点没变」，不需要 Power、不需要把逻辑搬到卡组件上。**
- 落地（`HextechOrbOverflowPatch`，`OrbCmd.Channel` Prefix，球位已满且队首是持续型情绪球且该玩家有遗物 2）：
  1. `queue.Remove(front)` —— 从模型队列摘下 ⇒ 后面 `Orbs.Count >= Capacity` 不成立 ⇒ **不会 EvokeNext**；
  2. `OrbManager.EvokeOrbAnim(front)` —— 视觉上离场（顺带把空位补回队尾，保证接下来 `AddOrbAnim` 能找到空位）；
  3. `HangingEmotionOrbs.Hang(player, front)` —— 登记成战斗钩子监听者 + 通知 UI。
- **下个玩家回合开始**：`HextechEmotionOverflow.AfterPlayerTurnStart` ⇒ `HangingEmotionOrbs.Release(player)`
  （逐个 `orb.RemoveInternal()`）—— 与球体自身「到下回合开始消散」对齐。
- 显示：`EmotionHangDisplay : Control`，由 `EmotionHangDisplayPatch`（`NCreature._Ready` postfix）挂到每个玩家生物上，
  每帧贴在 `%HealthBar`（`NCreatureStateDisplay`）→ `%HealthBar`（`NHealthBar`）→ `HpBarContainer` 正下方。
  卡面渲染照抄 `EmotionOrbVisualPatch`（`NCard.Create` + `UpdateVisuals(PileType.None, CardPreviewMode.Normal)` + scale 0.5 + 去掉文字层），
  悬浮照抄 `YalisalinFireColorCounter`（透明 `ColorRect` 当靶子 + `NHoverTipSet.CreateAndShow(HoverTipFactory.FromCard(card))`）。
- ⚠️ `HangingEmotionOrbs.Iterate` **必须立刻物化成列表**：钩子派发过程中可能有人 `Release` 改字典，
  惰性迭代字典会抛 `InvalidOperationException`。
- ⚠️ 旧版「`OrbCmd.AddSlots(+1)` 多开一格 + 之后归还」那套记账（`NoteExtraSlot` / `TryReturnExtraSlot` / `ExtraSlots`）**已全部删除**
  —— 用户要的是脱离球位，不是占着球位不让出去。
- ⚠️ `EmotionElationOrb` 本体没有钩子（效果在卡 `OnPlay` 与 `Loseengry` 里），球只是「还有雀跃」的标记；
  它离开球位后 `Loseengry.RemoveAndTrigger` 的 `hasOrb` 判定必须**把挂着的雀跃球也算上** ⇒
  改成 `orbs.Any(...) || HangingEmotionOrbs.HasHanging(Owner.Player, o => o is EmotionElationOrb)`。

**遗物 3 的「计数来源」**：`Witchification` 组件照搬 `RetainCounterComponent` 范式
（`OnAttach` / `BeforeSideTurnEndPostfix` 续期 `GiveSingleTurnRetain()`，`AfterPlayerTurnStartEarlyPostfix`
在 `PileType.Hand` 里 `AddWitchificationCount(1)`）⇒ 每回合保留一次就 +1 计数 ⇒ 回调 +10 层【魔女化】。
⚠️ 2026-10-01 用户裁定：遗物 3 **只有「保留 → +1 计数 → 获得魔女化」这一件事**，
旧版「回合开始把 >300 层削减到 100 并 25:1 转计数」已删除（`AfterPlayerTurnStart`、`CardsWithWitchification`、
`grantWitchification:false` 折返路径全部移除）。

**Patch 文件**：`ManosabaLinCode/Patches/HextechRelicPatches.cs`，5 个 patch 类。
⚠️ **全部打的是引擎方法或我方自己的类型**（`OrbCmd.Channel` / `PlayerCombatState.HasEnoughResourcesFor` /
`CardModel.SpendResources` / `TransmigrationSingleton`（我方）/ `JusticePower`（我方）），
**没有一处碰海克斯的 `internal` 类**（§0-1）。

**本地化**：5 语言 `relics.json` 各追加 **19** 键（6 组 `title`/`description`/`flavor` = 18，
遗物 1 另需 `selectionScreenPrompt`）；`powers.json` **无新增**
（09-28 晚为「持续型能力」加的 21 键已于 09-29 全数撤回 —— 持续型不需要能力）。

**⚠️ 与原始描述的偏差**（用户 2026-09-28 已裁定 ①②⑤，其余待定）：

1. ✅ **遗物 2「厌恶 / 骇厌立刻执行一次」的伤害量来源（已裁定，2026-09-28 晚修正）**：
   原描述没给伤害量从哪来。用户裁定 = **直接用敌人当前意图里的攻击总伤害**。
   ⚠️ **目标不是「敌人打自己」，而是「敌人打激发者（你）一次」**（用户 2026-09-28 晚明确纠正，我原先按「打它自己」实现是错的）。
   流程 = 遍历**所有正打算攻击**的敌人 → 各用自己的攻击**打你**一次 → 随即结算该情绪的反伤
   （厌恶：等量打回该敌人；骇厌：敌方全体吃等量伤害 + 抽 1）→ 最后**回复等量生命**（等于你这次总共挨的伤害）。
   落点照搬我方既有卡 `AnanlinTakeTheHitForTeammate`：`move.Intents` 里 `AttackIntent.GetTotalDamage(...)` 取值
   → `DamageCmd.Attack(...).FromMonster(monster).Targeting(Owner.Creature)` → 手动补 `MoveStateMachine.OnMovePerformed`
   + `History.MonsterPerformedMove`，**全程不调** `PerformMove`/`SetMoveImmediate` ⇒ **敌人原本意图不变**。
   ⚠️ 没有任何敌人「正打算攻击」时无伤害量可取 ⇒ 本项不生效。
   ⚠️ 这是**真实承伤**（受格挡/能力影响）⇒ 低血时主动挤球可能致死；好处是触发时机完全由玩家掌握（只有主动 Channel 满位球才走到这里）。
   ⚠️ 球在 `Hook.AfterOrbEvoked` 之前就已经 `OrbQueue.Remove`（`OrbCmd.Evoke` 先 Remove 后播 hook），
   而 `CombatState.IterateHookListeners` 是从 `player.PlayerCombatState.OrbQueue.Orbs` 枚举球的
   ⇒ 球自己的 `AfterDamageReceived` **不会再跑**，不会重复结算反伤。
2. ✅ **遗物 3「10 倍」（已裁定）** = **【魔女化计数】每 +1 ⇒ +10 层【魔女化】**。
   **不是**「挂上组件那一刻按当前【魔女化】层数 × 10 放大一次」（旧的 `Witchification.OnAttach` 回调已删）。
   现由 `Witchification.AddWitchificationCount` 回调 `HextechWitchificationCore.OnWitchificationCountGained`。
3. 遗物 3 的计数**刻意不加 `[ComponentState]`**（不持久化）—— `Witchification` 是既有序列化类型，
   加字段会改 `[SavedProperty]` 布局、破坏联机与旧存档。
4. ✅ **遗物 5「完全没有【伪证】时不生效」（已裁定）** = **只要有【正义】就照样生效**；
   兑换后**多出来的层数原样留成【伪证】**。
   做法：不再要求 `PerjuryPower` 已存在 —— 玩家一层【伪证】都没有时，用同步 API
   `ModelDb.Power<PerjuryPower>().ToMutable()` + `ApplyInternal(creature, remaining, silent: true)` 现场建一个
   （等价于 `PowerCmd.Apply` 的同步内核：`SetAmount` + `Owner.ApplyPowerInternal`；后者才是「注册为战斗监听器」的那一步）。
   ⚠️ 不能在 prefix 里 `await PowerCmd.Apply`（同步扣费前缀等不了异步），所以必须走这条同步路径。
   `CanCoverEnergyDeficit` 早就已经把【正义】折算进来了，所以 `HasEnoughResourcesFor` 那侧无需改动 —— 之前只是**消费侧**走不到。
   顺带：`ApplyInternal` 与 `SetAmount` 都**不触发** `AfterPowerAmountChanged` / `AfterApplied`
   ⇒ 兑换出的多余【伪证】**不会被自动转回【正义】**，正好符合「多余的正常保存为伪证」。
5. ✅ **遗物 6 时序（已裁定）** = 补出的【再生】**本回合就回血**。
   做法：把补层从 `AfterSideTurnEnd` 提到 **`BeforeSideTurnEndVeryEarly`** ——
   它是 `Hook.BeforeSideTurnEnd` 内部的第一个子阶段（`Hook.cs`：`VeryEarly` → `Early` → `BeforeSideTurnEnd` 各遍历一遍监听器），
   因而排在 `RegenPower.BeforeSideTurnEndEarly`（结算回血 + 递减）**之前**。
   附带好处：此刻 `JusticePower.Amount` 还没被它自己的 `AfterSideTurnEnd` 递减 ⇒ 读到的是「回合结束时的当前层数」，
   与监听器顺序无关（旧实现挂在 `AfterSideTurnEnd`，与 `JusticePower` 同阶段、顺序未定义，可能读到已递减的值）。
6. ✅ **遗物 2 整套重做（用户 2026-09-28 晚「现在就整套重做」+ 09-29「不要能力 / 脱离默认球位」三次裁定）** —— 14 颗情绪球按三类重排：
   - **持续型 7 种**（悲伤 / 愤怒 / 快乐 / 怅然 / 雀跃 / 好奇 / 友谊）⇒ **脱离默认球位**：球从 `OrbQueue` 里被摘下、
     改**挂在血条下方**（`EmotionHangDisplay`），由**球自身钩子**继续生效，到下个玩家回合开始释放。
     ⚠️ **不是**「效果搬进血条下方的能力」（09-28 晚那版 7 个 Power 已全部删除）；
     ⚠️ **也不是**「球留在球位不让出去」（09-29 早些时候那版 `OrbCmd.AddSlots(+1)` 已删除）；
     挂着的**就是同一颗球**，靠 `ModHelper.SubscribeForCombatStateHooks` 继续收钩子。
   - **反伤型 2 种**（厌恶 / 骇厌）⇒ 见上方第 1 条（两者一律「所有正打算攻击的敌人各打你一次」，不再区分单/全体触发者）。
   - **延迟型 5 种**（恐惧 / 惊讶 / 恼惧 / 凄惶 / 无助）⇒ 「回合结束 / 下回合开始」才结算的收益**立刻结算一次**。
     旧实现只覆盖恐惧 + 惊讶，现补上恼惧（先掉 3 血、自己和队友拿到剩余等量格挡）、凄惶（先回半格挡血、再拿等量格挡）、
     无助（把负敏捷换成等量荆棘 + 给随机敌人等量负力量）。
   - 骇厌额外把「下回合开始随机造成 = 手牌数的 1 点伤害」也**立刻结算一次**。
7. ✅ **遗物 2 触发条件（用户明确）**：只有玩家**主动获得充能球、且把球挤出去**才触发 ⇒ 靠 `OrbCmd.Channel` 的
   Prefix 打标记（`HextechOrbOverflowPatch`）+ `AfterOrbEvoked` 取走标记，正常消散（`EvokeNext`）不触发。
   ⚠️ 持续型是唯一的例外：它**不会被挤掉**（在 `OrbCmd.Channel` 的 Prefix 里就被摘下来挂着了），
   因此 `AfterOrbEvoked` 里对持续型一律不结算（走到那里只可能是它自己在回合开始消散）。
8. ✅ **雀跃的循环接续**：雀跃球本体没有钩子，它的效果写在卡 `OnPlay`（获得 = 能量上限的能量 + 挂 `Loseengry`）和
   `Loseengry` 内部；球只是「还有雀跃」的**标记**。⚠️「挂到血条下方」方案下球已**不在** `OrbQueue` ⇒
   `Loseengry.RemoveAndTrigger` 的 `hasOrb` 必须**额外**查一份：
   `orbs.Any(o => o is EmotionElationOrb) || HangingEmotionOrbs.HasHanging(Owner.Player, o => o is EmotionElationOrb)`
   （09-28 晚为「纯标记能力」加的 `hasElation` 分支与 `using` 已回退，这次只加这一行）。

**愤怒的后半段（2026-09-29 补齐，用户「愤怒把效果改成本地化这样」）**：
`EmotionAngerOrb` 原本只有 `ModifyDamageMultiplicative => 2m`，**漏了**本地化里明写的后半句
（`MANOSABA_LIN_CARD_EMOTION_ANGER.description` / `MANOSABA_LIN_ORB_EMOTION_ANGER_ORB.description`）：
「造成双倍伤害，**每造成20伤害对随机友方造成1点伤害**」。
现补在 `AfterDamageGiven`：只统计**打敌人**的伤害、取 `DamageResult.TotalDamage`（= 挡掉的 + 实打实的，
就是玩家看到的被翻倍后的那串数字），每满 20 点用 `Owner.RunState.Rng.CombatTargets` 选一个存活友方打 1 点
（`ValueProp.Unpowered`）；`_resolvingBacklash` 重入保护保证反噬自己造成的伤害不再被统计。

**遗物 3「计数是拆分」不是「每张各一份」（用户 2026-09-29 裁定）**：
> ⚠️ 2026-10-01 起**该条作废**——「拆分」逻辑随 300→100 削层折返一起删除（见上）。
> 现在计数**只**来自「保留」自动累积（每张带组件的卡各自 +1），不再有「把 N 点计数随机瓜分给组件卡」这条路径。

### 2.9 注册桥：已落地（2026-09-28，`-t:Compile` 0 错误）

**新增 3 个文件**（`ManosabaLinCode/Compat/Hextech/`）：

| 文件 | 职责 |
| --- | --- |
| `HextechCompat.cs` | 门控。按**程序集名**找 `HextechRunes` → 解析 `HextechRunes.HextechRunesInterop` → 读 `ApiVersion`；没找到就订阅 `AssemblyLoad`（载入后自解绑再注册）；`ResolveInteropMethod(name, paramTypes)` 供**逐成员**能力探测 |
| `HextechRuneCatalog.cs` | 纯数据。**9 条** `HextechRuneSpec`（字段就是 `RegisterPlayerRune` 入参；2026-09-29 追加安安 3 条）+ `Validate()` 自检（具体类型 / `RelicModel` 子类 / 品级名合法） |
| `HextechRuneRegistrar.cs` | 反射桥。`RegisterPlayerRune` × 9 → `SetPlayerRunePoolLabel` × 9 → `RegisterConfigSectionTitle` × 1；幂等，**逐个 try/catch**（`TargetInvocationException.InnerException` 解包），绝不因单个符文打断 mod 初始化 |

**接线**：`MainFile.cs` 在 `GuardOneRewardRegistrar.Register()` 之后调 `HextechCompat.Initialize()`（紧邻 `harmony.PatchAll()`），
注释里点明「必须在模组初始化阶段调用，要赶在共享遗物池首次枚举之前」。

**与 §2.2 草图的差异**：门控的程序集属性改名 `Assembly` → **`TargetAssembly`**
（`Assembly` 会与 `System.Reflection.Assembly` 类型名在同一作用域冲突）。

**数据表（2026-09-28 用户裁定可用性：3 张仅雪莉琳 + 3 张仅希罗，无「通用」；2026-09-29 用户裁定品级：全部棱彩；
2026-09-29 追加 3 张仅安安，品级同样棱彩）**

| # | 遗物 | 海克斯**品级** | `isAvailableForPlayer` | 依据 |
| --- | --- | --- | --- | --- |
| 1 | `HextechDeckCleanser` | Prismatic | **仅雪莉琳** | 用户裁定（我原按「不依赖角色专属机制」定通用，被否） |
| 2 | `HextechEmotionOverflow` | Prismatic | **仅雪莉琳** | 情绪球只存在于雪莉琳（其余角色 0 处引用） |
| 3 | `HextechWitchificationCore` | Prismatic | **仅雪莉琳** | 用户裁定（【魔女化】虽 5 个角色都有，但本遗物绑定雪莉琳那套组件链） |
| 4 | `HextechTransmigrationEcho` | Prismatic | **仅希罗** | 【轮回】只在希罗卡池（25 处引用，其余角色 0） |
| 5 | `HextechPerjuryPayment` | Prismatic | **仅希罗** | 【伪证】/【正义】只在希罗卡池（27 处） |
| 6 | `HextechJusticeRegen` | Prismatic | **仅希罗** | 同上 |
| 7 | `HextechColorlessPage` | Prismatic | **仅安安** | 书页（`BlankPage`/`MarginPage`/`BorrowedMarginPage`）只在安安体系里（详 §2.10） |
| 8 | `HextechPeaceOfMindEcho` | Prismatic | **仅安安** | 【安心】只在安安（`AnanlinPeaceOfMindPower`） |
| 9 | `HextechBrainwashResonance` | Prismatic | **仅安安** | 【洗脑】/【缄默】/【洗脑反噬】整条链只在安安 |

- ✅ **品级：9 条一律 `"Prismatic"`（用户 2026-09-29「这六个遗物都是棱彩品质」+ 追加 3 条时答复「也用棱彩」）**，
  落地为常量 `HextechRuneCatalog.RuneRarity`（原先 1/4/5 = Gold、6 = Silver）。
  ⚠️ **这个「棱彩」是海克斯自己的三档品级（字符串），不是原版稀有度** ——
  原版 `RelicRarity` 枚举里**没有 Prismatic**（`None/Starter/Common/Uncommon/Rare/Shop/Event/Ancient`），
  而 `Starter` 是海克斯《符文类的要求》写死的（保证原版别的抽取路径抽不到）⇒ **两个维度都别串**。
- ⚠️ **#1 / #3 定成「仅雪莉琳」是用户点名裁定的**，不要再按「机制通用性」自行放宽成 `null`。
  这一条也是**授权范围铁律**的实例：数据表这类「我给建议、用户裁决」的字段，以用户裁决为准。
- 品级只是掉率档位，**随时可改**（现在 6 条共用常量 `HextechRuneCatalog.RuneRarity`，改一处即可）。
- **用 `isAvailableForPlayer` 按角色收窄，而不是 `characterPool`** —— 对方枚举里只有原版 5 个角色、没有本 mod 角色（§5-1）。
  副作用是**把给作者的 R1 请求降到了最低优先级**：我们已经有可行的绕法，只是界面分组体现不出来。
- 委托是**确定性纯函数**（只读 `player.Character`）⇒ 满足 §0-9 的联机要求。

**界面文字**：新增本地化表 `ManosabaLin/localization/{zhs,eng,jpn,kor,rus}/relic_collection.json`，
各 3 键：`HEXTECH_POOL.MANOSABA_LIN`（来源标签）/ `HEXTECH_TAG.MANOSABA_LIN`（海克斯标签）/
`MANOSABA_LIN_HEXTECH_SECTION`（配置菜单小标题）。
⚠️ 表名必须是 `relic_collection`（游戏只合并与原版同名的表）；新表按技能规范为**无 BOM + LF + 2 空格 + 末尾换行**。

**⚠️ 本机无法验证「装了」分支**：2026-09-28 检查 `mods/` 里**没有 `HextechRunes`**（只有 ManosabaLin / MinionLib / RitsuLib / TestTheSpire）。
`.local/hex_src/` 那份反编译也**早于本指南**（`HextechRunesInterop` 只有 `RegisterExtraActProvider`，
`RegisterPlayerRune` 还在硬依赖的 `HextechRunesApi` 里，且要求继承 `HextechRelicBase`）。
⇒ 本次实现**只保证**：未装时零副作用、装了且版本够新时按指南注册、版本旧/能力缺失时**逐项降级并打日志**（不会崩）。

**验收（已实测）**：产物 DLL 里 `HextechRunes` 在 **`#Strings`(UTF-8) 命中 0 次**、只在 **`#US`(UTF-16) 命中 12 次**
⇒ 「零编译期引用」成立（§3-1 的判据）。

### 2.10 安安专属 3 个联动遗物：已落地（2026-09-29，`-t:Compile` 0 错误）

用户原话（一字不改）：

> 「写三个安安专属的海克斯联动遗物。第一个是你的书页（包括留白书页和空白书页）会再增加一个可以免费打出一次的无色选项，
> 第二个是安心会按照层数给予额外效果，打出攻击牌获得当前安心层数活力，技能牌获得当前安心层数格挡，能力牌消耗当前所有安心获得等量留白书页然后获得3层安心
> 第三个是洗脑和缄默共用缄默的可成长替换池并且洗脑每场战斗第一次不获得洗脑反噬」

**三个新文件**（与其余 6 个同目录 `ManosabaLinCode/Characters/Common/LinRelics/`，全部
`ManosabaRelicTemplate` + `[RegisterRelic(typeof(LinRelicPool))]` + `Rarity => RelicRarity.Starter`）：

| # | 类 | 效果 | 关键落点 |
| --- | --- | --- | --- |
| 7 | `HextechColorlessPage` | 书页的选项网格**多一个**「本回合可免费打出的无色牌」选项 | `AppendColorlessOption(player, options)`，由 `AnansSketchbook` 三个书页入口调用 |
| 8 | `HextechPeaceOfMindEcho` | 【安心】按层数追加上限：攻击→等量活力／技能→等量格挡／能力→消耗全部安心换等量留白书页再补 3 层 | `AfterCardPlayed` |
| 9 | `HextechBrainwashResonance` | 【洗脑】吃【缄默】的可成长替换池；每场战斗第一次洗脑不获得【洗脑反噬】 | `AnanlinSilenceIntentManager.ForceBrainwashAndGetTargets` 的 `baseBonus` + `AnanlinBrainwashPower.OnRightClick` |

#### 7. `HextechColorlessPage`（无色书页）

- 追加点 = `AnansSketchbook.UseBlankPage` / `UseMarginPage` / `ResolveBorrowedMarginPage` 三处，
  **放在各自的升级循环之后、`CardSelectCmd` 之前**。
  ⚠️ 顺序是有意的：这样追加的那张无色牌**不会**跟着升级版书页一起被 `CardCmd.Upgrade`。
- ⚠️ 为了让「升级后再追加」可行，把两个私有方法 `RollMarginPageOptions` / `RollBorrowedMarginOptions` 的返回类型
  从 `IReadOnlyList<CardModel>` **收窄为 `List<CardModel>`**（纯内部签名、无行为变化），
  这样 `options` 的静态类型才能匹配 `AppendColorlessOption(Player, List<CardModel>)`。
- 取牌：`ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)`
  → 过滤 `CanBeGeneratedInCombat` + 排除 `Basic/Ancient/Event/Token/Status/Curse/Quest`
  → `player.RunState.Rng.CombatCardGeneration.NextItem(...)`（**联机铁律：一切随机必须走 `RunState.Rng`**）。
- 「免费打出一次」= `CardModel.SetToFreeThisTurn()`（原版 `Discovery`/`BulletTime` 用的就是这个）。
- ⚠️ 遗留行为：书页在 `recordedPools.Length == 0` 时**仍会提前 `return []`**（在原逻辑之前）⇒
  素描本一张卡池都没记录时，书页依旧什么都不给（追加点在那之后）。这是**刻意保留原判据**，没动。

#### 8. `HextechPeaceOfMindEcho`（安心回响）

- `AfterCardPlayed(PlayerChoiceContext, CardPlay)`，先 `if (cardPlay.Card.Owner != Owner) return;`，
  再在**这一瞬间**读 `Owner.Creature.GetPower<AnanlinPeaceOfMindPower>()?.Amount` = 「当前安心层数」。
- 攻击（`CardType.Attack`，层数 > 0）：`PowerCmd.Apply<VigorPower>(..., stacks, ...)`。
- 技能（`CardType.Skill`，层数 > 0）：`CreatureCmd.GainBlock(Owner.Creature, stacks, ValueProp.Move, cardPlay)`
  （`ValueProp.Move` 与 `AnanlinSealedPagePower` 那种「能力给格挡」的写法一致）。
- 能力（`CardType.Power`）：**走现成工具 `AnanlinCardHelpers.LosePeaceOfMind`**
  ⭐ 用户 2026-09-29 裁定：「能力牌消耗当前所有安心时**要**触发安心原有的『一次性失去 ≥2 层 → 选择一张手牌获得【重放1】』」
  ⇒ 所以不能用裸 `PowerCmd.ModifyAmount`，必须走这个 helper（它内部 ≥2 层会弹 `CardSelectCmd.FromSimpleGrid`）。
  然后 `AddMarginPagesToHand(consumed)` 加等量【留白书页】，最后
  `PowerCmd.Apply<AnanlinPeaceOfMindPower>(..., 3, ...)` 补回 3 层（上限仍由能力的 `MaxStacks = 3` 兜底）。
- ⚠️ **按字面执行**：能力牌那条**无条件**先消耗（有几层耗几层）⇒ 安心为 0 时也照常补到 3 层。
  若这不是想要的语义，只需给 `case CardType.Power` 加一个 `when stacks > 0`。

#### 9. `HextechBrainwashResonance`（洗脑共鸣）

- ⭐ **共用可成长替换池**：「可成长替换池」= `AnanlinSilenceIntentManager` 的
  `SilenceGrowthByPlayer`（`GetSilenceGrowth(owner)`，每次**缄默**替换意图后 +1）。
  默认洗脑走的是写死的 `baseBonus: 0`（注释原话「不随缄默成长」）；
  持有本遗物后改为 `GetSilenceGrowth(owner)` ⇒ 洗脑与缄默**吃同一个池子**。
- ⭐ 用户 2026-09-29 裁定原话：「**现在缄默的池子就是可成长替换池，遗物只是让洗脑也能吃这个加成**」
  ⇒ 最初实现为「洗脑只读取、不推进」。
- ⭐ **2026-10-01 用户（海克斯联动第 5 条）改判**：洗脑不仅要**读取**、还要**推进**同一个通用意图池 ——
  「通用意图池并且都能强化」。现 `ForceBrainwashAndGetTargets` 末尾**补上了** `SilenceGrowthByPlayer[...] + 1`
  （仅当 `HextechBrainwashResonance.IsActiveFor(owner)` 时），与缄默 `TriggerAndGetTargets` 末尾一致。
  ⇒ 缄默与洗脑**都能**让这个可成长池 +1。
- **每场战斗第一次免反噬**：`AnanlinBrainwashPower.OnRightClick` 里那次
  `PowerCmd.Apply<AnanlinBrainwashBacklashPower>` 外面包一层
  `if (!HextechBrainwashResonance.TryWaiveBrainwashBacklash(Owner.Player)) { ... }`。
  - 位置语义正确：该处只在**真正改写成功**、且**不是「无援助」路径**时才会执行
    （前面已有 `if (rewrittenTargets.Count == 0 && !useNoahAssist) return;` + `if (useNoahAssist) { …; return; }`）
    ⇒ 「第一次**能获得**反噬的机会」才消耗豁免，失败尝试不吃掉它。
  - 标记 `[SavedProperty] public bool BrainwashBacklashWaivedThisCombat`（**联机同步**），
    在 `BeforeCombatStart()` 重置。
  - ⚠️ 只跳过**反噬**；那 25 层【魔女化】照常给。

**本地化**：5 语言 `relics.json` 各 +9 键（`…_COLORLESS_PAGE` / `…_PEACE_OF_MIND_ECHO` / `…_BRAINWASH_RESONANCE` × `.title/.description/.flavor`）。
⚠️ 表与其余海克斯遗物同表（`relics`），插入时**逐文件保持原有 BOM=True + 纯 CRLF**
（用脚本「末条补逗号 + 插行」，绝不 `json.dump` 整表重写）；插入后 5 个文件均 `json.loads` 通过。

### 2.11 2026-10-01 修复（用户本轮 7 项诉求的落地）

1. **牌库清道夫**：`AfterPlayerTurnStart` → `BeforeHandDraw`（自动抽牌**前**触发，先清道夫后抽牌）。
2. **魔女化核心**：删掉旧版「>300 降到 100 + 25:1 转计数」整条 `AfterPlayerTurnStart`，只留
   「保留 → +1 计数 → +10 层【魔女化】」这一件事（见 §2.8 遗物 3 更新）。
3. **情绪溢流持续性 UI**：`EmotionHangDisplay.CardScale` 0.5 → **0.25**（原本 1/4）；`CanShow()` 不再用
   `GetCurrentScreen() is NCombatRoom`（切牌组等覆盖层会误隐藏），改为「生物节点在场景树可见即显示」；
   `_Process` 兜底 `Visible = true`（`Rebuild` 签名不变时提前 return 不再卡在隐藏态）。
4. **厌恶/骇厌/延迟形失效 + 挤球吞新卡**：`HextechOrbEvokeRules` 由单槽静态字段改
   `ConditionalWeakTable<OrbModel, object>`（引用语义、多球不互踩、未命中不清空、命中才消费）；
   `HextechOrbOverflowPatch` 只给 `EmotionOverflowRules.HasOverflowPayout` 的球打标记（非情绪球/魔女化球/
   无遗物时的持续型球不再占用/残留标记）。
5. **洗脑共鸣**：`ForceBrainwashAndGetTargets` 末尾补 `SilenceGrowthByPlayer[...] + 1`（仅遗物在场时）⇒
   洗脑与缄默**共用同一通用意图池、都能推进**。
6. **洗脑/洗脑反噬中文显示英文**：`zhs/powers.json` 缺两个 `.smartDescription` 键（eng/jpn/kor/rus 都有），
   补上 `MANOSABA_LIN_POWER_ANANLIN_BRAINWASH_POWER.smartDescription` 与 `..._BACKLASH_POWER.smartDescription`。
7. **海克斯每层必出专属联动符文**：⛔ **无公开能力、做不了**——候选生成链（`ResolveActRoll`→`PickWeightedDistinct`）
   无「保底槽」插入点；`RegisterChaosTransform` 只跑 Mayhem 且 `IsHextechRelic` 拒绝外部符文。按红线（§0-1）不 patch
   对方 internal 类 ⇒ **已写进配对文件 `hextech-compat-hextech.md` 的 R4 需求**（`RegisterGuaranteedPlayerRune` 等三种方案），
   等海克斯作者开放能力后再落地。

---

### 2.12 艾玛专属 3 个联动遗物：已落地（2026-10-01，`publish` EXIT=0，产物已核验）

用户口述三条效果 + 四项裁决（① 「同等效果的组件」按现有组件范式；② 位移 = **累计偏移、长期停留**；
③ 触发粒度 = **每次变化事件一次**；④ 多人卡 = `CardMultiplayerConstraint.MultiplayerOnly`，
移除来源 = 手牌 + 抽牌堆 + 弃牌堆）。`HextechRuneCatalog.All` 追加 3 条 `typeof(Emalin)` 的 spec（注册桥未改，数据驱动）。

#### 10. `HextechWitchFactorErosion`（魔女因子侵蚀）

敌方阵营回合开始时，每个带 `EmaWitchFactorPower` 的敌人按**自身层数**受等量伤害。

- 触发点 `AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)`，判 `side != Owner.Creature.Side`
  （多人下队友与自己同侧 ⇒ 队友回合不会误触发）。
- 伤害走 `CreatureCmd.Damage(choiceContext, enemy, stacks, ValueProp.Unpowered, null, null)`（可被格挡、不吃力量）。

#### 11. `HextechTrialEmbodiment`（审判具现）

让持有者的卡**可以同时拥有多个不同名的附魔**。

- ⭐ **2026-10-02 用户裁决改为「全部转换」**（推翻此前的「第 1 个附魔留在卡上作代表附魔」）：
  卡上获得的附魔**全部**具现进卡里（`EnchantmentEmbodimentComponent`）⇒
  卡牌附魔槽的**真槽位**（`<Enchantment>k__BackingField`）**恒为空** ⇒
  **卡面上不再使用引擎的附魔显示框**，且同一张牌还能继续被附魔。
- ⭐⭐ **2026-10-02 第二轮（用户第二次数落：「审判具现的行为只由遗物自己实现，与艾玛有 p 关系，
  而且你这样艾玛用也没用」）⇒ 读取侧改为「属性级读取桥」**：
  给 `CardModel.get_Enchantment` 打 **Postfix**（`Patches/HextechTrialEmbodimentBridge.cs`），
  真槽位为空而卡里有具现附魔时**返回第 1 条具现附魔**（**已 `ApplyInternal` 绑回本卡**）。
  ⇒ 全游戏任何读 `card.Enchantment` 的代码（**引擎自己 / 任何角色 / 任何第三方模组**）都无需改动，
  **不再需要逐个去改读取方**。此前「改艾玛 25 处 + 其它角色 6 处」的做法**已全部回滚**
  （艾玛 19 个文件用 `git checkout` 精确还原，`Data.cs` 只退掉附魔那两句、保留便签条升级改动）。
- ⭐ **为什么桥只交出一条**：`card.Enchantment` 是**单值**属性，引擎的数值 / 打出 / 打出次数 / 悬浮
  也都只读它一次 ⇒ 第 1 条由引擎经桥自己结算，**第 2 条起**才由遗物补算：
  - 数值：`Patches/EnchantmentReadPatches.cs` 走 `EffectiveEnchantments.BeyondPrimary(card)`；
  - 打出：组件 `OnPlayPostfix`；打出次数：组件 `ModifyCardPlayCount`；悬浮：组件 `HoverTips`（跳过第 1 条，否则重复）。
- ⭐ **桥必须被临时关掉、只看真槽位的 5 处**（`EmbodiedEnchantmentBridge.Enter/Exit`，Prefix 进 Postfix 出、嵌套安全）：
  `CardModel.ToSerializable`（否则具现附魔会**写进存档的真槽位**）、
  `CardModel.DeepCloneFields`（否则克隆体会**同时**有真附魔 + 具现附魔）、
  `CardModel.DowngradeInternal`（它调 `Enchantment?.ModifyCard()`，未绑卡的具现实例会抛）、
  `EnchantmentModel.CanEnchant`（引擎「一卡一附魔」判定点，必须看到真槽位为空）。
  同类「必须看真槽位」的语义一律走 `EffectiveEnchantments.Raw(card)`（backing field 的 `FieldRefAccess`，
  已实测 `sts2.dll` 里存在 `<Enchantment>k__BackingField` 这个字段名）。
- ⭐ **写回 `Amount`**：桥交出的是**临时实例**，而引擎 / 卡牌会直接写 `card.Enchantment.Amount`
  （艾玛审判徽章的计数回写 `card.Enchantment.Amount = _agreeCount`、原版 `Goopy` 的 `Amount++`）⇒
  `EnchantmentModel.set_Amount` 后置补丁把写入落回组件 `SavedCards`（组件 `WriteBackAmount`），
  否则「和真挂在卡上一模一样」是假象。
- ⭐ **卡面表现**：桥接了之后引擎会把附魔标签页显示出来 ⇒ `NCard.UpdateEnchantmentVisuals` 后置补丁把
  **具现实例**的标签页藏掉；`EnchantmentModel.get_DynamicExtraCardText` 后置补丁把具现实例的额外文字掐成 null
  （这两样都属于用户明确不要的「正常附魔显示框」）。
- ⭐ **`CardCmd.ClearEnchantment` 也要能清掉具现的**：`CardModel.ClearEnchantmentInternal` 后置补丁 →
  组件 `ClearAll()` + 刷新卡面（否则「清旧附魔」会变成空操作、卡面名字还留着）。
- 卡面的替代显示（用户原话「转换过的附魔在卡牌上面显示附魔名字就行了，然后自带附魔效果悬浮框」）：
  - **名字**：补丁 `CardModel.GetDescriptionForPile`（私有实现重载）把具现附魔的
    `enchantments` 表 `<Id>.title` 拼到描述末尾（`[purple]【A】【B】[/purple]`）；
  - **效果**：组件的 `HoverTips`（每个具现附魔一条 `HoverTip`）。
- ⚠️ **代价：引擎的附魔数值加成只会算到「第 1 条」**（它读单值的 `card.Enchantment`，值由读取桥交出）——
  其余条数必须由遗物补算；这些读取点是：

  | 引擎读取点 | 作用 |
  |---|---|
  | `Hook.ModifyDamage`（`Hook.cs:1503`）/ `Hook.ModifyBlock`（`:1329`） | **实际战斗**的伤害 / 格挡加成 |
  | `BlockVar` / `CalculatedBlockVar` / `DamageVar` / `CalculatedDamageVar` / `ExtraDamageVar` / `OstyDamageVar` 的 `UpdateCardPreview` | 卡面数字预览 |
  | `CardModel.GetDescriptionForPile`（`:1405` `Enchantment?.DynamicExtraCardText`） | 卡面附魔文字 |

  接法见 `Patches/EnchantmentReadPatches.cs`：**Hook 用 Prefix 注入形参**（在引擎 `num = damage` 之前，
  ⇒ 实际战斗数值**精确**等价于「附魔还挂在卡上」）；**DynamicVar 用 Postfix 补算** PreviewValue/EnchantedValue
  （加法/乘法口径与引擎一致，只是排在 hooks 之后 ⇒ 属**显示值**，可接受）。
  ⚠️ `ExtraDamageVar` 没有 `Props`（引擎对它硬编码 `ValueProp.Move`），且**只吃乘算** ⇒ 单独处理。
- ⭐ **读取侧由「属性层面的桥」统一解决，不再动任何 mod 业务代码** —— 因此艾玛自己的那批判定
  （`EmalinCombatHelper` 的 5 个统计、`FinalJudgment`、`EmaForgottenOne`、`Emadeath`、`SmallKey`、`Data`、
  `StabbingBlade`、`Emamonv`、`Emamlym`、`Witchfactorechantcard`、`EnchantTransform`、`Trialenchantcyclepower`、
  `Randomtrialenchantpower`、`EnchantmentConvergencePower`、`JointJudgmentPower`、`PrisonBlueprintPower`、
  `TrueCriminalPower`、`WitchTrialPower`、`EmaTrialBadge`）**全部保持原样**，靠桥就能正确工作：
  - `card.Enchantment is Rebuttal or Agreement or Doubt` 之类的**类型判定**现在能命中（桥交出第 1 条）；
  - `card.Enchantment.Amount = n` 之类的**写入**经 `set_Amount` 补丁落回卡里（见上）；
  - `card.Enchantment != null` / `== null` 之类的**有无判定**也正确（桥只在卡内确有具现附魔时才交出值）。
  ⚠️ **唯一的语义上限**：属性是单值的 ⇒ 那些代码**只看得见第 1 条**具现附魔。一张牌同时挂多条**审判**
  附魔时，`WitchTrialPower` 之类的"逐条计数"只会数到第 1 条。这是「不改读取方」这条约束的必然代价，
  不是 bug（要突破就得回到"逐个改读取方"，已被用户否决）。

**统一读取器** `Extensions/EffectiveEnchantments.cs`：
`Of`（真附魔 + 具现附魔）/ `BeyondPrimary`（引擎不管的那部分 = 第 2 条起）/ `Raw`（真槽位）/
`Has` / `Has<T>` / `Count` / `OfTrial` / `FindComponent` / `MarkEmbodied`（登记桥接实例）/
`IsEmbodiedInstance`。没装遗物时退化为引擎原行为（零行为变化）。

**补丁点**：`Patches/HextechTrialEmbodimentPatch.cs` → `CardCmd.Enchant(EnchantmentModel, CardModel, decimal)` 的 **Prefix**
（泛型 `Enchant<T>` 内部转调它 ⇒ 只打这一处）。**必须前置** —— 原逻辑的 `CanEnchant` 会先把第二格拒掉，Postfix 太晚。

**补丁点 2**：`EnchantmentModel.CanEnchant` 的 **Postfix**（`HextechTrialEmbodimentCanEnchantPatch`）。
- 起因（2026-10-02 用户实测反馈）：事件「自助指南」（`SelfHelpBook`，读封底/读段落/读全书 = 给攻击/技能/能力牌附
  Sharp/Nimble/Swift +2）**选不到已附魔的卡**。根因 = 选牌走 `CardSelectCmd.FromDeckForEnchantment`
  用 `enchantment.CanEnchant(c)` 过滤（`CardSelectCmd.cs:660`），而 `CanEnchant` 末段
  「`card.Enchantment != null` && (!IsStackable ‖ 类型不同) ⇒ false」（`EnchantmentModel.cs:289`）
  **就是「一卡一附魔」的判定点** ⇒ 卡在**过滤阶段**就被筛掉，根本走不到 `CardCmd.Enchant`。
- ⚠️ **加上读取桥之后**：`card.Enchantment` 会返回第 1 条具现附魔 ⇒ 这个判定点会**无条件拒绝**已经具现过附魔的卡。
  所以本补丁改成 **Prefix 关桥 + Postfix 判同名**：关桥后引擎看到「真槽位为空」自然放行；
  Postfix 只剩拦「同名」（卡内已具现过同名 ⇒ 拒绝，保持「同名只能一个、不堆叠」）。另外兼容旧存档：
  真槽位还残留附魔时也放行（第 2 条会被具现进卡里）。
- ⚠️ 子类 override 的 `CanEnchant`（如 `Nimble` = `base.CanEnchant(card) && card.GainsBlock`）内部都调 `base`，
  所以打在基类上有效，**各附魔自己的附加条件仍然生效**。

**拦截逻辑**（`HextechTrialEmbodiment.TryEmbodify`）：

| 情形 | 处理 |
|---|---|
| 该玩家没装遗物 | **不拦截** ⇒ 照常挂在卡上（引擎原行为，零影响） |
| 卡上残余的真附魔与它同名（旧存档） | 直接丢弃（不堆叠），返回 `null` |
| 卡内已具现的某条同名 | 直接丢弃，返回 `null` |
| 其余（**含第 1 条**） | 全部封进卡里那张「只带附魔的卡」 |

**组件** `Characters/Ema/Components/EnchantmentEmbodimentComponent.cs`
（⚠️ 只是**文件放在** `Characters/Ema/Components/` 下 —— 它属于审判具现，与艾玛角色本身无关；
当初是跟着「艾玛专属 3 遗物」一起建的）：

- `[ComponentState] List<SerializableCard> SavedCards` —— 每条 = 一张载体卡
  （`Id` 取主卡自己、`Enchantment` 挂附魔），形态与 `GenerateComponent` 的「内嵌一张卡」同构。
- `GetEmbodiedEnchantments()` **带缓存**（`_embodied`）——
  ⚠️ **必须有**：读取器被挂在**每次伤害 / 格挡计算**的热点路径上（`Hook.ModifyDamage` 等），
  每次 `FromSerializable` 重建模型会明显掉帧。
- `GetBridgeEnchantment()` —— 交给读取桥的**第 1 条**，且**已 `ApplyInternal` 绑回本卡**
  （引擎处处假定 `EnchantmentModel.Card` 非空，例如 `Adroit.OnPlay` 要用它取 Owner）；单独缓存 `_boundBridge`。
- `WriteBackAmount(instance, amount)` —— 由 `EnchantmentModel.set_Amount` 后置补丁调用，把对桥接实例的写入落回 `SavedCards`。
- `InvalidateCaches()` —— `SavedCards` 一有变化就把三个缓存（`_hoverTips` / `_embodied` / `_boundBridge`）全部作废。
- `OnPlayPostfix`：只遍历 `EffectiveEnchantments.BeyondPrimary(card)`（**第 2 条起**），
  逐个 `ApplyInternal` + **借附魔本体跑同一份 `OnPlay`** + `finally ClearInternal()`
  —— 第 1 条已由引擎那句 `Enchantment.OnPlay` 经桥结算，重复处理会**打两次**。
- `ModifyCardPlayCount`：同样只补第 2 条起的 `EnchantPlayCount`
  （引擎的 `GetEnchantedReplayCount`（`CardModel.cs:1132`）经桥已经算过第 1 条）。
- `HoverTips` = 「【附魔具现】」标题 tip + 第 2 条起每个具现附魔的 `HoverTip`
  （第 1 条由引擎经桥给出，再列一次就重复；经 `ComponentsCardModel.ExtraHoverTips` 并入卡牌悬浮；
  包 try-catch 防对方模组卸载时炸）。
- `SyncTrialAmounts(agreement, rebuttal, doubt)` —— 直接改 `SavedCards` 里那条
  `SerializableEnchantment.Amount` 的入口（艾玛侧的 `EmaTrialBadge` **没有**调用它，
  那边走的是「写 `card.Enchantment.Amount` + `set_Amount` 写回」这条通用路径）。
- `ClearAll()` —— 由 `CardModel.ClearEnchantmentInternal` 的后置补丁调用。
- `TryMergeWith` 按 `Enchantment.Id.Entry` 去重（**同名只能一个、不堆叠**）。

**本地化键**：遗物 `MANOSABA_LIN_RELIC_HEXTECH_TRIAL_EMBODIMENT.*`；组件 `cards` 表
`ManosabaLin.EnchantmentEmbodimentComponent.hovertip.title / .description`。

> ⭐⭐ **铁律（2026-10-02 用户两次裁决；第二次原话：「审判具现的行为只由遗物自己实现，与艾玛有 p 关系，
> 而且你这样艾玛用也没用」）**：
> **本遗物的行为只能在它自己的代码里实现 —— 一处都不去改其它 mod 业务代码。** 属于它的文件只有这 6 个：
> `Characters/Common/LinRelics/HextechTrialEmbodiment.cs`、
> `Characters/Ema/Components/EnchantmentEmbodimentComponent.cs`（组件；⚠️ 只是**文件放在** `Characters/Ema/` 下，
> 它属于审判具现、与艾玛角色无关）、`Patches/HextechTrialEmbodimentPatch.cs`、
> `Patches/HextechTrialEmbodimentBridge.cs`、`Patches/EnchantmentReadPatches.cs`、
> `Extensions/EffectiveEnchantments.cs`。
>
> - ⚠️ **艾玛自己的卡与 Power（`Characters/Ema/**`）也不算「遗物自己的代码」**（2026-10-02 第二轮裁决）：
>   第一轮曾把艾玛 25 处 + 其它角色 6 处（`Mlym`/`YalisalinMlym`/`AnanlinMlym`/`SherrylinMlym` 的
>   `CanBeExchanged`、`Bloodiedclothing` 的技能牌筛选、`AnansSketchbook.CopyVisibleAdditions`）
>   改成 `EffectiveEnchantments` —— **全部被否决并回滚**。
>   读取侧现在靠**属性层面的桥**（`CardModel.get_Enchantment`）统一解决 ⇒ 那些文件**不需要也不允许**再改。
> - ✅ 允许的是**给引擎方法打补丁**（`CardModel` / `EnchantmentModel` / `NCard` / `Hook`）——
>   那正是「遗物自己完成的适配」，且只 patch 引擎，不碰任何 mod 业务代码。
> - ⚠️ 这条约束的必然代价：`card.Enchantment` 是**单值**属性 ⇒ 别的代码只看得见**第 1 条**具现附魔。
>   要突破就得回到"逐个改读取方"，已被用户否决 ⇒ 不再尝试。

#### 12. `HextechBondDrift`（羁绊漂移）

亲近/疏远变化 ⇒ 模型产生**累计、长期停留**的左右偏移；左移给队友多人卡，右移移除一张并给减费。

- **变化观测**（`Patches/HextechBondDriftPatches.cs`）：patch `BondPower.set_Affinity` / `set_Estrangement`。
  ⚠️ 不能借 `Yalisabond.ApplyBondDeltaAsync` —— 两个 setter 只在 `delta > 0` 时才调它，**减少时没有任何回调**。
- **防读档误触发**：`SavedProperties.FillInternal` 用 `PropertyInfo.SetValue` ⇒ 恢复会走 setter。
  所以基线（`LastAffinity` / `LastEstrangement` + 两侧各一个 `*BaselineSet`）**随遗物 `[SavedProperty]` 存档** ⇒
  恢复后 delta = 0；新遗物首次观察只对齐基线不触发。
- **位移**：改 `NCreature.Position`（`Visuals.Position` 会被受击抖动 `AnimShake` 清零）。
  因 `NCombatRoom.PositionPlayersAndPets`（static）每次重排都会覆盖，故 patch 它的 Postfix 把**绝对偏移**叠回去；
  事件发生时用**增量**叠加 ⇒ 两者自洽。偏移 = `Estrangement − Affinity`，`StepPerPoint = 40f`（左 = 负 x）。
  ⚠️ 2026-10-02 实测反馈「移动太不明显，只看得出动了一点」⇒ 由 `12f` 调到 `40f`（12f 时 1 点只有 12px，1920 宽屏上看不出来）。
- **左移**（`HextechBondDriftEffects.OnShiftedLeft`）：每个队友各拿到一张其卡池的 MultiplayerOnly 卡
  （`RunState.Rng.CombatCardGeneration` + `combatState.CreateCard` + `CardPileCmd.AddGeneratedCardToCombat`）。
- **右移**（`OnShiftedRight`）：从所有队友三堆里随机移除一张 MultiplayerOnly（`RunState.Rng.CombatCardSelection`
  + `CardPileCmd.RemoveFromCombat`），成功后给其主人挂 1 层 `HextechMultiplayerDiscountPower`
  （Counter 可叠加；`TryModifyEnergyCostInCombat` 只对 MultiplayerOnly 生效；`BeforeCardPlayed` 打出即整层移除）。

> 本地化：5 语言 × `relics.json`（3 遗物 × title/description/flavor）、`powers.json`（减费能力）、
> `cards.json`（组件 hovertip）；80 个 localization JSON 全部 `json.loads(utf-8-sig)` 通过。
> 产物核验：DLL 内 10 个新类型名全部命中；PCK 内抽样键各命中 5 次（5 语言）。

---

## 4. 已否决路线（记录理由，避免回头重走）

| 路线 | 否决理由 |
| --- | --- |
| **反射 + Harmony 窄 patch `HextechCatalog` 的 ~18 个静态方法**（本文档上一版的主方案） | ⛔ **官方指南明令禁止**（§0-1）。这条路线从「唯一可行」变成「违规」。 |
| 建可选子程序集引用 `HextechRunes.dll`，符文继承 `HextechRelicBase`，调官方 `RegisterPlayerRune<T>` | ① 仍是**硬引用**；② 版本绑定，对方改 API 就要重编；③ 对方没装时对加载时序要求苛刻（引擎与 `HextechCatalog` 都会 `assembly.GetTypes()`，基类缺失即 `TypeLoadException`）。**软依赖非泛型重载已完全覆盖需求，不需要它。** |
| 用 `[ModInterop("HextechRunes", …)]` 做主要接入 | 与指南「按程序集名检测、不要按 manifest id」冲突（`ModInteropAttributes.cs:18` 按 manifest id 解析）。仅作为可选补充。 |
| 我们主动 `ModHelper.AddModelToPool(typeof(SharedRelicPool), 我方符文)` | 海克斯注册时会自己加；我们重复加只会让符文更早暴露在原版其他抽取路径下。 |

---

## 5. 与作者的落差（→ 同步进配对文件）

1. **`characterPool` 没有 mod 角色。** 对方枚举只有 `Ironclad / Silent / Regent / Defect / Necrobinder`，
   我们的 5 个角色（艾玛 / 亚里沙 / 希罗 / 杏奈 / 雪莉）**只能落进「通用」**。
   绕过：`isAvailableForPlayer` 里按 `player.Character` 过滤（能实现效果，但**界面分组体现不出来**）。
   ⇒ 已写进配对文件的需求。
2. **软依赖面（`ApiVersion == 1`）只有 4 个成员**：
   `RegisterPlayerRune` / `SetPlayerRunePoolLabel` / `RegisterConfigSectionTitle` / `RegisterExtraActProvider`。
   我们想在软依赖下做**锻造器 / 事件遗物 / 附魔图标 / `RelicBundleGrantHelper.GrantRelics`** 都**不够用**，
   只能硬依赖。⇒ 已写进配对文件的需求。
3. **`HextechNaturalRelicPoolHooks` 的过滤是类型判定**（`!(relic is HextechRelicBase)`）⇒ 外部符文
   （不继承基类）**不会**被它滤掉，官方靠「必须 `Starter`」兜底。若我们不慎给某个符文标了非 Starter，
   它就会被**原版自然遗物奖励**随机发出。⇒ 我们的硬约束：**所有海克斯符文一律 `RelicRarity.Starter`**。
4. **待实测**：
   ① 我方符文同时进 `LinRelicPool`（我方登记）与 `SharedRelicPool`（海克斯登记）时的**去重表现**
   （图鉴/遗物列表会不会出现两份）；
   ② `LocString("relic_collection", <缺失键>)` 的确切表现（指南说「显示键名、不报错」，实测确认）；
   ③ 重复注册时对方的警告文案（用来核对我们的幂等保护）；
   ④ **图鉴分区补丁**（`HextechCharacterGroupsPatch`）的实测：绑定成功后，我方 12 符文应从海克斯图鉴的
   「通用」档消失、出现在 `HEXTECH_CHARACTER.MANOSABA_LIN_*` 的 4 个专属档里；
   Godot 日志应无我们前缀的 Error（失败会自动降级回官方行为并报一次错）。
5. **待确认**：`ApiVersion` 是否随 `HextechRunesInterop` 新增能力而递增（指南承诺了，但当前仍是 1）；
   将来若它变 ≥ 2，我们要用「按需降级」而不是「整体拒绝」。
