# -*- coding: utf-8 -*-
import json, io, re

texts = []
with io.open(r'D:\ManosabaLin\uniq_powers.txt', encoding='utf-8-sig') as f:
    content = f.read()
blocks = re.split(r'### \[\d+\] ', content)
for b in blocks:
    if not b.strip():
        continue
    lines = b.split('\n')
    zh_lines = []
    for ln in lines:
        if ln.startswith('  keys:'):
            break
        zh_lines.append(ln)
    zh = '\n'.join(zh_lines).strip()
    if zh:
        texts.append(zh)
assert len(texts) == 316, len(texts)

eng_2 = [
    "When a card is played, a random enemy gains 3 times the stack count of [color=#ff99cc]【Witch Factor】[/color]",
    "Grand Investigation",
    "Great Witch's Power",
    "When [color=#CC6666]【Perjury】[/color] is converted to [color=#CC6666]【Justice】[/color], draw 1 [color=#CC6666]card[/color] and gain 1 Energy",
    "When you give a card [color=#CC6666]【Transmigration】[/color], automatically add [color=#ffcc66]【Truth】[/color] to it.\nWhen [color=#ffcc66]【Truth】[/color] is auto-played, the 1 Energy gained is nullified.\nFor every 3 cards with [color=#ffcc66]【Truth】[/color] components auto-played, gain 1 Energy and draw 2 [color=#CC6666]cards[/color].",
    "When you right-click or two-finger tap [color=#6666cc]【Silence】[/color] to make a monster act immediately, remove all [color=#6666cc]【Silence】[/color] and [color=#9999ff]【Lie】[/color]; gain {VigorPerLie:diff()} stacks of [b]Vigor[/b] per [color=#9999ff]【Lie】[/color] stack,\nand gain 1 stable multi-hit [b]Attack[/b] card costing 0, randomly generated from the pool recorded by [color=#6666cc]【Anan's Sketchbook】[/color], playable now.",
    "When you deal damage [color=#33ccff]this turn[/color], deal extra damage equal to the stack count",
    "When perjury returns to justice, new life is born",
    "When a target with this power takes damage from an [b]Attack card[/b], it takes extra damage equal to that damage value once more. Each Attack card removes at most 1 stack.",
    "After right-clicking or two-finger tapping to consume 13 stacks of [color=#6666cc]【Silence】[/color] to rewrite an intent, gain {Amount} Block and add 1 [color=#6666cc]【Blank Page】[/color] to your hand.\nIf the source card is upgraded, gain [color=#6666cc]【Blank Page+】[/color] instead.",
    "Countdown of Three",
    "Landmine",
    "Immune to the 2nd instance of HP loss from the same recorded intent, and remove this.",
    "1st acquisition: your current [color=#99ccff]【Reassurance】[/color] decreases by 1.\n2nd acquisition: add 1 [color=#6666cc]【Blank Page】[/color] to the draw pile.\n3rd acquisition: lose all [color=#99ccff]【Reassurance】[/color], gain {WithPower:diff()} stacks of [b]【Witchification】[/b], and 【Brainwash】 now requires consuming [color=#6666cc]【Silence】[/color] while this power is at 3+ stacks.\nCurrently 【Brainwash】 requires consuming {NextBrainwashSilenceCost:diff()} stacks of [color=#6666cc]【Silence】[/color]\nAfterwards, for every {SilenceTaxThreshold:diff()} stacks of [color=#6666cc]【Silence】[/color] you accumulate, lose [color=#6666cc]【Silence】[/color] equal to this power's stacks.",
    "Warden's Statue",
    "Click to copy 1 currently craftable [rainbow][b]【Complex Emotion】[/b][/rainbow] into [b]【Others' Emotions】[/b], or cancel. After choosing, consume the materials and 1 stack.",
    "Movie Promise",
    "Stack 5 to convert into 1 [color=#CC6666]【Justice】[/color]",
    "Stacked Crime",
    "Butterfly Talisman",
    "Discard all hand cards to gain 26 each",
    "Replay of Multiple Loops",
    "Malice's Power",
    "Nikaido Hiro's Magic",
    "Justice of the Second Loop",
    "Sealed Page",
    "Complex Emotion",
    "This enemy's intent has been forcibly induced into another intent of the same effect type",
    "Accomplice",
    "Conceiving Novelist",
    "Isolated",
    "Super Strength",
    "Strange Smell",
    "Mind-Cost Draw",
    "Kurobe Nao's Magic",
    "Synced Breathing",
    "Butterfly",
    "Illusion",
    "Sudden Realization",
    "Lie",
    "At end of turn, if the number of [color=gray][Transformation Magic][/color] in your hand is the highest among all players, lose HP equal to the total [color=gray][Transformation Magic][/color] in all players' hands.",
    "At end of turn, randomly play 1/3 of your current hand.",
    "Removed at end of turn",
    "Removed at end of turn",
    "At start of turn, give all enemies 10 stacks of [color=#ff99cc]【Witch Factor】[/color]",
    "At start of turn, gain 1 「Shockwave Fist Wind」.",
    "At start of turn, lose Energy equal to this power's stacks",
    "At start of turn, if the consume pile contains 「Lion's Mane Jellyfish」, move it to your hand and play it automatically.",
    "At start of turn, all enemies gain 10 stacks of [color=#ff99cc]【Witch Factor】[/color], all allies gain 1.\nEnemies gain stacks equal to damage taken; allies gain a quarter of the stacks",
    "At start of turn, gain {energyPrefix:energyIcons(1)}",
    "At start of turn, gain emotion stacks.",
    "At start of turn, lose HP equal to stacks; when the owner is attacked, deal extra damage equal to half the stacks",
    "At start of turn, choose 1 hand card to exhaust and draw.",
    "At start of turn, choose 1 card from the draw pile to exhaust, gain temporary Strength equal to its cost, and gain {energyPrefix:energyIcons(1)}.",
    "At start of turn, randomly gain 【Left Fist】 or 【Right Fist】",
    "Bond",
    "Record [blue]damage taken this turn[/blue].\nAt [blue]start of turn[/blue], restore HP equal to the recorded value, then lose 1 stack",
    "Record the values of [color=#ff99cc][b]【Closeness】[/b][/color] and [color=#CC6666][b]【Estrangement】[/b][/color]\nWhen you play a [b]「Closeness」[/b] card, [color=#ff99cc]Closeness[/color] +1\nWhen you play a [b]「Estrangement」[/b] card, [color=#CC6666]【Estrangement】[/color] +1",
    "Record the current intents of all enemies when applied.\nAfter you are affected by a recorded intent, you are immune to the 2nd instance of the same recorded intent, and this Magic is removed",
    "Silence",
    "Reduce Backlash",
    "The next {Amount} attacks deal 50% less damage.",
    "Borrowing Period",
    "Sherry Tachibana's Magic",
    "Card Pile Change Record",
    "Star-Crossed Lovers",
    "The Cell",
    "The Cell is Home",
    "Play three cards of the same type consecutively to gain a power (once only)\nAttack: Mind-Cost Draw\nSkill: Dancing Men\nPower: Final Farewell",
    "Hasumi Reia's Magic",
    "Tracing",
    "Temporary Strength Down",
    "Temporary Strength",
    "Temporary Dexterity",
    "Unnoticed",
    "Every {Amount} turns, gain 1 stack of 【Complex Emotion】 and 1 random basic [rainbow][b]【Emotion】[/b][/rainbow]",
    "For every 13 cards played, randomly gain 1 [rainbow][b]【Basic Emotion】[/b][/rainbow] card added to [b]【Others' Emotions】[/b]; you may click this power to obtain [rainbow][b]【Emotion】[/b][/rainbow] cards.\nWhen 3 [rainbow][b]【Basic Emotions】[/b][/rainbow] are gained in one match, gain 1 [rainbow][b]【Complex Emotion】[/b][/rainbow] to fuse [rainbow][b]【Basic Emotions】[/b][/rainbow]",
    "For every 5 hand cards played, randomly upgrade 1 hand card.",
    "For every enough number of cards played, randomly upgrade 1 hand card and 1 draw pile card.",
    "Whenever you played no [b]Attack cards[/b] [color=#6666cc]last turn[/color], enter [color=#6666cc]【The Cell】[/color] at [color=#6666cc]start of this turn[/color].\nGain {Amount} Energy upon entering [color=#6666cc]【The Cell】[/color].",
    "Whenever you play an [color=#6699cc]Agree[/color] card, gain 1 [color=#ff99cc]Block[/color]. Allies gain 2 [color=#ff99cc]Block[/color]",
    "Gain 1 Energy for every 10 [color=#CC6666]【Transmigration】[/color] cards you play",
    "Whenever you play a card, gain 1 Vigor",
    "Whenever you deal attack damage to an enemy, gain 1 temporary Strength",
    "Every time you replace {Rewrites:diff()} monster intents, upgrade this combat's Silence-replacement intent pool",
    "Whenever you lose [color=#99ccff]【Reassurance】[/color], the next [b]Attack card[/b] is played twice.\n[color=#6666cc]Once per turn[/color]",
    "Whenever a card is exhausted, gain {Amount} Block.",
    "Whenever an ally plays a card for the first time in their turn, apply {Amount} stacks of [color=#6666cc]【Strange Smell】[/color] to a random enemy.\nTriggers once per ally per turn.",
    "At the start of each turn, randomly trigger [color=#cc99ff]【Audition】[/color] effects based on [color=#99ccff]【Reassurance】[/color] stacks.\nCurrently, triggering [color=#6666cc]【Brainwash】[/color] rewrite requires consuming {RequiredSilenceCost:diff()} stacks of [color=#6666cc]【Silence】[/color].",
    "The first {Amount} cards from recorded pools played each turn are exhausted after being played, and draw 1 [color=#6666cc]card[/color]",
    "Every time you replace {Rewrites:diff()} monster intents, upgrade this combat's Silence-replacement intent pool",
    "For every 4 {energyPrefix:energyIcons(1)} accumulated and spent, add 1 [color=#6666cc]【Blank Page】[/color] to your hand.\nIf the source card is upgraded, gain [color=#6666cc]【Blank Page+】[/color] instead.",
    "Each stack of this power reduces hand size by 1\nCards removed from combat by Erosion are stored here. When this power is removed, return these cards to your hand",
    "For every 3 cards with [color=#ffcc66]【Truth】[/color] components auto-played, gain 1 Energy and draw 2 [color=#CC6666]cards[/color].",
    "Door Gap Block",
    "The Door is Not Locked",
    "Witch Trial",
    "Witchification",
    "Witch Prison",
    "Witch Killer",
    "Witch Ritual",
    "Witch Factor",
    "Witch's Power",
    "Backstage Resentment",
    "Energy Gain",
    "Energy Loss",
    "At the start of your turn, randomly gain {Amount} total buffs:\nGain Energy, Buffer, [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nYou may choose 1 card with [color=ff99cc]Exhaust all cards[/color]\nRandomly set 1 card [color=#ffcc99]to 0 cost this turn and move it to your hand.[/color]",
    "At the start of your turn, randomly gain {Amount} total buffs:\nGain Energy, Buffer, [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nYou may choose 1 card with [color=ff99cc]Exhaust all cards[/color]\nRandomly set 1 card [color=#ffcc99]to 0 cost this turn and move it to your hand.[/color]\nWhen attacked, ignore Block; when attacking enemies, ignore Block and damage limits.\nMay revive once to full HP this combat.",
    "At the start of your turn, randomly gain {Amount} total buffs:\nGain Energy, Buffer, [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nYou may choose 1 card with [color=ff99cc]Exhaust all cards[/color]\nRandomly set 1 card [color=#ffcc99]to 0 cost this turn and move it to your hand.[/color]\nHigher stacks continue to strengthen enemies' positive effects.",
    "At the start of your turn, randomly gain {Amount} total buffs:\nGain Energy, Buffer, [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nYou may choose 1 card with [color=ff99cc]Exhaust all cards[/color]\nRandomly set 1 card [color=#ffcc99]to 0 cost this turn and move it to your hand.[/color]\nAt the start of the enemy turn, enemies gain {Mllm} stacks of [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nHigher stacks continue to strengthen enemies' positive effects.",
    "At the start of your turn, randomly gain {Amount} total buffs:\nGain Energy, Buffer, [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nYou may choose 1 card with [color=ff99cc]Exhaust all cards[/color]\nRandomly set 1 card [color=#ffcc99]to 0 cost this turn and move it to your hand.[/color]\nAt the start of the enemy turn, enemies gain {Mllm} stacks of [color=#ffcc99]【Meruru on Ice's Magic】[/color]. Each attack deals {Amount} extra damage to [color=#ff99cc]Sakuraba Emma[/color]; player attacks have a {MisdirectChance}% chance to misfire onto [color=#ff99cc]Sakuraba Emma[/color], and this damage cannot be lethal.\nHigher stacks continue to strengthen enemies' positive effects.",
    "At the start of your turn, randomly gain {Amount} total buffs:\nGain Energy, Buffer, [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nYou may choose 1 card with [color=ff99cc]Exhaust all cards[/color]\nRandomly set 1 card [color=#ffcc99]to 0 cost this turn and move it to your hand.[/color]\nAt the start of the enemy turn, enemies gain {Mllm} stacks of [color=#ffcc99]【Meruru on Ice's Magic】[/color]. Each attack deals {Amount} extra damage to [color=#ff99cc]Sakuraba Emma[/color]; player attacks have a {MisdirectChance}% chance to misfire onto [color=#ff99cc]Sakuraba Emma[/color], and this damage cannot be lethal.\nReduce this character's current HP to 1; afterwards, if this character would die, stacks become {MaxStacks}.\nHigher stacks change the effect.",
    "At the start of your turn, randomly gain {Amount} total buffs:\nGain Energy, Buffer, [color=#ffcc99]【Meruru on Ice's Magic】[/color].\nYou may choose 1 card with [color=ff99cc]Exhaust all cards[/color]\nRandomly set 1 card [color=#ffcc99]to 0 cost this turn and move it to your hand.[/color]\nAt the start of the enemy turn, enemies gain {Mllm} stacks of [color=#ffcc99]【Meruru on Ice's Magic】[/color]. Each attack deals {Amount} extra damage to [color=#ff99cc]Sakuraba Emma[/color].\nHigher stacks continue to strengthen enemies' positive effects.",
]

assert len(eng_2) == 113, len(eng_2)
mapping = {texts[i+88].replace('\n', '\\n'): eng_2[i].replace('\n', '\\n') for i in range(113)}
with io.open(r'D:\ManosabaLin\map_powers_eng_2.json', 'w', encoding='utf-8') as f:
    json.dump({"eng/powers.json": mapping}, f, ensure_ascii=False, indent=2)
print("eng powers 批次2 完成: 113 条 (89-201)")
