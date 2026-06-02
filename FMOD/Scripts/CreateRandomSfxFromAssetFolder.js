/* ---------------------------------------------------------
   从选中的 Assets 文件夹自动创建对应的随机 MultiSound SFX Event
   --------------------------------------------------------- */

const ModId = "ManosabaLin";

function createRandomSfxFromFolders() {
    const route = "sfx"; // 强制指定 route 为 sfx
    const selectedItems = studio.window.browserSelection();

    // 1. 【核心破局点】获取项目中所有的音频文件
    // 后面通过对比它们的 assetPath 来判定目录的层级归属，完全绕开 .items 属性
    const allAudioFiles = studio.project.model.AudioFile.findInstances();
    const validFolders = [];

    selectedItems.forEach(function(item) {
        // 如果用户不小心选到了单个音频文件，直接过滤掉
        if (item.isOfExactType("AudioFile")) {
            return;
        }

        const selectedPath = item.assetPath;
        if (!selectedPath) return;

        const prefix = selectedPath;
        let directAudioFiles = [];
        let hasSubFolder = false;

        // 2. 遍历全量音频，寻找属于当前选中路径的文件
        allAudioFiles.forEach(function(audioFile) {
            const afPath = audioFile.assetPath;
            if (afPath && afPath.indexOf(prefix) === 0) {
                // 说明此音频存在于该文件夹（或其深层子文件夹）中
                const relativePath = afPath.substring(prefix.length);

                if (relativePath.indexOf("/") !== -1) {
                    // 如果裁剪掉当前路径后，剩下的路径里还包含 "/"，说明它在更深的子文件夹里
                    hasSubFolder = true;
                } else {
                    // 没有 "/"，说明是正前文件夹直属的音频文件
                    directAudioFiles.push(audioFile);
                }
            }
        });

        // 3. 【条件判定】只有直属音频数量 > 0，且绝不包含子文件夹内音频的，才算有效文件夹
        if (directAudioFiles.length > 0 && !hasSubFolder) {
            validFolders.push({
                assetPath: selectedPath,
                audioFiles: directAudioFiles
            });
        } else if (hasSubFolder) {
            console.warn("【忽略文件夹】因包含子文件夹，不符合“有且只有音频”条件: " + selectedPath);
        } else {
            console.warn("【忽略文件夹】未在目录下找到任何有效音频: " + selectedPath);
        }
    });

    if (validFolders.length === 0) {
        alert("未找到符合条件的 Assets 文件夹！\n【有效条件】：选中的文件夹下直属必须有音频，且不能嵌套子文件夹。");
        return;
    }

    // 4. 路径与配置映射（严格与你给的示例保持完全一致）
    const baseFolderPath = "event:/" + ModId + "/" + route;
    const targetBusPath = "bus:/master/" + route;
    const targetBankPath = "bank:/" + ModId;

    const baseFolder = studio.project.lookup(baseFolderPath);
    const targetBus = studio.project.lookup(targetBusPath);
    const targetBank = studio.project.lookup(targetBankPath);

    if (!baseFolder) {
        alert("找不到基础事件文件夹: " + baseFolderPath + "\n请先在 Events 中手动建好。");
        return;
    }
    if (!targetBank) {
        alert("找不到指定的 Bank: " + targetBankPath + "\n请先在 Banks 视图中创建。");
        return;
    }

    let createdCount = 0;

    // 5. 遍历验证通过的文件夹数据，开始创建 Event
    validFolders.forEach(function(folderData) {
        let assetPath = folderData.assetPath.slice(0, -1);
        assetPath = assetPath.replace(/\\/g, "/");

        const pathParts = assetPath.split("/");

        // 【剔除重名的顶层目录】例如："sfx/example/attack" -> "example/attack"
        if (pathParts.length > 0 && pathParts[0].toLowerCase() === route.toLowerCase()) {
            pathParts.shift();
        }

        if (pathParts.length === 0) return;

        // 路径数组的最后一项作为 Event 的名字（例如 attack）
        const eventName = pathParts.pop();

        // 6. 镜像生成多级事件文件夹
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

        // 7. 查重
        let eventExists = false;
        currentFolder.items.forEach(function(item) {
            if (item.isOfExactType("Event") && item.name === eventName) {
                eventExists = true;
            }
        });

        if (eventExists) {
            console.warn("事件已存在，跳过: " + currentFolder.name + "/" + eventName);
            return;
        }

        // 8. 创建 Event 并关联 Bus 和 Bank
        const newEvent = studio.project.create("Event");
        newEvent.name = eventName;
        newEvent.folder = currentFolder;

        if (targetBus) {
            newEvent.mixerInput.output = targetBus;
        }
        if (targetBank) {
            newEvent.relationships.banks.add(targetBank);
        }

        // 9. 创建音轨，并计算组内最长音频作为 MultiSound 在时间轴上的切片长度
        const track = newEvent.addGroupTrack();
        let maxLength = 1.0;
        folderData.audioFiles.forEach(function(af) {
            if (af.length > maxLength) {
                maxLength = af.length;
            }
        });

        // 10. 【核心逻辑】在时间轴创建 MultiSound 容器
        const multiSound = track.addSound(newEvent.timeline, "MultiSound", 0, maxLength);

        // 11. 将选中的所有音频作为子乐器注入到 MultiSound 容器内
        folderData.audioFiles.forEach(function(audioFile) {
            // 根据 FMOD 官方 API 规范：往 MultiSound 里放音频，
            // 需要创建 SingleSound 实例，并将其 owner 属性指向该 MultiSound
            const singleSound = studio.project.create("SingleSound");
            singleSound.audioFile = audioFile;
            singleSound.owner = multiSound;
        });

        console.log("成功创建随机 SFX 事件: " + newEvent.getPath());
        createdCount++;
    });

    // alert("批量创建完毕！\n共成功创建了 " + createdCount + " 个随机播放 SFX 事件。");
}

/* ---------------------------------------------------------
   菜单注册
   --------------------------------------------------------- */
studio.menu.addMenuItem({
    name: "选中 Assets 文件夹创建随机 SFX",
    keySequence: "Ctrl+Alt+D",
    execute: function() { createRandomSfxFromFolders(); }
});
