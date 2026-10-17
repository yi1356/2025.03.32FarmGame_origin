using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 脚本用于允许玩家骑乘动物
    /// 确保玩家角色有一个独特的层级设置（如Player层）
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterRide : MonoBehaviour
    {
        private PlayerCharacter character;
        private bool is_riding = false;
        private AnimalRide riding_animal = null;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        //Z 仍然是条件和状态判断，决定是否中断骑乘
        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            if (is_riding)// Z 我发现这里都是停止骑乘的逻辑
            {
                // 如果正在骑乘但是骑乘的动物为空或已经死亡，则停止骑乘
                if (riding_animal == null || riding_animal.IsDead())
                {
                    StopRide();
                    return;
                }

                //Z 这里更新玩家位置和角度使其和骑乘动物一致
                // 将玩家位置  实时设置为骑乘动物的骑乘根位置
                transform.position = riding_animal.GetRideRoot();
                // 将玩家朝向设置 实时设置为骑乘动物的前方
                transform.rotation = Quaternion.LookRotation(riding_animal.transform.forward, Vector3.up);

                // 停止骑乘（Z：检测到点击空格会停止骑乘）
                PlayerControls controls = PlayerControls.Get(character.player_id);
                if (character.IsControlsEnabled())
                {
                    if (controls.IsPressJump() || controls.IsPressAction() || controls.IsPressUICancel())
                        StopRide();
                }
            }
        }

        public void RideNearest()
        {
            // 获取最近的可骑乘动物
            AnimalRide animal = AnimalRide.GetNearest(transform.position, 2f);
            RideAnimal(animal);
        }
        /// <summary>
        /// Z：开始骑行（禁止玩家移动和碰撞、玩家位置和马对齐、马会记录现有骑手）[骑乘时，玩家和动物 “绑定” 成一个整体]
        /// </summary>
        /// <param name="animal"></param>
        public void RideAnimal(AnimalRide animal)
        {
            // 如果没有在骑乘，并且角色可以移动，并且动物不为空
            if (!is_riding && character.IsMovementEnabled() && animal != null)
            {
                is_riding = true;
                character.SetBusy(true); // 设置角色忙碌状态
                character.DisableMovement(); // 禁用角色移动（Z：角色自己不能走路了（DisableMovement），也不会和其他物体碰撞（DisableCollider））
                character.DisableCollider(); // 禁用角色碰撞体
                riding_animal = animal; // 设置当前骑乘的动物
                transform.position = animal.GetRideRoot(); // 将玩家位置设置为动物的骑乘根位置（Z：让角色位置和马背对齐）
                animal.SetRider(character); // 设置动物的骑乘者为玩家角色(Z：马会记录 “现在有骑手”)
            }
        }

        public void StopRide()
        {
            if (is_riding)
            {
                if (riding_animal != null)
                    riding_animal.StopRide(); // 停止动物的骑乘
                is_riding = false;
                character.SetBusy(false); // 设置角色非忙碌状态
                character.EnableMovement(); // 启用角色移动
                character.EnableCollider(); // 启用角色碰撞体
                riding_animal = null; // 清空当前骑乘的动物
            }
        }

        public bool IsRiding()
        {
            return is_riding; // 返回当前是否在骑乘状态
        }

        public AnimalRide GetAnimal()
        {
            return riding_animal; // 返回当前骑乘的动物
        }

        public PlayerCharacter GetCharacter()
        {
            return character; // 返回玩家角色组件
        }
    }

}
