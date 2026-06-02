/* ---------------------------------------------------------
    自动按照事件路径进行路由事件
   --------------------------------------------------------- */

const ModId = "ManosabaLin"
const mappings = [
    {folderPath: "event:/" + ModId + "/music", busPath: "bus:/master/music"},
    {folderPath: "event:/" + ModId + "/sfx", busPath: "bus:/master/sfx"},
    {folderPath: "event:/" + ModId + "/ambience", busPath: "bus:/master/ambience"}
];

function autoRouteEvents() {
    let successCount = 0;

    mappings.forEach(function(map) {
        const sourceFolder = studio.project.lookup(map.folderPath);
        const targetBus = studio.project.lookup(map.busPath);

        if (!sourceFolder) {
            console.error("找不到文件夹: " + map.folderPath);
            return;
        }
        if (!targetBus) {
            console.error("找不到总线: " + map.busPath);
            return;
        }

        console.log("开始处理: " + map.folderPath + " -> " + map.busPath);

        // 递归处理函数
        function processItems(folder) {
            folder.items.forEach(function(item) {
                if (item.isOfExactType("Event")) {

                    item.mixerInput.output = targetBus;

                    successCount++;

                    console.log("成功路由: " + item.getPath() + " -> " + targetBus.getPath());

                } else if (item.isOfExactType("Folder")) {
                    processItems(item);
                }
            });
        }

        processItems(sourceFolder);
    });

    // alert("自动路由完成！共成功路由了 " + successCount + " 个事件\n详细日志请查看 Console (Ctrl+0)。");

}

// 注册菜单
studio.menu.addMenuItem({
    name: "自动路由事件",
    keySequence: "Ctrl+Alt+R",
    execute: autoRouteEvents
});
