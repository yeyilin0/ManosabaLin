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

eng_3 = [
    "For every [color=#CC6666]card[/color] you play, gain 1 [color=#CC6666]Block[/color]; for every 2 [color=#CC6666]cards[/color] played, deal 1 random [color=#CC6666]damage[/color]",
    "Gain 2 stacks of [color=#CC6666]【Perjury】[/color] for every [color=#CC6666]【Transmigration】[/color] card you play",
    "The Norwood Builder",
    "Noah's Cooperation",
    "The Bystander's Compliance",
    "Paranoid Justice",
    "Intel",
    "Emotion",
    "Emotion Surge",
    "Brute Force Method",
    "Confirm the Door Gap",
    "Group Chat Smell",
    "If the player did not play 1 [b]Attack[/b], 1 [b]Skill[/b], and 1 [b]Power[/b] card this turn, deal damage and gain Witchification",
    "If you play no [b]Attack[/b] [color=#6666cc]this turn[/color], automatically play a copy of the recorded card at [color=#6666cc]end of turn[/color].",
    "If the recorded card was not played [color=#6666cc]this turn[/color], gain {Amount} stacks of [color=#99ccff]【Reassurance】[/color] at [color=#6666cc]end of turn[/color]; afterwards, playing that card restores HP.",
    "If the target's HP percentage is ≤ a quarter of your [b]Witchification[/b], playing an [color=#ff99cc]Attack card[/color] that deals [color=#ff99cc]unblockable damage[/color] executes the target and you gain 10 stacks of [b]【Witchification】[/b]",
    "If exhausted, gain 50 Witchification; otherwise lose 30 Witchification",
    "Decision of the Third Loop",
    "Deleted Stress",
    "Trial in Session",
    "Judgement Chain",
    "Interrogation Pause",
    "Uncontrolled Justice",
    "Lion's Mane Jellyfish",
    "Ten Returns of Light",
    "Emotion Shield",
    "When taking attack damage, remove 1 stack and spread 50% of the unblocked damage to all other enemies. If there are no other enemies, gain 1 stack of [b]Weak[/b] instead.",
    "Cosmic Apathy",
    "Page Corner",
    "Justice by Different Roads",
    "Counting to Three is Fine",
    "Twin Witch Power",
    "Dual Ascension",
    "Death Rewind",
    "On [color=#CC6666]death[/color], [color=#CC6666]revive[/color] with HP equal to [b]Witchification[/b] stacks\nAt Witchification=200, remove [color=#CC6666]Death Rewind[/color] from the deck after combat\nAt Witchification=300, this power becomes [color=#CC6666]ineffective[/color]",
    "Investigation",
    "Damage [b]increases[/b] with stacks; at [b]100[/b] stacks, gain [b]【Magic】[/b],at [b]200 stacks[/b], playing [color=#CC6666]Skill cards[/color] and [color=#ff99cc]Power cards[/color] makes you lose 1 HP, and at end of turn you lose 13 HP,at [b]300 stacks[/b], your Skill cards gain Exhaust and playing them makes you lose [b]Witchification[/b] minus 100 HP; at this point, playing [color=#cccccc]Attack cards[/color] restores 3 HP",
    "Randomly give [color=#339966]【[/color][color=#cc9966]Tri[/color][color=#6699cc]al[/color][color=#339966]】[/color] enchantments to unenchanted cards in the deck",
    "Index Page",
    "Bodyguard Shield",
    "Dancing Men",
    "Frenzied Alternating Offense and Defense",
    "Mad Card Chase",
    "Perjury",
    "The fact that I hate you will never change",
    "Cannot gain [color=#99ccff]【Reassurance】[/color];\nyou draw 1 fewer [color=#6666cc]card[/color] at [color=#6666cc]start of your turn[/color], hand size is halved, and you lose 1 {energyPrefix:energyIcons(1)}\nRemoved at [color=#6666cc]end of turn[/color]",
    "Silent Footnote",
    "Silent Amplifier",
    "Ignore HP loss caused by Witchification\nDie after 3 turns\nAt [color=#CC6666]start of turn[/color], for every 50 stacks of [b]【Witchification】[/b], take 1 Attack card from the draw pile and set it to [gold]zero cost[/gold] this turn",
    "I Am My Own Friend",
    "A Hopeful Tomorrow",
    "Brainwash",
    "Brainwash Backlash",
    "Next-turn Energy Loss",
    "Gain 1 emotion stack for each attack taken before the start of next turn.",
    "At start of next turn, gain {energyPrefix:energyIcons(1)}",
    "At start of next turn, lose {energyPrefix:energyIcons(1)}",
    "At start of next turn, randomly copy 1 basic emotion card into your hand.",
    "After the next [b]Attack card[/b] is played, gain {energyPrefix:energyIcons(1)}.",
    "The next time you would lose [color=#99ccff]【Reassurance】[/color] from unblocked damage, lose only 1 stack instead.\nIf it does not trigger before [color=#6666cc]end of turn[/color], gain 1 stack of [color=#6666cc]【Silence】[/color].",
    "The next Brainwash consumes {NextBrainwashSilenceCost:diff()} stacks of Silence",
    "The next fatal damage instead leaves you with 1 HP; if not triggered, gain [color=#6666cc]【Blank Page】[/color] at end of turn.",
    "When the next [color=#6666cc]【Blank Page】[/color] generates a card, copy the generated card",
    "Natsume Anan's Magic",
    "Preliminary Vote",
    "Suspicion",
    "Soft Agreement",
    "Modify the [color=#6666cc]target intent[/color] and change the target to any ally",
    "Modify the target's [gold]intent target[/gold] (cannot be used when the target has card-giving or card-stuffing intents)",
    "Retained Counter",
    "Judgement Echo",
    "Choose 1 card to exhaust",
    "Choose cards to retain",
    "Choose the [rainbow][b]【Complex Emotion】[/b][/rainbow] to copy",
    "Choose an Attack card to set to zero cost",
    "Choose a hand card to exhaust",
    "Choose a card to remove and forget its pool",
    "Choose a hand card to rewrite",
    "Choose an [rainbow][b]【Emotion】[/b][/rainbow] card to add to your hand",
    "Choose the rewrite card from the designated pool",
    "Arisa's Bond",
    "Delayed Return",
    "Suspicion Rising",
    "Removed cards",
    "Intent has been induced; removed at end of turn.",
    "Sakuraba Emma's Magic",
    "When an enemy with this power deals [color=#CC6666]damage[/color], each stack adds 10%; gain 1 stack at end of each turn",
    "When the owner is attacked, the attacker gains 1 emotion stack, and this power loses 1 stack",
    "Internal effect for recording cards changing piles.",
    "After right-clicking or two-finger tapping [color=#6666cc]【Silence】[/color], remove all [color=#6666cc]【Silence】[/color] and gain Vigor and a 0-cost multi-hit [b]Attack[/b] card.",
    "Right-click or two-finger tap to consume 13 stacks of [color=#6666cc]【Silence】[/color], making all enemies immediately execute their current intents, then choose 1 replacement intent; each replacement intent requires [color=#6666cc]rotating selection[/color]",
    "Right-click or two-finger tap to directly rewrite all enemies' current intents without consuming [color=#6666cc]【Silence】[/color].\nAfter forced brainwash, gain 1 stack of [color=#6666cc]【Brainwash Backlash】[/color]",
    "Right Fist",
    "Omen",
    "Original Sin",
    "Hanna Tohno's Magic",
    "Damage you take [green]this turn[/green] is halved",
    "After dealing [color=ff99cc]unblockable damage[/color], if the target's HP is equal to or below the stack count, it [color=ff99cc]dies[/color] immediately",
    "Damage dealt becomes 3x, and the target changes to a random field target. This damage cannot be lethal. Removed after 1 turn.",
    "Sawada Koko's Magic",
    "On combat victory, randomly select rare or uncommon cards from your deck (excluding 【Ending】 itself), copy them into [color=#CC6666]【Ending】[/color]; attached [color=#CC6666]uncommon cards[/color] can trigger their extra effect once, [color=#CC6666]rare cards[/color] twice",
    "Vindication",
    "True Criminal",
    "Truth",
    "Justice",
    "Residual Warmth of Judgement",
    "Obsession",
    "Clutch the Sleeve",
    "Arisa Fuji's Magic",
    "Max 3 stacks.\nWhen taking any unblocked damage, lose all [color=#99ccff]【Reassurance】[/color].\nPlaying different cards triggers effects: [b]Attack cards[/b] deal extra damage equal to stacks; [b]Skill cards[/b] gain Block equal to stacks; [b]Power cards[/b] gain [color=#6666cc]【Silence】[/color] equal to stacks.\nIf [color=#99ccff]【Reassurance】[/color] is at 3 stacks at the end of 2 consecutive of your [color=#6666cc]turns[/color], lose it and gain [color=#6666cc]【Isolated】[/color]",
    "Final Farewell",
    "Guilt Chain",
    "Left Fist",
    "Left hand has great strength, right hand has great strength",
    "Saeki Miria's Magic",
]

assert len(eng_3) == 115, len(eng_3)
mapping = {texts[i+201].replace('\n', '\\n'): eng_3[i].replace('\n', '\\n') for i in range(115)}
with io.open(r'D:\ManosabaLin\map_powers_eng_3.json', 'w', encoding='utf-8') as f:
    json.dump({"eng/powers.json": mapping}, f, ensure_ascii=False, indent=2)
print("eng powers 批次3 完成: 115 条 (202-316)")
