using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    public class Door : MonoBehaviour
    {
        private Selectable select; // 可选择对象组件
        private Animator animator; // 动画控制器
        private Collider collide; // 碰撞体

        private bool opened = false; // 是否已打开

        void Start()
        {
            select = GetComponent<Selectable>(); // 获取可选择对象组件
            animator = GetComponentInChildren<Animator>(); // 获取子对象中的动画控制器组件
            collide = GetComponentInChildren<Collider>(); // 获取子对象中的碰撞体组件
            select.onUse += OnUse; // 注册使用事件的监听器
        }

        // 使用门的操作
        void OnUse(PlayerCharacter character)
        {
            opened = !opened; // 切换门的打开状态

            if (collide != null)
                collide.isTrigger = opened; // 设置碰撞体是否为触发器模式，根据门的状态决定

            if (animator != null)
                animator.SetBool("Open", opened); // 设置动画控制器中的开门状态
        }
    }
}