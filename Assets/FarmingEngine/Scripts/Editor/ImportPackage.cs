using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// 在导入时加载第三方包并进行集成
    /// </summary>

    [InitializeOnLoad]
    public class ImportPackage
    {
        static bool completed = false;

        static ImportPackage()
        {
            AfterCompile();
        }

        static void AfterCompile()
        {
            if (!completed)
            {
                completed = true;

                // 检查地板层
                string floorLayer = LayerMask.LayerToName(9);
                if (string.IsNullOrEmpty(floorLayer))
                {
                    Debug.LogWarning("Farming Engine: 建议为地板层分配一个名称。层级: 9 名称: Floor");
                }

                // 添加FARMING_ENGINE符号
                string symbolSE = "FARMING_ENGINE";
                if (!HasSymbol(symbolSE))
                {
                    AddSymbol(symbolSE);
                }

                // 加载InControl
                string symbolIC = "IN_CONTROL";
                bool hasInControl = DoNamespaceExist("InControl", "InControl");
                if (hasInControl && !HasSymbol(symbolIC))
                {
                    AddSymbol(symbolIC);
                }
            }
        }

        private static bool DoNamespaceExist(string assembly_name, string namespace_name)
        {
            System.Reflection.Assembly[] list = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (System.Reflection.Assembly assembly in list)
            {
                if (assembly.GetName().Name == assembly_name)
                {
                    System.Type[] types = assembly.GetTypes(); 
                    foreach (System.Type type in types)
                    {
                        if (type.Namespace == namespace_name)
                            return true;
                    }
                }
            }
            return false;
        }

        private static void AddSymbol(string symbol)
        {
            BuildTargetGroup build_group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string defines_string = PlayerSettings.GetScriptingDefineSymbolsForGroup(build_group);
            List<string> all_defines = defines_string.Split(';').ToList();
            string[] symbols = new string[] { symbol };
            all_defines.AddRange(symbols.Except(all_defines));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(build_group, string.Join(";", all_defines.ToArray()));
            Debug.Log("已将 " + symbol + " 添加到脚本定义符号中");
        }

        private static bool HasSymbol(string symbol)
        {
            BuildTargetGroup build_group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(build_group);
            List<string> allDefines = definesString.Split(';').ToList();
            return allDefines.Contains(symbol);
        }
    }
}
