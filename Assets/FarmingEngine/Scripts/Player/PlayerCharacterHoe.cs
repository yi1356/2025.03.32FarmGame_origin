using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 玩家角色锄地功能的类
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterHoe : MonoBehaviour
    {
        // 锄头物品的组数据
        public GroupData hoe_item;
        // 锄地建筑物的建造数据
        public ConstructionData hoe_soil;
        // 锄地范围
        public float hoe_range = 1f;
        // 锄地建造半径
        public float hoe_build_radius = 0.5f;
        // 锄地消耗的能量
        public int hoe_energy = 1;

        private PlayerCharacter character;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        // 离开场景时调用的方法
        private void OnDestroy()
        {
            // 留空
        }

        // 开始时调用的方法
        private void Start()
        {
            // 留空
        }

        // 固定帧更新时调用的方法
        void FixedUpdate()
        {
            // 留空
        }

        // 每帧更新时调用的方法
        private void Update()
        {
            // 自动锄地
            if (character.IsAutoMove())//Z：检查是否是自动移动，是就执行自动锄地方法（自动锄地方法中会自动判断是否真的锄地）
            {
                HoeGroundAuto(character.GetAutoMoveTarget());//Z：自动耕种，传入的位置是自动移动终点的位置
            }

            //Z：这个地方好像是手柄控制用的
            PlayerControls control = PlayerControls.Get();
            if (control.IsPressAttack() && character.IsControlsEnabled())
            {
                // 获取锄地位置
                Vector3 hoe_pos = character.GetInteractCenter() + character.GetFacing() * 1f;  
                HoeGround(hoe_pos);
            }
        }


        //TODO：2025.07.22，这里检查建筑是干什么的？
        /// <summary>
        /// Z：锄地方法，真正执行锄地操作（其中判断了锄地范围内障碍物等，决定是否创建土壤模型）
        /// </summary>
        /// <param name="pos">锄地位置</param>
        public void HoeGround(Vector3 pos)
        {
            if (!CanHoe())
                return;

            character.StopMove();
            character.Attributes.AddAttribute(AttributeType.Energy, -hoe_energy);

            // 触发角色动画
            character.TriggerAnim(character.Animation ? character.Animation.hoe_anim : "", pos);
            character.TriggerBusy(0.8f, () =>
            {

                //Z：这一段通过检测是否已经耕种+是否已经种植，来决定是否还原为普通草地
                // 获取最近的建筑和植物
                Construction prev = Construction.GetNearest(pos, hoe_build_radius);
                Plant plant = Plant.GetNearest(pos, hoe_build_radius);

                // 如果存在先前的建筑且没有植物，则销毁它
                if (prev != null && plant == null && prev.data == hoe_soil)
                {
                    prev.Destroy();
                    return;
                }

                // 创建建筑对象（Z：这里是临时建造，像建造时的半透明效果）
                Construction construct = Construction.CreateBuildMode(hoe_soil, pos);
                construct.GetBuildable().StartBuild(character);
                construct.GetBuildable().SetBuildPositionTemporary(pos);

                // 如果可以建造，则完成建造
                if (construct.GetBuildable().CheckIfCanBuild())
                {
                    construct.GetBuildable().FinishBuild();
                }
                else
                {
                    // 否则销毁建筑对象
                    Destroy(construct.gameObject);
                }
            });

        }

        /// <summary>
        /// 检查是否可以进行锄地操作（Z：能量、工具）
        /// </summary>
        /// <returns>是否可以锄地</returns>
        public bool CanHoe()
        {
            bool has_energy = character.Attributes.GetAttributeValue(AttributeType.Energy) >= hoe_energy;
            InventoryItemData ivdata = character.EquipData.GetEquippedItem(EquipSlot.Hand);
            ItemData idata = ItemData.Get(ivdata?.item_id);
            return has_energy && idata != null && idata.HasGroup(hoe_item) && !character.IsBusy();
        }



        /// <summary>
        /// Z：自动锄地方法（其中判断是否装备锄头、是否在锄地范围内、等等来判断是否执行锄地方法）
        /// </summary>
        /// <param name="pos">锄地位置</param>
        public void HoeGroundAuto(Vector3 pos)
        {
            Vector3 dir = pos - transform.position;
            //Z：锄地”，只要满足其中任何一个条件，就不执行自动锄地：正在忙碌、正在建造、超出锄地范围、有目标交互对象、自动移动去丢弃物品、
            if (character.IsBusy() || character.Crafting.ClickedBuild() || dir.magnitude > hoe_range
                || character.GetAutoSelectTarget() != null || character.GetAutoDropInventory() != null)
                return;

            InventoryItemData ivdata = character.EquipData.GetEquippedItem(EquipSlot.Hand);
            if (ivdata != null && CanHoe())//Z：这里会判断是否锄地
            {
                HoeGround(pos);

                // 减少物品耐久度
                if (ivdata != null)
                    ivdata.durability -= 1;
            }
        }
    }
}
