using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 优化渲染和应用视觉效果
    /// </summary>

    public class TheRender : MonoBehaviour
    {
        private Light dir_light; // 方向光
        private Quaternion start_rot; // 记录初始旋转
        private float update_timer = 0f; // 更新计时器

        void Start()
        {
            // 初始化光源
            GameData gdata = GameData.Get();
            bool is_night = TheGame.Get().IsNight(); // 判断是否为夜晚
            dir_light = GetDirectionalLight(); // 获取方向光

            float target = is_night ? gdata.night_light_ambient_intensity : gdata.day_light_ambient_intensity; // 目标环境光强度
            float light_angle = PlayerData.Get().day_time * 360f / 24f; // 根据时间计算光照角度
            RenderSettings.ambientIntensity = target; // 设置环境光强度
            if (dir_light != null && dir_light.type == LightType.Directional)
            {
                start_rot = dir_light.transform.rotation; // 记录方向光的初始旋转
                dir_light.intensity = is_night ? gdata.night_light_dir_intensity : gdata.day_light_dir_intensity; // 设置方向光强度
                dir_light.shadowStrength = is_night ? 0f : 1f; // 设置阴影强度
                if (gdata.rotate_shadows)
                    dir_light.transform.rotation = Quaternion.Euler(0f, light_angle + 180f, 0f) * start_rot; // 旋转方向光
            }
        }

        void Update()
        {
            // 更新昼夜变化
            GameData gdata = GameData.Get();
            bool is_night = TheGame.Get().IsNight(); // 判断是否为夜晚
            float light_mult = GetLightMult(); // 获取光照倍率
            float target = is_night ? gdata.night_light_ambient_intensity : gdata.day_light_ambient_intensity; // 目标环境光强度
            float light_angle = PlayerData.Get().day_time * 360f / 24f; // 根据时间计算光照角度
            RenderSettings.ambientIntensity = Mathf.MoveTowards(RenderSettings.ambientIntensity, target * light_mult, 0.2f * Time.deltaTime); // 平滑过渡环境光强度
            if (dir_light != null && dir_light.type == LightType.Directional)
            {
                float dtarget = is_night ? gdata.night_light_dir_intensity : gdata.day_light_dir_intensity; // 目标方向光强度
                dir_light.intensity = Mathf.MoveTowards(dir_light.intensity, dtarget * light_mult, 0.2f * Time.deltaTime); // 平滑过渡方向光强度
                dir_light.shadowStrength = Mathf.MoveTowards(dir_light.shadowStrength, is_night ? 0f : 1f, 0.2f * Time.deltaTime); // 平滑过渡阴影强度
                if (gdata.rotate_shadows)
                    dir_light.transform.rotation = Quaternion.Euler(0f, light_angle + 180f, 0f) * start_rot; // 旋转方向光
            }

            // 慢速更新
            update_timer += Time.deltaTime; // 增加计时器
            if (update_timer > GameData.Get().optim_refresh_rate)
            {
                update_timer = 0f; // 重置计时器
                SlowUpdate(); // 执行慢速更新
            }
        }

        void SlowUpdate()
        {
            // 优化循环
            Vector3 center_pos = TheCamera.Get().GetTargetPosOffsetFace(GameData.Get().optim_facing_offset); // 获取摄像机中心位置
            float dist_mult = GameData.Get().optim_distance_multiplier; // 获取距离乘数
            bool turn_off_obj = GameData.Get().optim_turn_off_gameobjects; // 是否关闭游戏对象
            List<Selectable> selectables = Selectable.GetAll(); // 获取所有可选择对象

            foreach (Selectable select in selectables)
            {
                float dist = (select.GetPosition() - center_pos).magnitude; // 计算对象与中心的距离
                select.SetActive(dist < select.active_range * dist_mult, turn_off_obj); // 设置对象的激活状态
            }
        }

        public Light GetDirectionalLight()
        {
            // 获取场景中的方向光
            foreach (Light light in FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional)
                    return light;
            }
            return null;
        }

        public float GetLightMult()
        {
            // 获取光照倍率
            if (WeatherSystem.Get())
                return WeatherSystem.Get().GetLightMult();
            return 1f;
        }
    }

}
