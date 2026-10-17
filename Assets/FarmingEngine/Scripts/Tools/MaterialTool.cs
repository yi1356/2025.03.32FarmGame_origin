using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
    }
    
    /// <summary>
    /// 材质工具类，用于修改材质的渲染模式
    /// 仅适用于标准着色器（STANDARD shaders），如果你使用其他着色器，可能需要添加另一个函数来执行相同的操作
    /// </summary>
    public class MaterialTool
    {
        /// <summary>
        /// 修改材质的渲染模式
        /// </summary>
        /// <param name="standardShaderMaterial">标准着色器材质</param>
        /// <param name="blendMode">混合模式</param>
        public static void ChangeRenderMode(Material standardShaderMaterial, BlendMode blendMode)
        {
            if (IsStandard(standardShaderMaterial))
            {
                switch (blendMode)
                {
                    case BlendMode.Opaque:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        standardShaderMaterial.SetInt("_ZWrite", 1);
                        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = -1;
                        break;
                    case BlendMode.Cutout:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        standardShaderMaterial.SetInt("_ZWrite", 1);
                        standardShaderMaterial.EnableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = 2450;
                        break;
                    case BlendMode.Fade:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        standardShaderMaterial.SetInt("_ZWrite", 0);
                        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.EnableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = 3000;
                        break;
                    case BlendMode.Transparent:
                        standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        standardShaderMaterial.SetInt("_ZWrite", 0);
                        standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                        standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                        standardShaderMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        standardShaderMaterial.renderQueue = 3000;
                        break;
                }
            }
        }

        /// <summary>
        /// 检查材质是否为标准着色器
        /// </summary>
        /// <param name="material">待检查的材质</param>
        /// <returns>如果是标准着色器返回 true，否则返回 false</returns>
        public static bool IsStandard(Material material)
        {
            return material != null && material.HasProperty("_Color")
                && material.HasProperty("_MainTex")
                && material.HasProperty("_SrcBlend")
                && material.HasProperty("_DstBlend")
                && material.HasProperty("_ZWrite");
        }

        /// <summary>
        /// 检查材质是否包含颜色属性
        /// </summary>
        /// <param name="material">待检查的材质</param>
        /// <returns>如果包含颜色属性返回 true，否则返回 false</returns>
        public static bool HasColor(Material material)
        {
            return material != null && material.HasProperty("_Color");
        }
    }
}
