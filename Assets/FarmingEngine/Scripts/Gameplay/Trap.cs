using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 当动物触发时会造成伤害
    /// </summary>
    public class Trap : MonoBehaviour
    {
        public int damage = 50;                 // 伤害值
        public GroupData target_group;          // 如果设置，只会捕捉该组动物；如果未设置，会捕捉所有角色

        public GameObject active_model;         // 激活时的模型
        public GameObject triggered_model;      // 触发时的模型

        private Construction construct;         // 建造组件
        private Buildable buildable;            // 可建造组件
        private bool triggered = false;         // 是否已触发
        private float trigger_timer = 0f;       // 触发计时器

        void Start()
        {
            active_model.SetActive(true);       // 激活激活时的模型
            triggered_model.SetActive(false);   // 禁用触发时的模型
            construct = GetComponent<Construction>(); // 获取建造组件
            buildable = GetComponent<Buildable>();   // 获取可建造组件
        }

        void Update()
        {
            trigger_timer += Time.deltaTime;    // 更新触发计时器
        }

        // 触发陷阱，关闭陷阱并造成伤害给触发者
        public void Trigger(Character triggerer)
        {
            if (buildable != null && buildable.IsBuilding()) // 如果正在建造中，直接返回
                return;

            if (!triggered && trigger_timer > 2f)   // 如果未触发且触发计时器超过2秒
            {
                triggered = true;   // 设置为已触发
                active_model.SetActive(false);      // 禁用激活时的模型
                triggered_model.SetActive(true);    // 激活触发时的模型

                // 耐久度处理
                if (construct != null)  // 如果有建造组件
                {
                    BuiltConstructionData bdata = PlayerData.Get().GetConstructed(construct.GetUID()); // 获取已建造的数据
                    if (bdata != null && construct.data != null && construct.data.durability_type == DurabilityType.UsageCount) // 如果存在数据且耐久度类型为使用次数
                        bdata.durability -= 1f; // 减少耐久度
                }

                // 造成伤害
                if (triggerer != null)
                    triggerer.GetDestructible().TakeDamage(damage); // 造成伤害给触发者
            }
        }

        // 激活陷阱，打开陷阱，准备触发
        public void Activate()
        {
            if (triggered)  // 如果已经触发
            {
                triggered = false;  // 设置为未触发
                active_model.SetActive(true);       // 激活激活时的模型
                triggered_model.SetActive(false);   // 禁用触发时的模型
                trigger_timer = 0f;  // 重置触发计时器
            }
        }

        // 是否激活状态
        public bool IsActive()
        {
            return !triggered;  // 返回是否未触发状态
        }

        // 触发器进入触发范围
        private void OnTriggerEnter(Collider other)
        {
            if (!triggered) // 如果未触发
            {
                Character character = other.GetComponent<Character>(); // 获取角色组件
                if (character != null)  // 如果获取到角色组件
                {
                    if (target_group == null || character.HasGroup(target_group)) // 如果目标组为空或者角色拥有目标组
                        Trigger(character); // 触发陷阱
                }
            }
        }
    }
}
