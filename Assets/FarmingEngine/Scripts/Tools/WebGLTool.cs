using System.Runtime.InteropServices;

namespace FarmingEngine
{

    /// <summary>
    /// 如果你在查找 IsMobile 函数时遇到问题，你需要在 Assets/Plugins/WebGL 文件夹中添加一个特殊文件
    /// 你可以在 Discord 的代码分享部分获取该文件，或者请我通过电子邮件发送给你
    /// 插件文件夹未包含在 unitypackage 中，因为它在 FarmingEngine 文件夹之外，所以你需要自己添加该文件。
    /// </summary>

    public class WebGLTool
    {

#if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern bool IsMobile();
#endif

        // 检测当前平台是否为移动设备
        public static bool isMobile()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return IsMobile();
#else
            return false;
#endif
        }

    }

}