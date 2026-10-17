using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 切割植物并使其返回生长阶段 0，并获得物品（例如切割草）
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/CutPlant", order = 50)]
    public class ActionCutPlant : AAction
    {
        // 执行切割植物操作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>(); // 获取植物组件
            if (plant != null)
            {
                string animation = character.Animation ? character.Animation.take_anim : ""; // 获取角色的采摘动画名称
                character.TriggerAnim(animation, plant.transform.position); // 触发角色的采摘动画，并传入植物位置
                character.TriggerBusy(0.5f, () =>//Z 这里有点意思，执行到这里会设置为basy状态直到行为执行完毕，这里的0.5f是指0.5f后行为执行完毕（一般需要看下动画需要多少时间）
                {
                    plant.GrowPlant(0); // 将植物生长阶段设为 0

                    Destructible destruct = plant.GetDestructible(); // 获取植物的可破坏组件
                    TheAudio.Get().PlaySFX("destruct", destruct.death_sound); // 播放破坏音效

                    destruct.SpawnLoots(); // 生成掉落物品
                });
            }
        }

        // 判断是否可以进行切割植物操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select.GetComponent<Plant>(); // 只有当选择对象拥有植物组件时才能执行操作
        }
    }

}