using UnityEngine;
using System.Collections;
using UnityEditor;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 一次性按照精确值移动所有选定的对象（而不是逐个移动，或者使用工具进行非精确值移动）
    /// </summary>

    public class MoveObjects : ScriptableWizard
    {
        public Vector3 move; // 移动的向量
        public Vector3 rotate; // 旋转的角度

        [MenuItem("Farming Engine/Transform Group", priority = 300)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<MoveObjects>("Transform Group", "Transform Group");
        }

        /// <summary>
        /// 移动对象的方法
        /// </summary>
        /// <param name="obj">要移动的对象的Transform</param>
        /// <param name="move_vect">移动的向量</param>
        void MoveObject(Transform obj, Vector3 move_vect)
        {
            obj.position += move_vect; // 修改位置
            obj.rotation = obj.rotation * Quaternion.Euler(rotate); // 修改旋转
        }

        /// <summary>
        /// 在向导创建时调用的方法，用于移动所有选定的对象
        /// </summary>
        void OnWizardCreate()
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "move objects"); // 注册撤销操作
            foreach (Transform transform in Selection.transforms)
            {
                MoveObject(transform, move); // 对每个选定的对象执行移动操作
            }
        }

        /// <summary>
        /// 在向导更新时调用的方法，显示帮助字符串
        /// </summary>
        void OnWizardUpdate()
        {
            helpString = "使用此工具按精确值移动所有选定的对象。";
        }
    }

}