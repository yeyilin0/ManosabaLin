---
name: manosabalin-create-card
description: Create or modify cards in the ManosabaLin Slay the Spire 2 mod. Use when adding a CardModel, editing card behavior, wiring card registration, card art, dynamic vars, keywords, upgrades, generated cards, card localization, or card pool membership under ManosabaLinCode/Characters.
---

# ManosabaLin Create Card

## Workflow

1. Read `AGENTS.local.md`; use the local STS2, MinionLib, and RitsuLib paths for API checks before guessing signatures.
2. Pick the owning character/pool:
   - Hiro: `HiroCardPool`
   - Ema/Emalin: `EmalinCardPool`
   - Sherrylin: `SherrylinCardPool`
   - Shared/token cards: usually `LinCardPool`
3. Put the class under the owning character folder, normally `ManosabaLinCode/Characters/<Character>/Cards/`.
4. Prefer `ManosabaCardTemplate`:

```csharp
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class ExampleCard()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.Value, ValueProp.Card, this);
    }
}
```

## Common Card Pieces

- Use `[RegisterCard(typeof(...Pool))]`; RitsuLib gives the public entry `MANOSABA_LIN_CARD_<TYPE_NAME>`.
- Use `protected override IEnumerable<DynamicVar> CanonicalVars` for text variables such as `DamageVar`, `BlockVar`, `CardsVar`, or project-specific vars.
- Use `CanonicalKeywords` for **创建即固定、此后不再变**的印刷关键字（如 `CardKeyword.Exhaust`）。
- Put upgrade logic in `OnUpgrade(ComponentContext componentContext)`.
  ⚠️ **升级授予的关键字必须在该处 `AddKeyword(...)` 实时写入**（原版先例 `Afterimage`：`OnUpgrade() { AddKeyword(CardKeyword.Innate); }`；同池 `SuperStrength` 同）。
  **不要**用 `CanonicalKeywords + IsUpgraded` 表达"升级后获得关键字"：`CardModel._keywords` 是**首次读取时**从 `CanonicalKeywords` 快照下来的实例状态（且随克隆传播，`DowngradeInternal` 才会重置）
  ⇒ 升级发生在任何一次关键字读取之后时会被快照吞掉（`FinalFarewell` 卡目前就是这种脆弱写法，尚未改动）。
- Use command APIs (`DamageCmd`, `CreatureCmd`, `PowerCmd`, `CardPileCmd`, `PlayerCmd`) instead of mutating runtime state directly.
- For generated combat cards, create mutable cards through `CombatState.CreateCard<T>(owner)`, `ICombatState.CreateCard(canonicalCard, owner)`, or `CardFactory.GetForCombat(...)`.
- Attach model capabilities with `card.GetOrCreateCapability<TCapability>()`; use `$manosabalin-create-capability` for capability details.

## Effect hover tips (卡浮层 `_EFFECT`)

卡面主描述由引擎渲染，但「浮层」是另一套：本地化键用 `<卡键>_EFFECT`（`.title` / `.description`），
由 `ManosabaLinCode/Characters/Common/CardEffectHoverTipFactory.cs` 生成：

```csharp
protected override IEnumerable<IHoverTip> AdditionalHoverTips
{
    get { yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry); }
}
```

- ⚠️ **`{IfUpgraded:show:A|B}` 在主描述里由引擎注入 `IfUpgradedVar`，浮层里没有那一步** ⇒ 必须由
  `CardEffectHoverTipFactory` 手动补（它已补了 `IfUpgradedVar` 与 `energyPrefix`），否则解析报错。
- ⭐ **要展示「别人的牌」的效果时**（如「火之时代」预览它生成的三张分支牌），用重载
  `FromCard<T>(locEntry)`（内部取 `ModelDb.Card<T>()` 规范模型）。
- ⚠️★ **预览「我拿到手时会是升级版」的牌时，绝不能直接用规范模型** —— 规范模型恒未升级，
  `{IfUpgraded:...}` 会走未升级分支、`DynamicVars` 也是旧数值。正解 = 自己拷一份可变副本并就地升级：

```csharp
var preview = ModelDb.Card<T>().ToMutable();   // 只拷贝，不注册进战斗
preview.UpgradeInternal();                     // = CardCmd.Upgrade 去掉特效的那两步
preview.FinalizeUpgradeInternal();
return CardEffectHoverTipFactory.FromCard(preview, locEntry);   // 该重载按 card.IsUpgraded 解析
```

- ⚠️★ **`AdditionalHoverTips` 会被 UI 反复求值** ⇒ **不要**在它里面用 `CombatState.CreateCard<T>(owner)`
  造预览牌（它内部 `AddCard` 会把牌注册进战斗状态，反复调用会不断堆积）。
  `CombatState.CreateCard` 只用于「真的要生成一张牌进战斗」的场合。建议外面套 `try/catch` 兜底。
- 先例：`AgeOfFire.cs`（火之时代，升级后分支牌浮层显示升级版）、
  `CardCmd.Upgrade` 的等价两步见 `AnanlinSaveLoad.cs` / `AnanlinUnfinishedDraft.cs`。

## Localization

Update all supported locales unless the user explicitly narrows the task:

- `ManosabaLin/localization/eng/cards.json`
- `ManosabaLin/localization/zhs/cards.json`
- `ManosabaLin/localization/jpn/cards.json`

Use the RitsuLib public entry:

```json
"MANOSABA_LIN_CARD_EXAMPLE_CARD.title": "Example",
"MANOSABA_LIN_CARD_EXAMPLE_CARD.description": "Gain {Block:diff()} Block.",
"MANOSABA_LIN_CARD_EXAMPLE_CARD.selectionScreenPrompt": "Choose a card"
```

Only add extra keys actually used by the code, such as `selectionScreenPrompt`, `selectionScreenPrompt2`, or custom `LocString` suffixes.

## Art

`ManosabaCardTemplate` resolves art from the class name lowercased:

- Big: `ManosabaLin/images/cards/big/<classname>.png`
- Small: `ManosabaLin/images/cards/<classname>.png`
- Beta: `ManosabaLin/images/cards/beta/<classname>.png`
- Fallback: `ManosabaLin/images/cards/card.png`

Do not edit generated `.uid` files.

## Card frame (装饰卡框)

The decorative character frame is applied by `ManosabaLinCode/Patches/CharacterCardFramePatch.cs`
(`NCard.Reload` postfix) and **its owner character is derived from the card class's namespace**, not its
physical folder and not its `CardPool`. Namespace segment after `Characters` ⇒ character
(`ManosabaLin.Characters.Hiro.Cards` ⇒ `Hiro`), then it looks for
`images/characters/{Character|Characterlin}/{stem}.png` where stem is `{character}`/`{character}lin` ×
`card.png`/`_card.png` (e.g. `Ema` ⇒ `Emalin/emalincard.png`, `Hiro` ⇒ `Hiro/hirolincard.png`).

- **Wrong frame on a card?** Almost always a copy-pasted `namespace` on the card file. Fix the namespace to
  match the owning character (physical folder) — that also fixes `AudioEventPathProvider.CardHomeName`.
- **One-off frame, same character folder** → add `[typeof(MyCard)] = "mystem"` to `ExclusiveFrameStems`.
- **Frame living in another character's folder** (e.g. a Sherry card class placed under `Hiro`) → add
  `[typeof(MyCard)] = ("Sherrylin", "sherrylincard")` to `CrossDirectoryFrames`.
- **No decorative frame at all** → add the type to `NoFrameCards` (the engine's own Ancient/etc. frame still shows).

## Checks

- Search for nearby cards in the same character before inventing patterns.
- Keep localization keys structurally aligned across `eng`, `zhs`, and `jpn`.
- Build with `dotnet build ManosabaLin.csproj` (never the `.sln`); deploy with `dotnet publish ManosabaLin.csproj`.
- If JSON changed, parse the touched localization files.
