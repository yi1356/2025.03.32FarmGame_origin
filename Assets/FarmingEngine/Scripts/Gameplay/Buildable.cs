using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    public enum BuildableType
    {

        Default = 0,
        Grid = 10,

    }

    /// <summary>
    /// Buildable是可以放置在地图上并具有建造模式的对象的基本脚本（Z：其实就是处理建造的模型的位置、颜色等状态的。如：透明模型版本，跟随鼠标）
    /// </summary>

    public class Buildable : MonoBehaviour
    {
        [Header("Buildable")]
        public BuildableType type;               // 建造类型（Z分为网格/默认，比如耕种就是网格类型）
        public float build_distance = 2f;        // 建造距离
        public float grid_size = 1f;             // 网格大小

        [Header("Build Obstacles")]
        public LayerMask floor_layer = 1 << 9;   // 可以在其上建造的层(Z：可以理解为第九层,比如种子只能放在土壤层上)
        public LayerMask obstacle_layer = 1;     // 如果有障碍物则不能建造Z：也就是障碍物层（Z：相当于1<<0,其实就是第0层）
        public float build_obstacle_radius = 0.4f;   // 如果周围有障碍物则不能建造
        public float build_ground_dist = 0.4f;   // 四周必须有至少这个距离的地面才能建造（防止在墙壁上或空中建造）
        public bool build_flat_floor = false;    // 如果为true，则必须建造在大部分平坦的地面上
        public bool auto_destroy = false;        // 如果为true，地面被破坏时会被销毁

        [Header("FX")]
        public AudioClip build_audio;            // 建造音效
        public GameObject build_fx;              // 建造特效

        public UnityAction onBuild;              // 建造时的事件

        protected Selectable selectable;         // 可选的（可以为null）
        protected Destructible destruct;         // 可破坏的（可以为null）
        protected UniqueID unique_id;            // 唯一ID（可以为null）

        private PlayerCharacter building_character = null;   // 正在建造的角色
        [Tooltip("Z：建造模式表示玩家正在选择建造位置，但实际上还不存在")]
        private bool building_mode = false;       // 建造模式表示玩家正在选择建造位置，但实际上还不存在

        [Tooltip("Z：建造位置是否已设置")]
        private bool position_set = false;        // 建造位置是否已设置
        private bool visible_set = true;          // 是否可见
        private Color prev_color = Color.white;   // 前一个颜色
        private float manual_rotate = 0f;         // 手动旋转角度
        private float update_timer = 0f;          // 更新计时器

        private List<Collider> colliders = new List<Collider>();       // 碰撞体列表
        private List<MeshRenderer> renders = new List<MeshRenderer>(); // 网格渲染器列表（Z：包括挂载的物体其子物体的render组件）
        private List<Material> materials = new List<Material>();       // 材质列表
        private List<Material> materials_transparent = new List<Material>(); // 透明材质列表
        private List<Color> materials_color = new List<Color>();       // 材质颜色列表

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();
            renders.AddRange(GetComponentsInChildren<MeshRenderer>());
            //Z：准备好原材质和半透明材质（为了后续切换方便）
            foreach (MeshRenderer render in renders)
            {
                foreach (Material material in render.sharedMaterials)
                {
                    bool valid_mat = material && MaterialTool.HasColor(material);
                    Material material_normal = valid_mat ? new Material(material) : null;//Z；新创建材质=原材质
                    Material material_trans = valid_mat ? new Material(material) : null;//Z：新创建材质,为了半透明效果
                    if (material_trans != null)
                        MaterialTool.ChangeRenderMode(material_trans, BlendMode.Fade);//Z：修改材质为半透明形式
                    materials.Add(material_normal);
                    materials_transparent.Add(material_trans);
                    materials_color.Add(valid_mat ? material.color : Color.white);
                }
            }

            //Z：收集所有 “启用且非触发器” 的碰撞体（如木栅栏的碰撞盒），后续用途：在预览模式时，将这些碰撞体设为isTrigger = true（允许穿透），建造完成后恢复（阻挡玩家）
            foreach (Collider collide in GetComponentsInChildren<Collider>())
            {
                if (collide.enabled && !collide.isTrigger)
                {
                    colliders.Add(collide);
                }
            }
        }

        void Start()
        {

        }
        //TODO：2025.07.28Update中逻辑已经看完了，接下来看StartBuild，在豆包里整体看下，buildable的作用
        //Z：Update中处理的是，建筑预览模式半透明效果+检测到不能建造变红，预览模型跟随鼠标移动
        void Update()
        {
            if (building_mode && building_character != null)//Z：其实就是点击Crft按钮后的状态
            {
                PlayerControls constrols = PlayerControls.Get(building_character.player_id);
                PlayerControlsMouse mouse = PlayerControlsMouse.Get();
                //TODO：2025.07.27（建造功能解析中，为什么半透明可以跟着鼠标）
                if (!position_set)//Z：在是建造模式+记录character且不能设置位置的状态下（就是还可以随鼠标移动的状态）
                {
                    if (constrols.IsGamePad())
                    {
                        //Controller Game Pad building
                        Vector3 forward = TheCamera.Get().IsFreeRotation() ? TheCamera.Get().GetFacingFront() : building_character.transform.forward;
                        transform.position = building_character.transform.position + forward * GetBuildRange(building_character);
                        transform.rotation = Quaternion.Euler(0f, manual_rotate, 0f) * Quaternion.LookRotation(building_character.GetFacing(), Vector3.up);
                    }
                    else //Z：不是游戏手柄状态
                    {
                        //Mouse/Touch controls
                        transform.position = mouse.GetPointingPos();
                        transform.rotation = Quaternion.Euler(0f, manual_rotate, 0f) * TheCamera.Get().GetFacingRotation();
                    }
                    
                    //Snap to grid（Z：自动调整到合适位置）
                    FindAutoPosition();

                    //Show/Hide on mobile
                    if (TheGame.IsMobile())
                    {
                        SetBuildVisible(mouse.IsMouseHold());
                    }
                }

                bool can_build = CheckIfCanBuild();
                Color color = can_build ? Color.white : Color.red * 0.9f;//Z：每帧判断鼠标位置是否可建筑，否则使用红色且0.9（注意new Color(1f, 1f, 1f, 0.5f)这样的颜色，color.a = 0.5）
                SetModelColor(new Color(color.r, color.g, color.b, 0.5f), !can_build);//Z:其实这里设置的是白色半透明/红色半透明

            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.02f, 0.02f);
                SlowUpdate(); //Optimization
            }

        }

        private void SlowUpdate()
        {
            if (destruct != null && auto_destroy && !destruct.IsDead() && !building_mode)
            {
                if (!CheckValidFloorBuilt() || !CheckIfFlatGround())
                {
                    destruct.Kill();
                }
            }
        }

        /// <summary>
        /// Z：开启 “建造模式”，让玩家可以选择建造位置，可穿透等？？？
        /// </summary>
        /// <param name="character"></param>
        public void StartBuild(PlayerCharacter character)
        {
            building_mode = true;
            position_set = false;
            building_character = character;

            if (selectable != null)
                selectable.enabled = false;
            if (destruct)
                destruct.enabled = false;

            foreach (Collider collide in colliders)
                collide.isTrigger = true;

            if (TheGame.IsMobile()) //Hide on mobile
            {
                SetBuildVisible(false);
            }
        }

       
        public void SetBuildVisible(bool visible)
        {
            if (visible_set != visible)
            {
                visible_set = visible;
                foreach (MeshRenderer mesh in renders)
                    mesh.enabled = visible;
            }
        }

        /// <summary>
        /// Z：临时设置建造物的位置（比如玩家用锄头点击地面时，先显示一个 “预览模型”，且如果是网格形式会自动吸附到合适位置）,应该是用来辅助是否可在此建筑的判断（ CheckIfCanBuild() 检查）？？？
        /// </summary>
        /// <param name="pos"></param>
        public void SetBuildPositionTemporary(Vector3 pos)
        {
            if (building_mode)
            {
                transform.position = pos;
                FindAutoPosition();
            }
        }

        /// <summary>
        /// Z：最终确定虚影位置的逻辑
        /// </summary>
        /// <param name="pos"></param>
        public void SetBuildPosition(Vector3 pos)
        {
            if (building_mode)
            {
                position_set = true;
                transform.position = pos;

                FindAutoPosition();

                SetBuildVisible(true);
            }
        }

        //TODO：2025.07.30建造基本流程基本看了一边，理解看下能不能总结成一个问题并背诵
        /// <summary>
        /// Z：结束创建（将虚影变换为真正的颜色，碰撞体什么的打开之类的）
        /// </summary>

        public void FinishBuild()
        {
            gameObject.SetActive(true);
            building_mode = false;
            position_set = true;
            building_character = null;

            foreach (Collider collide in colliders)
                collide.isTrigger = false;

            SetBuildVisible(true);

            if (selectable != null)
                selectable.enabled = true;

            if (destruct)
                destruct.enabled = true;

            SetModelColor(Color.white, false);//Z：这里设置成的是真正的物体颜色

            if (build_fx != null)
                Instantiate(build_fx, transform.position, Quaternion.identity);

            if (onBuild != null)
                onBuild.Invoke();
        }

        //Call this function to rotate building manually (like by using a key)
        public void RotateManually(float angle_y)
        {
            if (building_mode)
            {
                manual_rotate += angle_y;
            }
        }

        /// <summary>
        /// Z：设置模型颜色
        /// </summary>
        /// <param name="color"></param>
        /// <param name="replace"></param>
        private void SetModelColor(Color color, bool replace)
        {
            if (color != prev_color)//Z：检查和之前颜色不同才继续执行
            {
                int index = 0;//Z：这个物体及其子物体render的材质球都被存在一个列表中，想在改变半透明状态时操作；只要遍历这个列表就能一一对应了
                foreach (MeshRenderer render in renders)
                {
                    Material[] mesh_materials = render.sharedMaterials;
                    for (int i = 0; i < mesh_materials.Length; i++)
                    {
                        if (index < materials.Count && index < materials_transparent.Count)
                        {
                            Material mesh_mat = mesh_materials[i];
                            Material ref_mat = color.a < 0.99f ? materials_transparent[index] : materials[index];//Z：这里是根据传入的颜色透明度，决定使用半透明模式材质/不透明材质
                            if (ref_mat != null)
                            {
                                ref_mat.color = materials_color[index] * color;//Z：普通模式（叠加原始颜色）
                                if (replace)
                                    ref_mat.color = color;//Z：覆盖模式（直接使用新颜色）
                                if (ref_mat != mesh_mat)
                                    mesh_materials[i] = ref_mat;
                            }
                        }
                        index++;
                    }
                    render.sharedMaterials = mesh_materials;
                }
            }

            prev_color = color;
        }

        //Check if possible to build at current position
        /// <summary>
        /// Z：用于判断玩家选择的位置是否可以建造建筑
        /// </summary>
        /// <returns></returns>
        public bool CheckIfCanBuild()
        {
            bool dont_overlap = !CheckIfOverlap();//不重叠,不能和其他建筑、树木等障碍物重叠。
            bool flat_ground = CheckIfFlatGround();//地面平坦,不能建在悬崖、斜坡上
            bool accessible = CheckIfAccessible();//玩家能走到这个位置（比如不能隔着墙造房子）
            bool valid_ground = CheckValidFloor();//地面类型合法,必须建在 “草地”“沙地” 等允许的地面上，不能建在水里
            //Debug.Log(dont_overlap + " " + flat_ground + " " + accessible + " " + valid_ground);
            return dont_overlap && flat_ground && valid_ground && accessible;
        }

        //Check if overlaping another object (cant build)[Z：不重叠，检测此碰撞体范围内有无其它碰撞体]
        public bool CheckIfOverlap()
        {
            List<Collider> overlap_colliders = new List<Collider>();
            LayerMask olayer = obstacle_layer & ~floor_layer;  //Remove floor layer from obstaclesZ:核心目的是从 “障碍物层（obstacle_layer）” 中排除 “可建造地面层（floor_layer）”，最终得到一个仅包含 “真正需要检测的障碍物层” 的 LayerMask

            //Check collision with bounding box
            foreach (Collider collide in colliders)//Z：碰撞体范围检测
            {
                Collider[] over = Physics.OverlapBox(transform.position, collide.bounds.extents, Quaternion.identity, olayer);
                foreach (Collider overlap in over)
                {
                    if (!overlap.isTrigger)
                        overlap_colliders.Add(overlap);
                }
            }

            //Check collision with radius (includes triggers)
            if (build_obstacle_radius > 0.01f)//Z:半径检测
            {
                Collider[] over = Physics.OverlapSphere(transform.position, build_obstacle_radius, olayer);
                overlap_colliders.AddRange(over);
            }

            //Check collision list
            foreach (Collider overlap in overlap_colliders)
            {
                if (overlap != null)
                {
                    //Dont overlap with player and dont overlap with itself
                    PlayerCharacter player = overlap.GetComponent<PlayerCharacter>();
                    Buildable buildable = overlap.GetComponentInParent<Buildable>();
                    if (player == null && buildable != this)//Z：检测到的碰撞体，不是玩家也不是自己（返回true证明有遮挡）
                        return true;
                }
            }

            return false;
        }

        //Make sure there is no obstacles in between the player and the building, this applies only if placing with keyboard/gamepad
        public bool CheckIfAccessible()
        {
            PlayerControls controls = PlayerControls.Get(building_character.player_id);
            bool game_pad = controls != null && controls.IsGamePad();

            if (position_set || !game_pad)
                return true; //Dont check this is placing with mouse or if position already set

            if (building_character != null)
            {
                Vector3 center = building_character.GetColliderCenter();
                Vector3 build_center = transform.position + Vector3.up * build_ground_dist;
                Vector3 dir = build_center - center;

                RaycastHit h1;
                bool f1 = PhysicsTool.RaycastCollision(center, dir, out h1);

                return !f1 || h1.collider.GetComponentInParent<Buildable>() == this;
            }
            return false;
        }

        //Check if there is a flat floor underneath (can't build a steep cliff)
        public bool CheckIfFlatGround()
        {
            if (!build_flat_floor)
                return true; //Dont check for flat ground

            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollision(p0, dir, out h0);
            bool f1 = PhysicsTool.RaycastCollision(p1, dir, out h1);
            bool f2 = PhysicsTool.RaycastCollision(p2, dir, out h2);
            bool f3 = PhysicsTool.RaycastCollision(p3, dir, out h3);
            bool f4 = PhysicsTool.RaycastCollision(p4, dir, out h4);

            return f0 && f1 && f2 && f3 && f4;
        }

        //Check if ground is valid layer, this one check only the first hit collision (more strict)
        public bool CheckValidFloor()
        {
            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollision(p0, dir, out h0);
            bool f1 = PhysicsTool.RaycastCollision(p1, dir, out h1);
            bool f2 = PhysicsTool.RaycastCollision(p2, dir, out h2);
            bool f3 = PhysicsTool.RaycastCollision(p3, dir, out h3);
            bool f4 = PhysicsTool.RaycastCollision(p4, dir, out h4);
            f0 = f0 && PhysicsTool.IsLayerInLayerMask(h0.collider.gameObject.layer, floor_layer);
            f1 = f1 && PhysicsTool.IsLayerInLayerMask(h1.collider.gameObject.layer, floor_layer);
            f2 = f2 && PhysicsTool.IsLayerInLayerMask(h2.collider.gameObject.layer, floor_layer);
            f3 = f3 && PhysicsTool.IsLayerInLayerMask(h3.collider.gameObject.layer, floor_layer);
            f4 = f4 && PhysicsTool.IsLayerInLayerMask(h4.collider.gameObject.layer, floor_layer);

            if (build_flat_floor)
                return f1 && f2 && f3 && f4 && f0; //Floor must be valid on all sides
            else
                return f1 || f2 || f3 || f4 || f0; //Floor must be valid only on one side
        }

        //Check if its still valid floor after built, this one ignore itself and check only the layer (less strict)
        public bool CheckValidFloorBuilt()
        {
            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollisionLayer(p0, dir, floor_layer, out h0);
            bool f1 = PhysicsTool.RaycastCollisionLayer(p1, dir, floor_layer, out h1);
            bool f2 = PhysicsTool.RaycastCollisionLayer(p2, dir, floor_layer, out h2);
            bool f3 = PhysicsTool.RaycastCollisionLayer(p3, dir, floor_layer, out h3);
            bool f4 = PhysicsTool.RaycastCollisionLayer(p4, dir, floor_layer, out h4);

            return f1 || f2 || f3 || f4 || f0;
        }

        /// <summary>
        /// Z：自动调整模型位置，比如吸附到网格（如果是网格类型，比如耕种）、贴合地面（避免悬浮）
        /// </summary>
        private void FindAutoPosition()
        {
            if (IsGrid())
            {
                transform.position = FindGridPosition(transform.position);
                transform.rotation = FindGridRotation(transform.rotation);
            }

            transform.position = FindBuildPosition(transform.position, floor_layer);
        }

        //Find the height to build
        public Vector3 FindBuildPosition(Vector3 pos, LayerMask mask)
        {
            float offset = build_distance;
            Vector3 center = pos + Vector3.up * offset;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (offset + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollisionLayer(p0, dir, mask, out h0);
            bool f1 = PhysicsTool.RaycastCollisionLayer(p1, dir, mask, out h1);
            bool f2 = PhysicsTool.RaycastCollisionLayer(p2, dir, mask, out h2);
            bool f3 = PhysicsTool.RaycastCollisionLayer(p3, dir, mask, out h3);
            bool f4 = PhysicsTool.RaycastCollisionLayer(p4, dir, mask, out h4);

            Vector3 dist_dir = Vector3.down * build_distance;
            if (f0 && h0.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h0.distance; }
            if (f1 && h1.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h1.distance; }
            if (f2 && h2.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h2.distance; }
            if (f3 && h3.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h3.distance; }
            if (f4 && h4.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h4.distance; }

            return center + dist_dir;
        }

        public Vector3 FindGridPosition(Vector3 pos)
        {
            if (grid_size >= 0.1f)
            {
                float x = Mathf.RoundToInt(pos.x / grid_size) * grid_size;
                float y = Mathf.RoundToInt(pos.y / grid_size) * grid_size;
                float z = Mathf.RoundToInt(pos.z / grid_size) * grid_size;
                return new Vector3(x, y, z);
            }
            return pos;
        }

        public Quaternion FindGridRotation(Quaternion rot)
        {
            Vector3 euler = rot.eulerAngles;
            float angle = Mathf.RoundToInt(euler.y / 90f) * 90f;
            return Quaternion.Euler(euler.x, angle, euler.z);
        }

        public float GetBuildRange(PlayerCharacter character)
        {
            return build_distance + character.interact_range;
        }

        public bool IsBuilding()
        {
            return building_mode;
        }

        public bool IsPositionSet()
        {
            return position_set;
        }

        /// <summary>
        /// Z：判断是否是网格类型
        /// </summary>
        /// <returns></returns>
        public bool IsGrid()
        {
            return type == BuildableType.Grid;
        }

        public Destructible GetDestructible()
        {
            return destruct; //May be null
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public string GetUID()
        {
            if (unique_id != null)
                return unique_id.unique_id;
            return "";
        }
    }

}