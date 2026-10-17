using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace FarmingEngine.EditorTool
{

    /// <summary>
    /// 工具用于将场景中所有选定的对象替换为一个预制体。比逐个替换更快速。
    /// </summary>

    public class ReplacePrefab : ScriptableWizard
    {
        public GameObject NewPrefab; // 新预制体对象

        [MenuItem("Farming Engine/Replace Prefab", priority = 304)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<ReplacePrefab>("Replace Prefabs", "Replace Prefabs");
        }

        /// <summary>
        /// 在向导创建时调用的方法，执行替换操作
        /// </summary>
        void OnWizardCreate()
        {
            if (NewPrefab != null)
            {
                List<GameObject> newObjs = new List<GameObject>();

                foreach (Transform transform in Selection.transforms)
                {
                    // 实例化新预制体对象
                    GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(NewPrefab);
                    Undo.RegisterCreatedObjectUndo(newObject, "created prefab"); // 注册创建操作的撤销

                    // 设置新对象的位置、旋转、缩放和父级
                    newObject.transform.position = transform.position;
                    newObject.transform.rotation = transform.rotation;
                    newObject.transform.localScale = transform.localScale;
                    newObject.transform.parent = transform.parent;
                    newObjs.Add(newObject);

                    // 立即销毁原选定对象
                    Undo.DestroyObjectImmediate(transform.gameObject);
                }

                // 更新选择对象为新创建的对象列表
                Selection.objects = newObjs.ToArray();
            }
        }

        /// <summary>
        /// 在向导更新时调用的方法，显示帮助字符串
        /// </summary>
        void OnWizardUpdate()
        {
            helpString = "使用此工具将场景中所有选定的对象替换为一个预制体。";
        }
    }

}