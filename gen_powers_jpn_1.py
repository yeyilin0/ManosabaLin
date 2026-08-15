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

jpn_1 = [
    "[b]ターン開始[/b]時、[b]【魔女化】[/b]を40層獲得",
    "[b]毎ターン[/b][b]【嫌疑】[/b]を持つ敵にカードをプレイする時、別のランダムな敵1体に[b]【嫌疑】[/b]を1層付与。毎ターン最大2回発動",
    "[b]魔女裁判、開廷！[/b]\nこの能力2層ごとに[b]筋力[/b]を1減少。\n味方が12層持つと[b]【バッドエンド】[/b]を獲得。\n敵が12層持つとプラス能力を失い、ターン終了時に返還",
    "[color=#6666cc]【空白ページ】[/color]がカードを生成する時、選択肢が{Amount}個追加で出現",
    "[color=#6666cc]このターン[/color]3枚目のカウントカードをプレイ後、手札に戻り、コストが0になり、次にプレイする時に消耗。",
    "[color=#6666cc]このターン[/color]カウントカード3枚プレイごとに[color=#6666cc]カード[/color]を1枚引く。\nこの能力獲得時、[color=#6666cc]安心[/color]1層ごとに、カウントカード2枚プレイごとに次のカウントカードのコストをその数値分減少",
    "[color=#6666cc]このターン[/color]次の非[b]攻撃[/b]カードをプレイした後、敵に攻撃の意図がなければ[color=#99ccff]【安心】[/color]を1層獲得しカードを引く",
    "[color=#6666cc]このターン[/color]次に指定枚数の同じ種類のカードを連続プレイすると、[color=#99ccff]【安心】[/color]を1層獲得し、ドローパイルから別の種類のカードを1枚取る。",
    "[color=#6666cc]このターン[/color]次に致命的ダメージを受ける時、代わりにHP1で生存しページを獲得。\n発動しなければ[color=#6666cc]ターン終了[/color]時[color=#6666cc]【空白ページ】[/color]を1枚獲得。",
    "[color=#6666cc]ターン終了[/color]時、この[color=#6666cc]ターン[/color][b]攻撃カード[/b]をプレイしていなければ[color=#6666cc]【沈黙】[/color]を{Amount}層獲得",
    "[color=#6666cc]ターン開始[/color]時、手札のカード1枚を選択できる。その後ランダムなキャラクターのカードプールが指定され、そのプールからカード1枚を選んで手札のカードを変形できる。そのプールが[color=#6666cc]安安のスケッチブック[/color]に記録済みか、この効果で記録に成功すれば、変形後のカードを1枚コピーしてデッキに追加。スケッチブックがすでに3つのプールを記録していれば、先に記録済みプールに属するデッキのカードを1枚削除してそのプールを忘れられる。",
    "[color=#6666cc]ターン開始[/color]時、ランダムなキャラクターのカードプールが指定され、手札のカード1枚を書き換えられる。そのプールが記録済みか記録に成功すればデッキにコピー。",
    "[color=#6666cc]ターン開始[/color]時[color=#cc99ff]【オーディション】[/color]を発動、現在の[color=#99ccff]【安心】[/color]層に応じてランダムな効果が発動。\n1: 記録済みカードプールの一時0コスト消耗カードを1枚生成\n2: [color=#6666cc]【空白ページ+】[/color]を獲得\n3: [color=#6666cc]【沈黙】[/color]を{RequiredSilenceCost:diff()}層消費して[color=#6666cc]【洗脳】[/color]の書き換えを発動、沈黙不足または書き換え不能なら[color=#6666cc]【空白ページ】[/color]を獲得\n4: [color=#99ccff]カード[/color]を1枚引き、ランダムな手札カードがプレイ後に[color=#99ccff]【安心】[/color]を1層獲得\n元のカードが強化されていれば、最初にランダムで出た効果を1回追加発動",
    "[color=#6666cc]毎ターン[/color][color=#6666cc]【沈黙】[/color]の最初の消費時{energyPrefix:energyIcons(1)}を獲得、以降は[color=#6666cc]カード[/color]を1枚引く代わりに変更。\n4回ごとに繰り返し",
    "[color=#6666cc]毎ターン[/color]最初の[b]スキルカード[/b]はドローパイルの上に置かれ、プレイするまでコスト-1。\n[color=#6666cc]ターン終了[/color]時[color=#6666cc]【沈黙】[/color]を{Amount}層獲得し、手札を最大1枚まで保持。\n[b]攻撃カード[/b]をプレイすると[color=#6666cc]【牢獄】[/color]から出る。",
    "[color=#6666cc]毎ターン[/color]各味方が最初に[b]レア[/b]カードをプレイする時、コスト1の一時コピーを1枚手札に追加。それは[b]消耗[/b]を獲得。\nこの能力が強化カード由来なら、コピーのコストは0。\n着想コピーカードをプレイ後、[b]筋力[/b]1と[b]敏捷[/b]1を獲得。",
    "[color=#6666cc]次のターン開始[/color]時、約束されたカード{Amount}枚を手札に戻し、この[color=#6666cc]ターン[/color]無料で1回プレイ可能にする。その後そのプールからこの[color=#6666cc]ターン[/color]無料1回プレイ可能で[b]消滅[/b]と[b]消耗[/b]を持つカードを1枚生成。",
    "[color=#6666cc]次のターン開始[/color]時、預けたカード{Amount}枚を手札に戻す。",
    "[color=#999999]ターン開始[/color]時、捨て札パイルからランダムなカードを1枚獲得後、1層減少",
    "[color=#CC6666][b]選択[/b][/color]",
    "[color=#CC6666]ターン終了[/color]時、HPを{Amount}回復。",
    "[color=#CC6666]ターン終了[/color]時\n[color=#CC6666]【偽証】[/color]を1層消費して[color=#CC6666]【正義】[/color]を1層獲得\n[b]【嫌疑】[/b]を1層消費して[b]【魔女化】[/b]を2層獲得",
    "[color=#CC6666]ターン終了[/color]時[color=#CC6666]【正義】[/color]の回復効果を1回追加発動",
    "[color=#CC6666]ターン終了[/color]時[color=#CC6666]HP[/color]を回復し1層減少。",
    "[color=#CC6666]ターン開始[/color]時[color=#CC6666]【偽証】[/color]を1層獲得",
    "[color=#CC6666]ターン開始[/color]時[color=#CC6666][b]【疎遠】[/b][/color]＋1、手札のカード1枚をランダムな[color=#CC6666][b]【疎遠】[/b][/color]カードに変形",
    "[color=#CC6666]ターン開始[/color]時{energyPrefix:energyIcons(1)}を獲得",
    "[color=#CC6666]ターン開始[/color]時[color=#CC6666]【正義】[/color]を2層獲得",
    "[color=#CC6666]ターン開始[/color]時[color=#CC6666]【正義】[/color]を1層失い、{energyPrefix:energyIcons(1)}を獲得",
    "[color=#cc9966]カードプレイ禁止[/color]手札交換が終わるまで",
    "[color=#cccccc]ターン開始[/color]時[color=#cccccc]筋力[/color]1、[color=#CC6666]エネルギー[/color]1、[color=#ff99cc]カード[/color]1枚を獲得",
    "[color=#cccccc]ターン開始[/color]時[color=#ff99cc][b]【親密】[/b][/color]と[color=#CC6666][b]【疎遠】[/b][/color]の数値を入れ替え",
    "[color=#cccccc]次のターン開始[/color]時[color=#CC6666]【正義】[/color]を1層獲得",
    "[color=#ff0000][b]【親密】[/b][/color]が増加する時、[color=#ff99cc]同量のエネルギー[/color]と[color=#ff0000]【藤アリサの魔法】[/color]を獲得\n[color=#ff0000][b]【疎遠】[/b][/color]が増加する時、同量の[color=#ff0000]【藤アリサの魔法】[/color]を消費して同数枚の[color=#ff99cc]カード[/color]を引く",
    "[color=#ff0000]ターン終了[/color]時HPを1失い、削除",
    "[color=#ff9966]ターン開始[/color]時、フィールドの敵の意図に応じて対応するバフを獲得\n攻撃: 防御5獲得\n防御/バフ: 筋力1獲得\nデバフ: 敏捷1獲得\nその他: ランダムな敵に弱体1層",
    "[color=#ff99cc][b]「親[/b][/color][color=#CC6666][b]密」[/b][/color]\n同じカードを2回目にプレイすると、そのカードは「魔女の記憶」のマークを獲得し、[color=#ff99cc]再演[/color]と[color=#CC6666]プレイ時消耗[/color]を獲得。カード名ごとに[color=#CC6666]1回のみ発動[/color]\nカードを13枚削除すると(右クリックまたは2本指タップで確認可能)、デッキの【エンディング】に[color=#ff99cc]「ヒロ本人が泣いているのに、私を責めるなんて」[/color]を1層付与",
    "[color=#ff99cc][b]選択[/b][/color]",
    "[color=#ff99cc]手動でのカードプレイ不可[/color]\n満HPでこの能力とバッファを削除。3ターン後も満HPでなければ再度死亡",
    "[color=#ff99cc]クリックで選択[/color]した敵は[color=#ff99cc]HPの4分の1を失い[/color]、1層減少し、デッキの[color=#CC6666]「仕方ないから、そばにいてあげる」[/color]を1層削除",
    "[color=#ff99cc]ターン開始[/color]時、デッキ内の全ての[color=#339966]【[/color][color=#cc9966]裁[/color][color=#6699cc]判[/color][color=#339966]】[/color]エンチャントを削除、削除数と同じ数だけランダムなエンチャント種類を選び、同数の未エンチャントカードに付与し、そのキーワードのカード1枚を生成してそのエンチャントを付けて手札に追加",
    "[color=#ff99cc]ターン開始[/color]時、[color=#CC6666]【疎遠】[/color]を2層消費して手札のカードを[color=#CC6666]【疎遠】[/color]カードに変形、または[color=#ff99cc]【親密】[/color]を2層消費して[color=#ff99cc]【親密】[/color]カードを生成、いずれも無料で1回プレイ可能",
    "[color=#ffcc99]ターン開始[/color]時HPを30回復",
    "[color=CC6666]死亡[/color]時、HP40で復活、その後1層減少",
    "[color=ff99cc]毎ターン[/color][color=#339966]【[/color][color=#cc9966]裁[/color][color=#6699cc]判[/color][color=#339966]】[/color]エンチャントカードを5の倍数プレイする時、HPが最も低い敵に[color=#ff99cc]ダメージ[/color]15",
    "[color=ff99cc]毎ターン開始[/color]時\n[color=#339966]【[/color][color=#cc9966]裁[/color][color=#6699cc]判[/color][color=#339966]】[/color]エンチャントのカウンター+1",
    "[color=ff99cc]毎ターン開始[/color]時、[color=#ff99cc]ドローパイル[/color]の上{Stacks:diff()}枚を確認、捨てるか戻すかを選択できる",
    "[color=ff99cc]選択[/color]",
    "[jitter][color=#CC6666]「この世界で、私は牢獄をさまよう残骸になった————」[/color][/jitter]\n意図を実行するたび、層ごとに20%の確率で同じ意図を1回追加実行",
    "[jitter][color=#cccccc]「醜さの化身——不死の怪物【魔女】となり、果てしない絶望に陥る」[/color][/jitter]\n[color=gray]第三の意図[/color]の攻撃後に全プレイヤーが[color=gray]【変身の魔法】[/color]カードをプレイヤー数の3倍持っていれば、次のターン[color=gray]追加で[/color]1回攻撃\nターン内の最初の死亡を防ぎ、HPが無限になり魔女化の3倍の[b]防御[/b]を獲得して意図を切り替え[color=gray](1ターン1回)[/color]；防御が破られなければ、次のターン開始時に最大HPが戦闘開始時の値に戻り、HP半分回復。",
    "[jitter][color=#cccccc]「第二幕、開幕」[/color][/jitter]\nHPが半分まで減ると、魔女化の2倍の防御を獲得して意図を切り替え。",
    "[jitter][color=#cccccc]「魔女の島が再び空を舞う」[/color][/jitter]\nHPが半分まで減ると魔女化の2倍の盾を獲得し、意図を切り替え",
    "[jitter][color=#cccccc]「理性を失い残骸となったお前は、過去に戻っても皆を殺すだけだ」[/color][/jitter]\n死亡時復活: 全プレイヤー満HP回復、マイナス能力削除；自身は他のマイナス能力を削除、最大HP増加、満HP回復、防御50獲得\n攻撃されてHPを失う時、20%の確率で攻撃者に[b]情報[/b]を1層付与、死亡するたび確率10%増加、毎ターン最大5回発動",
    "[jitter][color=#cccccc]「【魔女】を殺すために、同じ効果の薬を開発したようだ......」[/color][/jitter]\n13層到達時この能力を削除し、味方が[b]サーティーンウォーターズ[/b]を1枚獲得",
    "[purple]このターン[/purple]味方が受けるダメージを肩代わり。\n[purple]次のターン[/purple]守った味方に受けたダメージ分の盾を付与。親密>疎遠なら、あなたも受けたダメージ分の盾を獲得",
    "[purple]ターン開始[/purple]時、プレイ可能な手札カードを1枚ランダムにプレイし、1層削除",
    "「仮死」",
    "「ヒロ本人が泣いているのに、私を責めるなんて」",
    "「真エンディング」",
    "【魔女化】増加時、能力の影響を受けるランダムな対象に同量のダメージ",
    "100層で魔法を獲得",
    "ボスが意図を実行する時、幻影が意図を1つ追加実行",
    "エマの協力",
    "安心",
    "マグの魔法",
    "マークした敵が次にあなたにダメージを与える時、[color=#6666cc]ダメージ[/color]最大{Amount}を[blue]【紅い蝶】[/blue]の回復として記録。すでに[blue]【紅い蝶】[/blue]があれば加算され、なければ[color=#6666cc]次のターン開始[/color]時に回復。",
    "記録されたカードをプレイすると、追加の防御を獲得。",
    "記録されたカードを[color=#6666cc]次のターン[/color]にプレイすると、[color=#99ccff]【安心】[/color]を1層獲得。",
    "この戦闘では魔女化を獲得できず、代わりに嵐を記録。ターン開始時、嵐40を消化するごとにエネルギー1+カード1枚+ランダムな基本感情を獲得",
    "この戦闘で【洗脳反動】はすでに減少済み",
    "この戦闘中、敵の意図が書き換えられるたびに1回記録。\n[color=#6666cc]ターン開始[/color]時、最低3回記録されていれば、記録を最大{Amount}回消費: 同数枚の[color=#6666cc]カード[/color]を引き、同量の{energyPrefix:energyIcons(1)}を獲得",
    "このターン[color=#6666cc]【沈黙】[/color]または[color=#6666cc]【洗脳】[/color]を右クリックまたは2本指タップで無料使用可能。[blue]ターン終了[/blue]時、この無料使用で書き換えた敵に{RewrittenNymPower:diff()}層の[blue]【木ノ下ノアの魔法】[/blue]を付与；書き換えた敵がいなければ全ての敵に{NoRewriteNymPower:diff()}層を付与。",
    "このターンカード1枚プレイごとに感情カウントを追加+1。",
    "このターン、味方がカードを1枚引いて{energyPrefix:energyIcons(1)}を回復するたび、その味方に正義を1層付与",
    "このターン、あなたのいずれかの裁判エンチャントのカウントが5に達すると固定、以降このエンチャントカウントの追加効果を味方も発動可能",
    "このターン{DamageTarget}以上のダメージを与え、{BlockTarget}以上の防御を獲得。\n成功: 防御20獲得。\n失敗: ボスが[b]魔女化[/b]10層と防御20を獲得、タスク失敗を1回記録(失敗1回ごとに次の攻撃が追加ダメージ)",
    "このターン[b]活気[/b]が減少しようとする次の{Amount}回は減少しない。",
    "氷上メルルの魔法",
    "もう逸らさない",
    "無応答",
    "残響",
    "草稿書き換え",
    "解除支援",
    "敵からのダメージ2倍。敵のダメージは味方に半分\n累計攻撃ダメージ20ごとに、自分は次のターンのエネルギー1層、他の味方はそれぞれ次のターンのドロー1層と魔女化10層を獲得",
    "木ノ下ノアの魔法",
    "衝撃波",
    "初生の感情",
    "錯乱した記憶",
]

assert len(jpn_1) == 88, len(jpn_1)
mapping = {texts[i].replace('\n', '\\n'): jpn_1[i].replace('\n', '\\n') for i in range(88)}
with io.open(r'D:\ManosabaLin\map_powers_jpn_1.json', 'w', encoding='utf-8') as f:
    json.dump({"jpn/powers.json": mapping}, f, ensure_ascii=False, indent=2)
print("jpn powers 批次1 完成: 88 条")
