using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FarmingEngine.EditorTool
{

    /// <summary>
    /// Unity检视面板上UniqueID组件的自定义编辑器
    /// </summary>

    [CustomEditor(typeof(UniqueID)), CanEditMultipleObjects]
    public class UniqueIDEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            UniqueID myScript = target as UniqueID; // 获取当前目标对象的UniqueID组件

            DrawDefaultInspector(); // 绘制默认的检视面板

            EditorGUILayout.Space(); // 添加空白间隔

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("生成唯一标识符", style); // 显示标题文本，并应用自定义样式
            EditorGUILayout.LabelField("点击 Farming Engine->Generate UIDs 生成场景中所有空的UID。\n或者点击下面按钮生成当前对象的UID。", GUILayout.Height(50)); // 显示说明文本，并设置高度

            if (GUILayout.Button("生成UID"))
            {
                Undo.RecordObject(myScript, "生成UID"); // 在Undo系统中记录对象的状态，以便撤销操作
                myScript.GenerateUIDEditor(); // 调用对象的生成UID方法
                EditorUtility.SetDirty(myScript); // 标记对象为脏，确保修改被保存
            }

            EditorGUILayout.Space(); // 添加空白间隔
        }

    }

}