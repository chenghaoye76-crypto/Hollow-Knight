using System.Collections.Generic;

// Scope is the options UI; gameplay can use the same language preference independently.
public static class OptionsText
{
    private static readonly Dictionary<string, string> Chinese = new()
    {
        { "Options", "选项" }, { "Game", "游戏" }, { "Game Options", "游戏选项" },
        { "Audio", "音频" }, { "Video", "视频" }, { "Keyboard", "键盘" }, { "Controller", "控制器" },
        { "Reset Defaults", "重置为默认值" }, { "Back", "返回" }, { "On", "开启" }, { "Off", "关闭" },
        { "Master Volume", "主音量" }, { "Sound Volume", "音效音量" }, { "Music Volume", "音乐音量" },
        { "Language", "语言" }, { "Camera Shake", "镜头震动" }, { "HUD Appearance", "界面大小" },
        { "Small", "小" }, { "Large", "大" }, { "Show Achievements", "显示成就" }, { "Backer Credits", "支持者名单" },
        { "Resolution", "分辨率" }, { "Full Screen", "全屏" }, { "V-Sync", "垂直同步" }, { "Frame Rate Cap", "帧率上限" },
        { "Screen Scale", "屏幕缩放" }, { "ScreenScale", "屏幕缩放" }, { "Brightness", "亮度" }, { "Advanced Settings", "高级设置" },
        { "Unlimited", "无限制" }, { "Quality", "画质" }, { "Anti-Aliasing", "抗锯齿" }, { "Run in Background", "后台运行" },
        { "Left", "向左" }, { "Right", "向右" }, { "Up", "向上" }, { "Down", "向下" }, { "Jump", "跳跃" },
        { "Attack", "攻击" }, { "Dash", "冲刺" }, { "Focus / Cast", "聚集 / 施法" }, { "Quick Map", "快速地图" },
        { "Super Dash", "超级冲刺" }, { "Dream Nail", "梦之钉" }, { "Quick Cast", "快速施法" }, { "Inventory", "物品栏" },
        { "No Active Controller", "没有激活的控制器" },
        { "Press a button on the controller or connect a new controller to activate", "按下控制器上的按键，或连接新的控制器以激活" },
        { "Vibration Test", "震动测试" }, { "Press a key. Esc to cancel.", "请按下新按键。按 Esc 取消。" },
        { "That key is already assigned.", "该按键已被使用，请选择其他按键。" },
        { "Show in-game messages when earning an achievement.", "获得成就时显示游戏内提示。" },
        { "Limit maximum frame rate for a consistent experience.", "限制最高帧率以获得稳定的游戏体验。" }
    };
    public static string Get(string text) => !string.IsNullOrEmpty(text) && GameOptions.Chinese && Chinese.TryGetValue(text, out string value) ? value : text;
}
