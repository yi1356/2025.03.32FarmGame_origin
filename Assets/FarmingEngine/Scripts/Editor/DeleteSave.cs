using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 检查是否可以应用任何自动修复，以解决由于更改资产版本可能导致的问题
    /// </summary>

    public class DeleteSave : ScriptableWizard
    {
        [MenuItem("Farming Engine/Delete Save File", priority = 350)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<DeleteSave>("Delete Save File", "Delete");
        }

        void OnWizardCreate()
        {
            // 删除最新的保存文件
            PlayerData.Delete(PlayerData.GetLastSave());
        }

        void OnWizardUpdate()
        {
            helpString = "使用此工具删除最新的保存文件。";
        }
    }
}