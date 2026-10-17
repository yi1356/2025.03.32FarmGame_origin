using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmingEngine
{

    // 管理场景之间转换的脚本
    public class SceneNav
    {
        // 重新加载当前场景
        public static void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // 转到指定场景
        public static void GoTo(string scene)
        {
            SceneManager.LoadScene(scene);
        }

        // 获取当前场景的名称
        public static string GetCurrentScene()
        {
            return SceneManager.GetActiveScene().name;
        }

        // 检查指定场景是否存在
        public static bool DoSceneExist(string scene)
        {
            return Application.CanStreamedLevelBeLoaded(scene);
        }
    }

}