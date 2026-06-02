/* ---------------------------------------------------------
   从选中的 Assets 自动创建对应的 Event
   --------------------------------------------------------- */

const ModId = "ManosabaLin"

// 核心函数：接受 route 参数 ("sfx" 或 "music")
function createEventsFromAssets(route) {
    const selectedItems = studio.window.browserSelection();
    const audioFiles = [];

    // 1. 过滤出真正的音频文件
    selectedItems.forEach(function(item) {
        if (item.isOfExactType("AudioFile")) {
            audioFiles.push(item);
        }
    });

    if (audioFiles.length === 0) {
        alert("请先在 Assets 窗口中选中至少一个音频文件！");
        return;
    }

    // 2. 根据传入的 route 动态拼装路径
    const baseFolderPath = "event:/" + ModId + "/" + route;
    const targetBusPath = "bus:/master/" + route;
    const targetBankPath = "bank:/" + ModId;

    const baseFolder = studio.project.lookup(baseFolderPath);
    const targetBus = studio.project.lookup(targetBusPath);
    const targetBank = studio.project.lookup(targetBankPath);

    // 路径合法性检查
    if (!baseFolder) {
        alert("找不到基础事件文件夹: " + baseFolderPath + "\n请先在 Events 中手动建好。");
        return;
    }
    if (!targetBank) {
        alert("找不到指定的 Bank: " + targetBankPath + "\n请先在 Banks 视图中创建。");
        return;
    }

    let createdCount = 0;

    // 3. 遍历并创建
    audioFiles.forEach(function(audioFile) {
        let assetPath = audioFile.assetPath;
        if (!assetPath) return;

        assetPath = assetPath.replace(/\\/g, "/");

        const pathParts = assetPath.split("/");
        const fileNameExt = pathParts.pop();
        let fileName = fileNameExt;
        const dotIndex = fileNameExt.lastIndexOf('.');
        if (dotIndex > 0) {
            fileName = fileNameExt.substring(0, dotIndex);
        }

        // 【新增的核心逻辑：剔除重名的顶层目录】
        // 例如：assetPath 是 "music/a/b/c.mp3"，此时 pathParts 里面是 ["music", "a", "b"]
        // 如果第一项是 "music"，和我们传入的 route ("music") 一样，就把它剔除掉
        if (pathParts.length > 0 && pathParts[0].toLowerCase() === route.toLowerCase()) {
            pathParts.shift(); // 删除并返回数组的第一个元素
        }

        // 4. 镜像生成多级文件夹
        let currentFolder = baseFolder;
        pathParts.forEach(function(part) {
            if (!part) return;
            let found = false;
            currentFolder.items.forEach(function(item) {
                if (item.isOfExactType("EventFolder") && item.name === part) {
                    currentFolder = item;
                    found = true;
                }
            });
            if (!found) {
                const newFolder = studio.project.create("EventFolder");
                newFolder.name = part;
                newFolder.folder = currentFolder;
                currentFolder = newFolder;
            }
        });

        // 5. 查重
        let eventExists = false;
        currentFolder.items.forEach(function(item) {
            if (item.isOfExactType("Event") && item.name === fileName) {
                eventExists = true;
            }
        });

        if (eventExists) {
            console.warn("事件已存在，跳过: " + currentFolder.name + "/" + fileName);
            return;
        }

        // 6. 创建 Event 并关联 Bus 和 Bank
        const newEvent = studio.project.create("Event");
        newEvent.name = fileName;
        newEvent.folder = currentFolder;

        if (targetBus) {
            newEvent.mixerInput.output = targetBus;
        }
        if (targetBank) {
            newEvent.relationships.banks.add(targetBank);
        }

        // 7. 配置音频轨道和乐器 (Instrument)
        const track = newEvent.addGroupTrack();
        // 直接将 instrument 长度设为底层音频的总长
        const singleSound = track.addSound(newEvent.timeline, "SingleSound", 0, audioFile.length);
        singleSound.audioFile = audioFile;

        // 8. 【核心新增】如果当前路线是 Music，则为其添加等长的 Loop Region
        if (route === "music") {
            // FMOD 默认会为新事件生成至少一个 MarkerTrack
            const markerTrack = newEvent.markerTracks[0];

            if (markerTrack) {
                // 根据 FMOD 官方 API 规范实例化 LoopRegion 对象
                const loopRegion = studio.project.create("LoopRegion");
                loopRegion.position = 0;                        // 放在 0 秒开始
                loopRegion.length = audioFile.length;           // 长度对齐音频文件长度
                loopRegion.timeline = newEvent.timeline;        // 绑定到事件的时间轴
                loopRegion.markerTrack = markerTrack;           // 挂载到标记轨道上
            } else {
                console.warn("创建 Loop Region 失败，未找到 MarkerTrack: " + newEvent.name);
            }
        }

        console.log("成功创建 [" + route.toUpperCase() + "] 事件: " + newEvent.getPath());
        createdCount++;
    });

    // alert("批量创建完毕！\n共成功创建了 " + createdCount + " 个 " + route.toUpperCase() + " 事件。");
}

/* ---------------------------------------------------------
   菜单注册
   --------------------------------------------------------- */

// 注册 SFX 菜单项
studio.menu.addMenuItem({
    name: "选中 Assets 创建 SFX (单次播放)",
    keySequence: "Ctrl+Alt+A",
    execute: function() { createEventsFromAssets("sfx"); }
});

// 注册 Music 菜单项
studio.menu.addMenuItem({
    name: "选中 Assets 创建 BGM (无限循环)",
    keySequence: "Ctrl+Alt+S",
    execute: function() { createEventsFromAssets("music"); }
});
