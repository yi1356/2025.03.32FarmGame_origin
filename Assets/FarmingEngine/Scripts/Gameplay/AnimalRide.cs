using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 允许玩家骑乘动物的类
    /// </summary>
    
    [RequireComponent(typeof(Character))]
    public class AnimalRide : MonoBehaviour
    {
        public float ride_speed = 5f;  // 骑乘速度
        public Transform ride_root;  // 骑乘根节点
        public bool use_navmesh = true;  // 使用导航网格

        private Character character;  // 角色组件
        private Selectable select;  // 可选择组件
        private Animator animator;  // 动画控制器
        private AnimalWild wild;  // 野生动物组件
        private AnimalLivestock livestock;  // 家畜动物组件
        private float regular_speed;  // 常规移动速度
        private bool default_avoid;  // 默认避开障碍物
        private bool default_navmesh;  // 默认使用导航网格

        private PlayerCharacter rider = null;  // 骑乘的玩家角色

        private static List<AnimalRide> animal_list = new List<AnimalRide>();  // 静态列表，存储所有骑乘动物的实例

        void Awake()
        {
            animal_list.Add(this);  // 添加到骑乘动物列表
            character = GetComponent<Character>();  // 获取角色组件
            select = GetComponent<Selectable>();  // 获取可选择组件
            wild = GetComponent<AnimalWild>();  // 获取野生动物组件
            livestock = GetComponent<AnimalLivestock>();  // 获取家畜动物组件
            animator = GetComponentInChildren<Animator>();  // 获取动画控制器
            regular_speed = character.move_speed;  // 记录常规移动速度
            default_avoid = character.avoid_obstacles;  // 记录默认避开障碍物设置
            default_navmesh = character.use_navmesh;  // 记录默认使用导航网格设置
        }

        private void OnDestroy()
        {
            animal_list.Remove(this);  // 从骑乘动物列表移除

            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            mouse.onClickFloor -= OnClickFloor;
            mouse.onClickObject -= OnClickObject;
            mouse.onHold -= OnMouseHold;
            mouse.onLongClick -= OnLongClick;
            mouse.onRightClick -= OnRightClick;
        }

        private void Start()
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            mouse.onClickFloor += OnClickFloor;//Z： AnimalRide 监听了鼠标点击事件，
            mouse.onClickObject += OnClickObject;
            mouse.onHold += OnMouseHold;
            mouse.onLongClick += OnLongClick;
            mouse.onRightClick += OnRightClick;
            character.onDeath += OnDeath;  // 监听角色死亡事件
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (IsDead())
                return;

            if (rider == null)
                return;


        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (IsDead())
                return;

            if (rider != null)
            {
                //Z: 获取玩家的输入（键盘/WASD或虚拟摇杆）
                PlayerControls controls = PlayerControls.Get(rider.player_id);
                JoystickMobile joystick = JoystickMobile.Get();

                //Z：把玩家输入的方向，转换成符合相机视角的移动方向（比如玩家按“上”，对应相机前方）
                Vector3 cam_move = TheCamera.Get().GetRotation() * controls.GetMove();
                if (joystick != null && joystick.IsActive())
                {
                    Vector2 joy_dir = joystick.GetDir();
                    cam_move = TheCamera.Get().GetRotation() * new Vector3(joy_dir.x, 0f, joy_dir.y);
                }

                // 让动物按照玩家输入的方向移动
                Vector3 tmove = cam_move * ride_speed;
                if (tmove.magnitude > 0.1f)
                    character.DirectMoveToward(tmove);

                //Character stuck
                if (tmove.magnitude < 0.1f && character.IsStuck())
                    character.Stop();
            }

            //Animations
            if (animator.enabled)
            {
                animator.SetBool("Move", IsMoving());
                animator.SetBool("Run", IsMoving());
            }
        }
        /// <summary>
        /// Z：记录骑乘者（）
        /// </summary>
        /// <param name="player"></param>
        public void SetRider(PlayerCharacter player)
        {
            if (rider == null) {
                rider = player;
                character.move_speed = ride_speed;
                character.avoid_obstacles = false;
                character.use_navmesh = use_navmesh;
                character.Stop();
                if (wild != null)
                    wild.enabled = false;
                if (livestock != null)
                    livestock.enabled = false;
            }
        }

        public void StopRide()
        {
            if (rider != null)
            {
                rider = null;
                character.move_speed = regular_speed;
                character.avoid_obstacles = default_avoid;
                character.use_navmesh = default_navmesh;
                StopMove();
                if (wild != null)
                    wild.enabled = true;
                if (livestock != null)
                    livestock.enabled = true;
            }
        }

        public void StopMove()
        {
            character.Stop();
            animator.SetBool("Move", false);
            animator.SetBool("Run", false);
        }

        public void RemoveRider()
        {
            if (rider != null)
            {
                rider.Riding.StopRide();
            }
        }

        //--- on Click

        private void OnClickFloor(Vector3 pos)
        {
            if (rider != null)
            {
                if(rider.interact_type == PlayerInteractBehavior.MoveAndInteract)
                    //Z：玩家点击地面，让动物移动到该位置
                    character.MoveTo(pos);
            }
        }

        private void OnClickObject(Selectable select, Vector3 pos)
        {
            if (rider != null)
            {
                if (rider.interact_type == PlayerInteractBehavior.MoveAndInteract)
                    character.MoveTo(select.transform.position);
            }
        }

        private void OnMouseHold(Vector3 pos)
        {
            if (TheGame.IsMobile())
                return; //On mobile, use joystick instead, no mouse hold

            if (rider != null)
            {
                if (rider.interact_type == PlayerInteractBehavior.MoveAndInteract)
                    character.DirectMoveTo(pos);
            }
        }

        private void OnLongClick(Vector3 pos)
        {
            if (rider != null)
            {
                float diff = (transform.position - pos).magnitude;
                if (diff < 2f)
                {
                    RemoveRider();
                }
            }
        }

        private void OnRightClick(Vector3 pos)
        {
            if (rider != null)
            {
                //RemoveRider();
            }
        }

        void OnDeath()
        {
            animator.SetTrigger("Death");
        }

        public bool IsDead()
        {
            return character.IsDead();
        }

        public bool IsMoving()
        {
            return character.IsMoving();
        }

        public Vector3 GetMove()
        {
            return character.GetMove();
        }

        public Vector3 GetFacing()
        {
            return character.GetFacing();
        }

        public Vector3 GetRideRoot()
        {
            return ride_root != null ? ride_root.position : transform.position;
        }

        public Character GetCharacter()
        {
            return character;
        }

        public static AnimalRide GetNearest(Vector3 pos, float range = 999f)
        {
            float min_dist = range;
            AnimalRide nearest = null;
            foreach (AnimalRide animal in animal_list)
            {
                float dist = (animal.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = animal;
                }
            }
            return nearest;
        }

        public static List<AnimalRide> GetAll()
        {
            return animal_list;
        }
    }

}
