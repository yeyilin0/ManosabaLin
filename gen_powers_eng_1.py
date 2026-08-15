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

eng_1 = [
    "At [b]the start of the turn[/b], gain 40 stacks of [b]【Witchification】[/b]",
    "[b]Each turn[/b], when you play a card against an enemy with [b]【Suspicion】[/b], apply 1 stack of [b]【Suspicion】[/b] to another random enemy. Triggers up to 2 times per turn",
    "[b]Witch trial, court is now in session![/b]\nFor every 2 stacks of this power, lose 1 [b]Strength[/b].\nWhen an ally has 12 stacks, they gain [b]【Bad Ending】[/b].\nWhen an enemy has 12 stacks, they lose positive powers, restored at end of turn",
    "When [color=#6666cc]【Blank Page】[/color] generates a card, {Amount} extra options appear",
    "[color=#6666cc]This turn[/color], after the 3rd counting card is played, it returns to your hand, costs 0, and is exhausted on its next play.",
    "[color=#6666cc]This turn[/color], for every 3 counting cards played, draw 1 [color=#6666cc]card[/color].\nWhen gaining this power, for each stack of [color=#6666cc]Reassurance[/color], every 2 counting cards played reduces the cost of the next counting card by that amount",
    "[color=#6666cc]This turn[/color], after the next non-[b]Attack[/b] card is played, if no enemy intends to attack, gain 1 stack of [color=#99ccff]【Reassurance】[/color] and draw a card",
    "[color=#6666cc]This turn[/color], when the next consecutive cards of the same type reach the specified count, gain 1 stack of [color=#99ccff]【Reassurance】[/color] and take a card of a different type from the draw pile.",
    "[color=#6666cc]This turn[/color], the next time you take fatal damage, survive with 1 HP instead and gain a Page.\nIf it does not trigger, gain 1 [color=#6666cc]【Blank Page】[/color] at [color=#6666cc]end of turn[/color].",
    "At [color=#6666cc]end of turn[/color], if you played no [b]Attack cards[/b] this [color=#6666cc]turn[/color], gain {Amount} stacks of [color=#6666cc]【Silence】[/color]",
    "At [color=#6666cc]start of turn[/color], you may choose 1 card from your hand; then a random character card pool is designated, and you may choose 1 card from it to transform your hand card into. If that pool has been recorded by [color=#6666cc]Anan's Sketchbook[/color], or is successfully recorded through this effect, add a copy of the transformed card to your deck. If the Sketchbook has already recorded 3 pools, you may first remove 1 card belonging to a recorded pool from your deck and forget that pool.",
    "At [color=#6666cc]start of turn[/color], a random character card pool is designated, and you may rewrite 1 hand card; if that pool is recorded or successfully recorded, copy it into your deck.",
    "At [color=#6666cc]start of turn[/color], trigger [color=#cc99ff]【Audition】[/color], with effects triggered randomly based on current [color=#99ccff]【Reassurance】[/color] stacks.\n1: Generate a temporary 0-cost exhaust card from a recorded pool\n2: Gain [color=#6666cc]【Blank Page+】[/color]\n3: Consume {RequiredSilenceCost:diff()} stacks of [color=#6666cc]【Silence】[/color] and trigger [color=#6666cc]【Brainwash】[/color]'s rewrite; if Silence is insufficient or cannot rewrite, gain [color=#6666cc]【Blank Page】[/color]\n4: Draw 1 [color=#99ccff]card[/color] and make a random hand card gain 1 stack of [color=#99ccff]【Reassurance】[/color] after being played\nIf the source card is upgraded, trigger the first randomly rolled effect one extra time",
    "[color=#6666cc]Each turn[/color], the 1st time [color=#6666cc]【Silence】[/color] is consumed, gain {energyPrefix:energyIcons(1)}; afterwards, instead draw 1 [color=#6666cc]card[/color].\nEvery 4th cycle",
    "[color=#6666cc]Each turn[/color], the first [b]Skill card[/b] played is placed on top of the draw pile instead, and its cost is reduced by 1 until played.\nAt [color=#6666cc]end of turn[/color], gain {Amount} stacks of [color=#6666cc]【Silence】[/color] and retain up to 1 hand card.\nIf you play an [b]Attack card[/b], leave [color=#6666cc]【The Cell】[/color].",
    "[color=#6666cc]Each turn[/color], the first time each ally plays a [b]rare[/b] card, add a temporary copy costing 1 to your hand. It gains [b]Exhaust[/b].\nIf this power comes from an upgraded card, the copy costs 0 instead.\nAfter you play a conceived copy, gain 1 [b]Strength[/b] and 1 [b]Dexterity[/b].",
    "At [color=#6666cc]start of next turn[/color], return {Amount} promised cards to your hand and let them be playable once for free this [color=#6666cc]turn[/color]; then generate 1 card from their pool that is playable once for free this [color=#6666cc]turn[/color] and has [b]Ethereal[/b] and [b]Exhaust[/b].",
    "At [color=#6666cc]start of next turn[/color], return {Amount} stored cards to your hand.",
    "At [color=#999999]start of turn[/color], take 1 random card from the discard pile, then lose 1 stack",
    "[color=#CC6666][b]Choose[/b][/color]",
    "At [color=#CC6666]end of turn[/color], restore {Amount} HP.",
    "At [color=#CC6666]end of turn[/color]\nConsume 1 stack of [color=#CC6666]【Perjury】[/color] to gain 1 stack of [color=#CC6666]【Justice】[/color]\nConsume 1 stack of [b]【Suspicion】[/b] to gain 2 stacks of [b]【Witchification】[/b]",
    "At [color=#CC6666]end of turn[/color], trigger [color=#CC6666]【Justice】[/color]'s healing effect one extra time",
    "At [color=#CC6666]end of turn[/color], restore [color=#CC6666]HP[/color] and lose 1 stack.",
    "At [color=#CC6666]start of turn[/color], gain 1 stack of [color=#CC6666]【Perjury】[/color]",
    "At [color=#CC6666]start of turn[/color], [color=#CC6666][b]【Estrangement】[/b][/color]＋1, and transform 1 hand card into a random [color=#CC6666][b]【Estrangement】[/b][/color] card",
    "At [color=#CC6666]start of turn[/color], gain {energyPrefix:energyIcons(1)}",
    "At [color=#CC6666]start of turn[/color], gain 2 stacks of [color=#CC6666]【Justice】[/color]",
    "At [color=#CC6666]start of turn[/color], lose 1 stack of [color=#CC6666]【Justice】[/color], gain {energyPrefix:energyIcons(1)}",
    "[color=#cc9966]Card play is forbidden[/color] until the hand exchange ends",
    "At [color=#cccccc]start of turn[/color], gain 1 [color=#cccccc]Strength[/color], 1 [color=#CC6666]Energy[/color], 1 [color=#ff99cc]card[/color]",
    "At [color=#cccccc]start of turn[/color], swap the values of [color=#ff99cc][b]【Closeness】[/b][/color] and [color=#CC6666][b]【Estrangement】[/b][/color]",
    "At [color=#cccccc]start of next turn[/color], gain 1 stack of [color=#CC6666]【Justice】[/color]",
    "When [color=#ff0000][b]【Closeness】[/b][/color] increases, gain [color=#ff99cc]equal Energy[/color] and [color=#ff0000]【Arisa Fuji's Magic】[/color]\nWhen [color=#ff0000][b]【Estrangement】[/b][/color] increases, consume equal [color=#ff0000]【Arisa Fuji's Magic】[/color] to draw an equal number of [color=#ff99cc]cards[/color]",
    "At [color=#ff0000]end of turn[/color], lose 1 HP then remove this",
    "At [color=#ff9966]start of turn[/color], gain buffs based on the enemy's intent\nAttack: gain 5 Block\nDefense/Buff: gain 1 Strength\nDebuff: gain 1 Dexterity\nOther: apply 1 Weak to a random enemy",
    "[color=#ff99cc][b]「Clo[/b][/color][color=#CC6666][b]seness」[/b][/color]\nWhen you play the same card a 2nd time, that card gains the 「Memory of the Witch」 mark, plus [color=#ff99cc]Recur[/color] and [color=#CC6666]Exhaust on play[/color]. Each card name [color=#CC6666]only triggers once[/color]\nIf 13 cards are removed (viewable via right-click or two-finger tap), give 【Ending】 in the deck 1 stack of [color=#ff99cc]「Hiro himself is crying, yet he blames me」[/color]",
    "[color=#ff99cc][b]Choose[/b][/color]",
    "[color=#ff99cc]Cannot play cards manually[/color]\nAt full HP, remove this power and the buffer. If not at full HP after 3 turns, you are killed again",
    "[color=#ff99cc]Click to select[/color] an enemy, making it [color=#ff99cc]lose a quarter of its HP[/color], lose 1 stack and remove 1 stack of [color=#CC6666]「No choice, I'll just stay by your side」[/color] from the deck",
    "At [color=#ff99cc]start of turn[/color], remove all [color=#339966]【[/color][color=#cc9966]Tri[/color][color=#6699cc]al[/color][color=#339966]】[/color] enchantments in the deck; for each removed, choose a random enchantment type and apply it to an equal number of unenchanted cards, then generate 1 card matching that keyword with the enchantment and add it to your hand",
    "At [color=#ff99cc]start of turn[/color], consume 2 stacks of [color=#CC6666]【Estrangement】[/color] to transform a hand card into an [color=#CC6666]【Estrangement】[/color] card, or consume 2 stacks of [color=#ff99cc]【Closeness】[/color] to generate a [color=#ff99cc]【Closeness】[/color] card; both may be played once for free",
    "At [color=#ffcc99]start of turn[/color], restore 30 HP",
    "When you [color=CC6666]die[/color], revive with 40 HP, then lose 1 stack",
    "When you play a [color=#339966]【[/color][color=#cc9966]Tri[/color][color=#6699cc]al[/color][color=#339966]】[/color]-enchanted card a multiple of 5 times [color=ff99cc]each turn[/color], deal 15 [color=#ff99cc]damage[/color] to the enemy with the lowest HP",
    "At [color=ff99cc]start of each turn[/color]\n[color=#339966]【[/color][color=#cc9966]Tri[/color][color=#6699cc]al[/color][color=#339966]】[/color] enchantment counter +1",
    "At [color=ff99cc]start of each turn[/color], look at the top {Stacks:diff()} cards of the [color=#ff99cc]draw pile[/color]; you may discard them or put them back",
    "[color=ff99cc]Choose[/color]",
    "[jitter][color=#CC6666]「In this world, I have become wreckage wandering the prison————」[/color][/jitter]\nAfter each intent is executed, each stack has a 20% chance to execute the same intent one extra time",
    "[jitter][color=#cccccc]「The embodiment of ugliness——turning into an immortal monster 【Witch】, falling into endless despair」[/color][/jitter]\nIf, after the [color=gray]third intent[/color]'s attack, all players hold a number of [color=gray]【Transformation Magic】[/color] cards equal to 3 times the player count, attack [color=gray]1 additional[/color] time next turn\nThe first death each turn is prevented; HP becomes infinite and you gain [b]Block[/b] equal to 3 times Witchification, then switch intent [color=gray](once per turn)[/color]; if Block is not broken, at the start of next turn max HP returns to its value at combat start and half HP is restored.",
    "[jitter][color=#cccccc]「Act Two begins」[/color][/jitter]\nWhen HP drops to half, gain Block equal to twice Witchification and switch intent.",
    "[jitter][color=#cccccc]「The Witch's Island soars through the sky once more」[/color][/jitter]\nWhen HP drops to half, gain Block equal to twice Witchification and switch intent",
    "[jitter][color=#cccccc]「You, who lost your mind and became wreckage, would only kill everyone even if you returned to the past」[/color][/jitter]\nOn death, revive: all players heal to full and negative powers are removed; self removes other negative powers, max HP increases, heal to full and gain 50 Block\nWhen attacked and losing HP, 20% chance to give the attacker 1 stack of [b]Intel[/b]; each death increases the chance by 10%, up to 5 triggers per turn",
    "[jitter][color=#cccccc]「To kill the 【Witch】, they seem to have developed a drug with the same effect......」[/color][/jitter]\nWhen 13 stacks are reached, remove this power and allies gain 1 [b]Thirteen Waters[/b]",
    "[purple]This turn[/purple], absorb damage taken by allies.\n[purple]Next turn[/purple], give the protected ally Block equal to the damage absorbed. If Closeness > Estrangement, you also gain Block equal to the damage absorbed",
    "At [purple]start of turn[/purple], randomly play a playable hand card, then remove 1 stack",
    "「Feigned Death」",
    "「Hiro himself is crying, yet he blames me」",
    "「True Ending」",
    "When 【Witchification】 increases, deal equal damage to a random target that can be affected by powers",
    "At 100 stacks, gain Magic",
    "When the Boss executes an intent, the phantom executes one extra intent",
    "Emma's Cooperation",
    "Reassurance",
    "Mag's Magic",
    "When a marked enemy next deals damage to you, record up to {Amount} points of [color=#6666cc]damage[/color] as [blue]【Red Butterfly】[/blue] healing. If you already have [blue]【Red Butterfly】[/blue], add to it; otherwise, heal at [color=#6666cc]start of next turn[/color].",
    "When a recorded card is played, gain extra Block.",
    "When a recorded card is played at [color=#6666cc]next turn[/color], gain 1 stack of [color=#99ccff]【Reassurance】[/color].",
    "Cannot gain Witchification this combat; instead record Storm. At start of turn, for every 40 Storm cleared, gain 1 Energy + draw 1 card + gain a random basic emotion",
    "【Brainwash Backlash】 has already been reduced this combat",
    "During this combat, each time an enemy's intent is rewritten, record 1.\nAt [color=#6666cc]start of turn[/color], if at least 3 have been recorded, consume up to {Amount} records: draw that many [color=#6666cc]cards[/color] and gain that much {energyPrefix:energyIcons(1)}",
    "This turn, you may use [color=#6666cc]【Silence】[/color] or [color=#6666cc]【Brainwash】[/color] via right-click or two-finger tap for free. At [blue]end of turn[/blue], give enemies rewritten by this free use {RewrittenNymPower:diff()} stacks of [blue]【Noah Kinoshita's Magic】[/blue]; if no enemy was rewritten, give all enemies {NoRewriteNymPower:diff()} stacks.",
    "Each card played this turn increases the emotion counter by 1 extra.",
    "This turn, whenever an ally draws 1 card and recovers {energyPrefix:energyIcons(1)}, give them 1 stack of Justice",
    "This turn, when any of your Trial enchantment counters reach 5, anchor them; afterwards, allies can also trigger the extra effect of that enchantment counter",
    "Deal at least {DamageTarget} damage and gain at least {BlockTarget} Block this turn.\nSuccess: gain 20 Block.\nFailure: the Boss gains 10 stacks of [b]Witchification[/b] and 20 Block, and records 1 task failure (each failure makes the next attack deal extra damage)",
    "The next {Amount} times [b]Vigor[/b] would decrease this turn, it does not decrease instead.",
    "Meruru on Ice's Magic",
    "No More Averting",
    "No Answer",
    "Echo",
    "Draft Rewrite",
    "Defusal Support",
    "Take double damage from enemies. Enemy damage to allies is halved\nFor every 20 attack damage you accumulate, gain 1 stack of next-turn Energy; other allies gain 1 stack of next-turn draw and 10 stacks of Witchification",
    "Noah Kinoshita's Magic",
    "Shockwave",
    "Nascent Emotion",
    "Tangled Memories",
]

assert len(eng_1) == 88, len(eng_1)
mapping = {texts[i].replace('\n', '\\n'): eng_1[i].replace('\n', '\\n') for i in range(88)}
with io.open(r'D:\ManosabaLin\map_powers_eng_1.json', 'w', encoding='utf-8') as f:
    json.dump({"eng/powers.json": mapping}, f, ensure_ascii=False, indent=2)
print("eng powers 批次1 完成: 80 条")
