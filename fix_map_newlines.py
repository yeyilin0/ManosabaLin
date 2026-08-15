# -*- coding: utf-8 -*-
import json, io

# 读取已生成的映射，修复多行 key（真实换行 -> \n 字面量）
with io.open(r'D:\ManosabaLin\map_ancients_eng.json', encoding='utf-8') as f:
    data = json.load(f)

fixed = {}
for zh, en in data['eng/ancients.json'].items():
    # JSON 文件中值内的换行是 \n 字面量（json.dump 已处理 en 侧），
    # 但 key 侧 zhs 原文来自文本文件用的是真实换行。
    # PowerShell 读取 JSON 后 key 会是真实换行还是 \n？取决于 json.dump。
    # 实际 json.dump 会把 key 中的真实换行写成 \n 字面量，读取后还原为真实换行。
    # 而 JSON 文件内部存储的值是 \n 字面量（json.load 还原为真实换行）。
    # 因此两侧都是真实换行，应能匹配。问题在于匹配时正则用 . 不匹配换行？
    # 不会，我们用的是 (?:escZh) 明确文本。真正问题：PowerShell JSON 解析后
    # 字符串是真实换行，与文件中的 \n 字面量不同！文件存储是 \n 两字符。
    # 所以我们需要生成 key 为 \n 字面量（两字符）的映射。
    fixed[zh.replace('\n', '\\n')] = en

# 同时把 en 侧的真实换行转成 \n 字面量（因为文件里就是 \n 字面量）
fixed2 = {}
for zh, en in fixed.items():
    fixed2[zh] = en.replace('\n', '\\n')

with io.open(r'D:\ManosabaLin\map_ancients_eng_v2.json', 'w', encoding='utf-8') as f:
    json.dump({"eng/ancients.json": fixed2}, f, ensure_ascii=False, indent=2)
print("生成 map_ancients_eng_v2.json:", len(fixed2))
