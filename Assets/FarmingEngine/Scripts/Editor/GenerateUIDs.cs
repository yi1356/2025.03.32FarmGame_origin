using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 在场景中生成所有空的唯一标识符（Unique IDs）。在添加新对象后使用此工具，以确保它们都有唯一标识符。
    /// 这也会查找重复的唯一标识符并用新的唯一标识符替换它们。它将保持没有重复的唯一标识符不变。
    /// </summary>

    public class GenerateUIDs : ScriptableWizard
    {
        [MenuItem("Farming Engine/Generate UIDs", priority = 200)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<GenerateUIDs>("Generate Unique IDs", "Generate All UIDs");
        }

        void OnWizardCreate()
        {
            UniqueID.GenerateAll(GameObject.FindObjectsOfType<UniqueID>());

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); // 标记场景为已修改状态
        }

        void OnWizardUpdate()
        {
            helpString = "Fill all empty UID in the scene with a random UID."; // 使用此工具在场景中填充所有空的唯一标识符（UID）。
        }
    }

}