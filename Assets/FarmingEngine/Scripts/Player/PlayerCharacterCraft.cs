using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 负责管理玩家角色的制作和建造的类（Z 建造应该是有两个地方负责，一个是这里[比如帽子]、一个是其他[比如门等]，是这样吗？）
    /// 从 “判断能不能做”→“开始做（显进度 / 显虚影）”→“确认细节（选位置）”→“做完给奖励”→“不想做就取消”，全程管得明明白白。
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterCraft : MonoBehaviour
    {
        public GroupData[] craft_groups;// Z：在这里设置可以制作的物品类别，会变成左侧制造面板的一级页面
        public float craft_energy = 1f;
        public float build_energy = 2f;

        public UnityAction<CraftData> onCraft;
        public UnityAction<Buildable> onBuild;

        private PlayerCharacter character;

        //Z：记录状态的 “临时变量”（管家的 “小本本”，记当前在干嘛）
        [Tooltip("Z：这个似乎是，半透明的木屋虚影")]
        private Buildable current_buildable = null;//Z：当前正在预览的 “建造虚影”（比如半透明木屋）
        private CraftData current_build_data = null;
        private CraftData current_crafting = null;//Z：当前正在制作的东西（比如正在做的锄头）
        private GameObject craft_progress = null;
        private float build_timer = 0f;//Z：“建造前的冷却
        private float craft_timer = 0f;//Z：建造需要的时间
        [Tooltip(" clicked_build = true，就表示 \"玩家已经确认位置，角色正在前往建造点的路上")]
        private bool clicked_build = false;
        private InventorySlot build_pay_slot;//Z：记录 “你用背包里的哪个物品来建造东西”，相当于给这个物品打个 “标记”，方便后续真正建造时扣掉它

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        void Start()
        {
            if (PlayerUI.Get(character.player_id))
            {
                PlayerUI.Get(character.player_id).onCancelSelection += CancelBuilding;
            }
        }

       
        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            build_timer += Time.deltaTime;
            craft_timer += Time.deltaTime;

            PlayerControls controls = PlayerControls.Get(character.player_id);

            //Cancel building
            if (controls.IsPressUICancel() || controls.IsPressPause())
                CancelBuilding();

            //Cancel crafting
            if (current_crafting != null && character.IsMoving())
                CancelCrafting();

            //Complete crafting after timer
            if (current_crafting != null)
            {
                if (craft_timer > current_crafting.craft_duration)
                    CompleteCrafting();
            }
        }
        //核心功能
        //1、第一步：判断 “能不能做”（CanCraft 系列方法）
        //---- Crafting ----
        /// <summary>
        /// Z：判断是否符合建造条件
        /// </summary>
        /// <param name="item"></param>
        /// <param name="skip_cost"></param>
        /// <param name="skip_near"></param>
        /// <returns></returns>
        public bool CanCraft(CraftData item, bool skip_cost = false, bool skip_near = false)
        {
            if (item == null || character.IsDead())
                return false;

            if (character.Attributes.GetAttributeValue(AttributeType.Energy) < craft_energy)
                return false; //Not enough energy

            //Z：检查是否在允许建造的区域（比如是否在农场内）
            bool has_craft_cost = skip_cost || HasCraftCost(item);
            bool has_near = skip_near || HasCraftNear(item);
            return has_near && has_craft_cost;
        }

        public bool CanCraft(CraftData item, InventorySlot pay_slot, bool skip_near = false)
        {
            if (item == null || character.IsDead())
                return false;

            if (character.Attributes.GetAttributeValue(AttributeType.Energy) < craft_energy)
                return false; //Not enough energy

            bool has_craft_cost = HasCraftCost(item);
            bool has_pay_slot = HasCraftPaySlot(item, pay_slot);
            bool has_near = skip_near || HasCraftNear(item);
            bool has_cost = has_craft_cost || has_pay_slot;
            return has_near && has_cost;
        }

        public bool HasCraftPaySlot(CraftData item, InventorySlot pay_slot)
        {
            if (pay_slot != null)
            {
                ItemData idata = pay_slot.GetItem();
                return idata != null && idata.GetBuildData() == item;
            }
            return false;
        }

        public bool HasCraftCost(CraftData item)
        {
            bool can_craft = true;
            CraftCostData cost = item.GetCraftCost();
            Dictionary<GroupData, int> item_groups = new Dictionary<GroupData, int>(); //Add to groups so that fillers are not same than items

            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                AddCraftCostItemsGroups(item_groups, pair.Key, pair.Value);
                if (!character.Inventory.HasItem(pair.Key, pair.Value))
                    can_craft = false; //Dont have required items
            }

            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                int value = pair.Value + CountCraftCostGroup(item_groups, pair.Key);
                if (!character.Inventory.HasItemInGroup(pair.Key, value))
                    can_craft = false; //Dont have required items
            }

            foreach (KeyValuePair<CraftData, int> pair in cost.craft_requirements)
            {
                if (CountRequirements(pair.Key) < pair.Value)
                    can_craft = false; //Dont have required constructions
            }
            return can_craft;
        }

        public bool HasCraftNear(CraftData item)
        {
            bool can_craft = true;
            CraftCostData cost = item.GetCraftCost();
            if (cost.craft_near != null && !character.IsNearGroup(cost.craft_near) && !character.EquipData.HasItemInGroup(cost.craft_near))
                can_craft = false; //Not near required construction
            return can_craft;
        }

        private void AddCraftCostItemsGroups(Dictionary<GroupData, int> item_groups, ItemData item, int quantity)
        {
            foreach (GroupData group in item.groups)
            {
                if (item_groups.ContainsKey(group))
                    item_groups[group] += quantity;
                else
                    item_groups[group] = quantity;
            }
        }

        private int CountCraftCostGroup(Dictionary<GroupData, int> item_groups, GroupData group)
        {
            if (item_groups.ContainsKey(group))
                return item_groups[group];
            return 0;
        }
        /// <summary>
        /// Z：遍历需求的材料列表，来扣除材料
        /// </summary>
        /// <param name="item"></param>
        /// <param name="build"></param>
        public void PayCraftingCost(CraftData item, bool build = false)
        {
            CraftCostData cost = item.GetCraftCost();
            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                character.Inventory.UseItem(pair.Key, pair.Value);
            }
            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                character.Inventory.UseItemInGroup(pair.Key, pair.Value);
            }

            float cost_energy = build ? build_energy : craft_energy;
            character.Attributes.AddAttribute(AttributeType.Energy, -cost_energy);
        }
        /// <summary>
        /// Z：接口方法，扣除需要的材料
        /// </summary>
        /// <param name="item"></param>
        /// <param name="pay_slot"></param>
        /// <param name="build"></param>
        public void PayCraftingCost(CraftData item, InventorySlot pay_slot, bool build = false)
        {
            if (pay_slot != null && pay_slot.inventory != null)
            {
                InventoryData inventory = pay_slot.inventory;
                inventory.RemoveItemAt(pay_slot.slot, 1);
            }
            else
            {
                PayCraftingCost(item, build);
            }
        }

        public int CountRequirements(CraftData requirement)
        {
            if (requirement is ItemData)
                return character.Inventory.CountItem((ItemData)requirement);
            else
                return CraftData.CountSceneObjects(requirement);
        }

        //----- Craftin process -----
        //2、第二步：开始 “做 / 建”（Start 系列方法）[分 “制作物品” 和 “建造建筑” 两种不同处理]
        /// <summary>
        /// Z 外部调用开始建造，并根据得到ConstructionData/PlantData来执行不同方法(建筑、植物、物品)
        /// </summary>
        /// <param name="data"></param>
        public void StartCraftingOrBuilding(CraftData data)
        {
            if (CanCraft(data))
            {
                ConstructionData construct = data.GetConstruction();
                PlantData plant = data.GetPlant();

                if (construct != null)
                    CraftConstructionBuildMode(construct);
                else if (plant != null)
                    CraftPlantBuildMode(plant, 0);
                else
                    StartCrafting(data);

                TheAudio.Get().PlaySFX("craft", data.craft_sound);
            }
        }

        //Start crafting with timer
        /// <summary>
        /// Z：似乎是用来处理（有进度条）建造情况的
        /// </summary>
        /// <param name="data"></param>
        public void StartCrafting(CraftData data)
        {
            if (data != null && current_crafting == null)
            {
                current_crafting = data;
                craft_timer = 0f;
                character.StopMove();

                if (AssetData.Get().action_progress != null && data.craft_duration > 0.1f)
                {
                    craft_progress = Instantiate(AssetData.Get().action_progress, transform);
                    craft_progress.GetComponent<ActionProgress>().duration = data.craft_duration;
                }

                if (data.craft_duration < 0.01f)
                    CompleteCrafting();
            }
        }

        //After reached build position, start crafting duration / anim
        public void StartCraftBuilding()
        {
            if (current_buildable != null)
            {
                StartCraftBuilding(current_buildable.transform.position); //Use current position
            }
        }
        /// <summary>
        /// Z：可以简单理解为这个是真正建造（不是虚影）（一般是在角色到达指定位置后才调用）
        /// </summary>
        /// <param name="pos"></param>
        public void StartCraftBuilding(Vector3 pos)
        {
            if (current_build_data != null && current_buildable != null && current_crafting == null)
            {
                if (CanCraft(current_build_data, build_pay_slot, true))
                {

                    current_buildable.SetBuildPositionTemporary(pos); //Set to position to test the condition, before applying it
                    if (current_buildable.CheckIfCanBuild())
                    {
                        current_buildable.SetBuildPosition(pos);
                        StartCrafting(current_build_data);
                        character.FaceTorward(pos);
                    }
                }
            }
        }

        //------------ Build Items ----------

        /// <summary>
        /// Z：简单说就是：从背包里选一个 “可以建造东西的物品”，然后进入建造预览模式
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="slot"></param>
        public void BuildItemBuildMode(InventoryData inventory, int slot)
        {
            ItemData idata = inventory.GetItem(slot);
            if (idata != null)
            {
                CraftData cdata = idata.GetBuildData();//Z：获取背包中物品的可建造物品
                if (cdata != null)
                {
                    CraftBuildMode(cdata);//
                    build_pay_slot = new InventorySlot(inventory, slot);//Z：记录背包格子
                }
            }
        }

        public void BuildItem(InventoryData inventory, int slot)
        {
            InventoryItemData invdata = inventory?.GetInventoryItem(slot);
            ItemData idata = ItemData.Get(invdata?.item_id);
            if (invdata == null || idata == null)
                return;

            CraftData data = null;
            if (idata.construction_data != null)
                data = idata.construction_data;
            else if (idata.plant_data != null)
                data = idata.plant_data;
            else if (idata.character_data != null)
                data = idata.character_data;

            if (data != null && CanCraft(data, true))
            {
                inventory.RemoveItemAt(slot, 1);

                Craftable craftable = CraftCraftable(data, true);
                if (craftable != null && craftable is Construction)
                {
                    Construction construction = (Construction)craftable;
                    BuiltConstructionData constru = PlayerData.Get().GetConstructed(construction.GetUID());
                    if (idata.HasDurability())
                        constru.durability = invdata.durability; //Save durability
                }

                TheAudio.Get().PlaySFX("craft", idata.craft_sound);
                PlayerUI.Get(character.player_id)?.CancelSelection();
            }
        }

        //----- Craft in Build mode -----
        /// <summary>
        /// 通俗理解为：根据你要建造的东西的 “类型”，启动对应的建造预览预览模式。
        /// </summary>
        /// <param name="data"></param>
        public void CraftBuildMode(CraftData data)
        {
            if (data is PlantData)
                CraftPlantBuildMode((PlantData)data, 0);
            if (data is ConstructionData)
                CraftConstructionBuildMode((ConstructionData)data);
            if (data is CharacterData)
                CraftCharacterBuildMode((CharacterData)data);
        }

        public void CraftPlantBuildMode(PlantData plant, int stage)
        {
            CancelCrafting();

            Plant aplant = Plant.CreateBuildMode(plant, transform.position, stage);
            current_buildable = aplant.GetBuildable();
            current_buildable.StartBuild(character);
            current_build_data = plant;
            build_pay_slot = null;
            clicked_build = false;
            build_timer = 0f;
        }

        /// <summary>
        /// Z：创建一个临时的、可预览的建筑虚影，让玩家能调整位置和方向
        /// </summary>
        /// <param name="item"></param>
        public void CraftConstructionBuildMode(ConstructionData item)
        {
            CancelCrafting();//Z：取消当前其他建造操作
            //Z：创建木栅栏的临时虚影（方便玩家调整位置）
            Construction construction = Construction.CreateBuildMode(item, transform.position + transform.forward * 1f);
            current_buildable = construction.GetBuildable();
            current_buildable.StartBuild(character);
            current_build_data = item;
            build_pay_slot = null;
            clicked_build = false;
            build_timer = 0f;
        }

        public void CraftCharacterBuildMode(CharacterData item)
        {
            CancelCrafting();

            Character acharacter = Character.CreateBuildMode(item, transform.position + transform.forward * 1f);
            current_buildable = acharacter.GetBuildable();
            if (current_buildable != null)
                current_buildable.StartBuild(character);
            current_build_data = item;
            build_pay_slot = null;
            clicked_build = false;
            build_timer = 0f;
        }

        //5、取消操作（Cancel 系列方法）

        //----- Cancel and confirm -----

        public void CancelCrafting()
        {
            current_crafting = null;
            if (craft_progress != null)
                Destroy(craft_progress);
            CancelBuilding();
        }

        public void CancelBuilding()
        {
            if (current_buildable != null)
            {
                Destroy(current_buildable.gameObject);
                current_buildable = null;
                current_build_data = null;
                build_pay_slot = null;
                clicked_build = false;
            }
        }

        //3、第三步：确认建造位置（TryBuildAt 方法）[如果是建造建筑（比如木屋），玩家选好位置后点击地面，代码会调用 TryBuildAt(点击的位置)：]

        //Order to move to and build there
        /// <summary>
        /// Z：通俗理解为：玩家尝试在某个位置建造东西（虚影）时的 "检查和行动" 逻辑
        /// </summary>
        /// <param name="pos"></param>
        public void TryBuildAt(Vector3 pos)
        {
            //Z：检查范围
            bool in_range = character.interact_type == PlayerInteractBehavior.MoveAndInteract || IsInBuildRange();
            if (!in_range)
                return;
            //Z：检查这里是否可以建造，可以的话就正式把虚影固定在这个位置，并移动到之这个位置
            if (!clicked_build && current_buildable != null)
            {
                //Z：用来辅助是否可在此建筑的判断
                current_buildable.SetBuildPositionTemporary(pos); //Set build position before checkifcanbuild

                bool can_build = current_buildable.CheckIfCanBuild();
                if (can_build)
                {
                    current_buildable.SetBuildPosition(pos);
                    clicked_build = true; //Give command to build
                    character.MoveTo(pos);
                }
            }
        }


        //4、第四步：完成 “做 / 建”（Complete 系列方法）[当 “制作进度条满了”（比如锄头 3 秒到了），或 “玩家走到建造位置了”（比如木屋位置），就会触发 “完成逻辑”。]
        //----- Crafting Completion -----

        //End of the craft timer
        /// <summary>
        /// Z：负责处理制作或建造成功后的一系列操作
        /// </summary>
        public void CompleteCrafting()
        {
            if (current_crafting != null)
            {
                if (current_buildable != null)
                    CompleteBuilding(current_buildable.transform.position);//Z；？？？建造（Building）：如果current_buildable不为空，说明正在制作的是 “需要放置在场景中的建筑”（比如房子、栅栏）。这时调用CompleteBuilding()，把建筑生成在指定位置（比如玩家面前）。
                else
                    CraftCraftable(current_crafting);//Z：？？？如果current_buildable为空，说明正在制作的是 “物品”（比如武器、工具）。这时调用CraftCraftable()，直接把物品放入玩家背包或生成在地上。
                current_crafting = null;
            }
        }

        //Craft immediately
        public Craftable CraftCraftable(CraftData data, bool skip_cost = false)
        {
            ItemData item = data.GetItem();
            ConstructionData construct = data.GetConstruction();
            PlantData plant = data.GetPlant();
            CharacterData character = data.GetCharacter();

            if (item != null)
                return CraftItem(item, skip_cost);
            else if (construct != null)
                return CraftConstruction(construct, skip_cost);
            else if (plant != null)
                return CraftPlant(plant, 0, skip_cost);
            else if (character != null)
                return CraftCharacter(character, skip_cost);
            return null;
        }

        public Item CraftItem(ItemData item, bool skip_cost = false)
        {
            if (CanCraft(item, skip_cost))
            {
                if (!skip_cost)
                    PayCraftingCost(item);

                Item ritem = null;
                // Z 这里是用来判断是放在背包里 还是 放在地上
                if (character.Inventory.CanTakeItem(item, item.craft_quantity))
                    character.Inventory.GainItem(item, item.craft_quantity);
                else
                    ritem = Item.Create(item, transform.position, item.craft_quantity);

                character.SaveData.AddCraftCount(item.id);
                character.Attributes.GainXP(item.craft_xp_type, item.craft_xp);

                if (onCraft != null)
                    onCraft.Invoke(item);

                return ritem;
            }
            return null;
        }

        public Character CraftCharacter(CharacterData character, bool skip_cost = false)
        {
            if (CanCraft(character, skip_cost))
            {
                if (!skip_cost)
                    PayCraftingCost(character);

                Vector3 pos = transform.position + transform.forward * 0.8f;
                Character acharacter = Character.Create(character, pos);

                this.character.SaveData.AddCraftCount(character.id);
                this.character.Attributes.GainXP(character.craft_xp_type, character.craft_xp);

                if (onCraft != null)
                    onCraft.Invoke(character);

                return acharacter;
            }
            return null;
        }

        public Plant CraftPlant(PlantData plant, int stage, bool skip_cost = false)
        {
            if (CanCraft(plant, skip_cost))
            {
                if (!skip_cost)
                    PayCraftingCost(plant);

                Vector3 pos = transform.position + transform.forward * 0.4f;
                Plant aplant = Plant.Create(plant, pos, stage);

                character.SaveData.AddCraftCount(plant.id);
                character.Attributes.GainXP(plant.craft_xp_type, plant.craft_xp);

                if (onCraft != null)
                    onCraft.Invoke(plant);

                return aplant;
            }
            return null;
        }

        public Construction CraftConstruction(ConstructionData construct, bool skip_cost = false)
        {
            if (CanCraft(construct, skip_cost))
            {
                if (!skip_cost)
                    PayCraftingCost(construct);

                Vector3 pos = transform.position + transform.forward * 1f;
                Construction aconstruct = Construction.Create(construct, pos);

                character.SaveData.AddCraftCount(construct.id);
                character.Attributes.GainXP(construct.craft_xp_type, construct.craft_xp);

                if (onCraft != null)
                    onCraft.Invoke(construct);

                return aconstruct;
            }
            return null;
        }

        public void CompleteBuilding()
        {
            if (current_buildable != null)
            {
                CompleteBuilding(current_buildable.transform.position);
            }
        }

        /// <summary>
        /// Z：将虚拟建筑（虚影）转化为真实建筑，并处理一系列后续操作（如消耗材料、保存数据、播放音效等）
        /// </summary>
        /// <param name="pos"></param>
        public void CompleteBuilding(Vector3 pos)
        {
            CraftData item = current_crafting;
            if (item != null && current_buildable != null && CanCraft(item, build_pay_slot, true))
            {
                current_buildable.SetBuildPositionTemporary(pos); //Set to position to test the condition, before applying it

                if (current_buildable.CheckIfCanBuild())
                {
                    current_buildable.SetBuildPosition(pos);

                    character.FaceTorward(pos);

                    PayCraftingCost(item, build_pay_slot, true);

                    Buildable buildable = current_buildable;
                    buildable.FinishBuild();

                    character.SaveData.AddCraftCount(item.id);
                    character.Attributes.GainXP(item.craft_xp_type, item.craft_xp);

                    current_buildable = null;
                    current_build_data = null;
                    clicked_build = false;
                    character.StopAutoMove();

                    PlayerUI.Get(character.player_id)?.CancelSelection();
                    TheAudio.Get().PlaySFX("craft", buildable.build_audio);

                    if (onBuild != null)
                        onBuild.Invoke(buildable);

                    character.TriggerBusy(1f);
                }
            }
        }

        //---- Values and getters

        public void LearnCraft(string craft_id)
        {
            character.SaveData.UnlockID(craft_id);
        }

        public bool HasLearnt(string craft_id)
        {
            return character.SaveData.IsIDUnlocked(craft_id);
        }

        public int CountTotalCrafted(CraftData craftable)
        {
            if (craftable != null)
                return character.SaveData.GetCraftCount(craftable.id);
            return 0;
        }

        public void ResetCraftCount(CraftData craftable)
        {
            if (craftable != null)
                character.SaveData.ResetCraftCount(craftable.id);
        }

        public void ResetCraftCount()
        {
            character.SaveData.ResetCraftCount();
        }

        //Did it click to order to build
        public bool ClickedBuild()
        {
            return clicked_build;
        }

        public bool CanBuild()
        {
            return current_buildable != null && current_buildable.IsBuilding() && build_timer > 0.5f;
        }

        public bool IsInBuildRange()
        {
            if (current_buildable == null)
                return false;
            Vector3 dist = (character.GetInteractCenter() - current_buildable.transform.position);
            return dist.magnitude < current_buildable.GetBuildRange(character);
        }

        public bool IsBuildMode()
        {
            return current_buildable != null && current_buildable.IsBuilding();
        }

        public bool IsCrafting()
        {
            return current_crafting != null;
        }

        public float GetCraftProgress()
        {
            if (current_crafting != null && current_crafting.craft_duration > 0.01f)
                return craft_timer / current_crafting.craft_duration;
            return 0f;
        }

        public Buildable GetCurrentBuildable()
        {
            return current_buildable; //Can be null if not in build mode
        }

        public CraftData GetCurrentCrafting()
        {
            return current_crafting;
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }

        public CraftStation GetCraftStation()
        {
            CraftStation station = CraftStation.GetNearestInRange(transform.position);
            return station;
        }

        public List<GroupData> GetCraftGroups()
        {
            CraftStation station = CraftStation.GetNearestInRange(transform.position);
            if (station != null)
                return new List<GroupData>(station.craft_groups);
            else
                return new List<GroupData>(craft_groups);//Z 创建一个 GroupData 类型列表的副本
        }
    }
}
