using UnityEngine;
using UnityEditor;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 工具：将所有选定的对象对齐到整数位置（1, 2），去除小数部分
    /// </summary>
    public class AlignObjects : ScriptableWizard
    {
        [MenuItem("Farming Engine/Align Objects", priority = 301)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<AlignObjects>("AlignObjects", "AlignObjects");
        }

        /// <summary>
        /// 对齐立方体的操作
        /// </summary>
        void DoAlignCubes()
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "align objects");

            foreach (Transform transform in Selection.transforms)
            {
                transform.position = new Vector3(Mathf.RoundToInt(transform.position.x), 0f, Mathf.RoundToInt(transform.position.z));
            }
        }

        /// <summary>
        /// 创建向导时调用的方法，执行对齐操作
        /// </summary>
        void OnWizardCreate()
        {
            DoAlignCubes();
        }

        /// <summary>
        /// 更新向导时显示的帮助信息
        /// </summary>
        void OnWizardUpdate()
        {
            helpString = "使用此工具将所有选定的对象的位置四舍五入到整数位置（去除小数部分）。";
        }
    }
}