using UnityEngine;
using System.Collections;
using UnityEditor;

namespace FarmingEngine.EditorTool
{

    /// <summary>
    /// 在X和Z轴上为所有选定的对象位置添加随机偏移，使它们看起来更加自然
    /// （在复制大量树木群组时很有用，避免它们的位置模式重复）
    /// </summary>

    public class RandomizeObjects : ScriptableWizard
    {
        public float noise_dist = 1f; // 随机偏移的距离范围

        [MenuItem("Farming Engine/Randomize Objects", priority = 302)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<RandomizeObjects>("Randomize Objects", "Randomize Objects");
        }

        /// <summary>
        /// 执行随机偏移操作
        /// </summary>
        void DoRandomize()
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "randomize"); // 注册撤销操作
            foreach (Transform transform in Selection.transforms)
            {
                DoRandomize(transform); // 对每个选定的对象执行随机偏移操作

                // 如果对象没有Selectable组件，则对其所有子对象也进行随机偏移
                if (!transform.GetComponent<Selectable>())
                {
                    for (int i = 0; i < transform.childCount; i++)
                        DoRandomize(transform.GetChild(i));
                }
            }
        }

        /// <summary>
        /// 执行单个对象的随机偏移操作
        /// </summary>
        /// <param name="transform">要随机偏移的Transform</param>
        void DoRandomize(Transform transform)
        {
            Vector3 offset = new Vector3(Random.Range(-noise_dist, noise_dist), 0f, Random.Range(-noise_dist, noise_dist)); // 在X和Z轴上生成随机偏移向量
            transform.position += offset; // 应用随机偏移
        }

        /// <summary>
        /// 在向导创建时调用的方法，执行随机偏移操作
        /// </summary>
        void OnWizardCreate()
        {
            DoRandomize();
        }

        /// <summary>
        /// 在向导更新时调用的方法，显示帮助字符串
        /// </summary>
        void OnWizardUpdate()
        {
            helpString = "使用此工具为所有选定的对象的位置添加随机偏移。";
        }
    }

}