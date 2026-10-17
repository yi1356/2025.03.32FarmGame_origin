using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 清除场景中的所有唯一标识符（UID）
    /// （警告：更改UID将使对象与旧的保存文件不兼容，因为所有UID都会更改，保存文件中的对象通过其UID进行跟踪）。
    /// </summary>
    public class ClearUIDs : ScriptableWizard
    {
        [MenuItem("Farming Engine/Clear UIDs", priority = 201)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<ClearUIDs>("Clear Unique IDs", "Clear All UIDs");
        }

        /// <summary>
        /// 当向导被创建时调用的方法，执行清除UID操作
        /// </summary>
        void OnWizardCreate()
        {
            UniqueID.ClearAll(GameObject.FindObjectsOfType<UniqueID>());

            // 标记场景为已修改，以便保存
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        /// <summary>
        /// 当向导更新时显示的帮助信息
        /// </summary>
        void OnWizardUpdate()
        {
            helpString = "清除场景中的所有唯一标识符（UID）。警告：这将使之前的保存文件不兼容。";
        }
    }
}