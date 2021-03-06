﻿using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using System;

namespace Barotrauma
{
    class AIObjectiveDecontainItem : AIObjective
    {
        public override string DebugTag => "decontain item";

        public Func<Item, float> GetItemPriority;

        //can either be a tag or an identifier
        private readonly string[] itemIdentifiers;
        private readonly ItemContainer container;
        private readonly Item targetItem;

        private AIObjectiveGoTo goToObjective;
        private bool isCompleted;

        public AIObjectiveDecontainItem(Character character, Item targetItem, ItemContainer container, AIObjectiveManager objectiveManager, float priorityModifier = 1) 
            : base(character, objectiveManager, priorityModifier)
        {
            this.targetItem = targetItem;
            this.container = container;
        }


        public AIObjectiveDecontainItem(Character character, string itemIdentifier, ItemContainer container, AIObjectiveManager objectiveManager, float priorityModifier = 1) 
            : this(character, new string[] { itemIdentifier }, container, objectiveManager, priorityModifier) { }

        public AIObjectiveDecontainItem(Character character, string[] itemIdentifiers, ItemContainer container, AIObjectiveManager objectiveManager, float priorityModifier = 1) 
            : base(character, objectiveManager, priorityModifier)
        {
            this.itemIdentifiers = itemIdentifiers;
            for (int i = 0; i < itemIdentifiers.Length; i++)
            {
                itemIdentifiers[i] = itemIdentifiers[i].ToLowerInvariant();
            }
            this.container = container;
        }

        public override bool IsCompleted() => isCompleted;

        public override float GetPriority()
        {
            if (objectiveManager.CurrentOrder == this)
            {
                return AIObjectiveManager.OrderPriority;
            }
            return 1.0f;
        }

        protected override void Act(float deltaTime)
        {
            if (isCompleted) { return; }
            Item itemToDecontain = null;
            //get the item that should be de-contained
            if (targetItem == null)
            {
                if (itemIdentifiers != null)
                {
                    foreach (string identifier in itemIdentifiers)
                    {
                        itemToDecontain = container.Inventory.FindItemByIdentifier(identifier) ?? container.Inventory.FindItemByTag(identifier);
                        if (itemToDecontain != null) { break; }
                    }
                }
            }
            else
            {
                itemToDecontain = targetItem;
            }
            if (itemToDecontain == null || itemToDecontain.Container != container.Item) // Item not found or already de-contained, consider complete
            {
                isCompleted = true;
                return;
            }
            if (itemToDecontain.OwnInventory != character.Inventory && itemToDecontain.ParentInventory != character.Inventory)
            {
                if (!character.CanInteractWith(container.Item, out _, checkLinked: false))
                {
                    TryAddSubObjective(ref goToObjective, () => new AIObjectiveGoTo(container.Item, character, objectiveManager));
                    return;
                }
            }
            itemToDecontain.Drop(character);
            isCompleted = true;
        }

        public override bool IsDuplicate(AIObjective otherObjective)
        {
            if (!(otherObjective is AIObjectiveDecontainItem decontainItem)) { return false; }
            if (decontainItem.itemIdentifiers != null && itemIdentifiers != null)
            {
                if (decontainItem.itemIdentifiers.Length != itemIdentifiers.Length) { return false; }
                for (int i = 0; i < decontainItem.itemIdentifiers.Length; i++)
                {
                    if (decontainItem.itemIdentifiers[i] != itemIdentifiers[i]) { return false; }
                }
                return true;
            }
            else if (decontainItem.itemIdentifiers == null && itemIdentifiers == null)
            {
                return decontainItem.targetItem == targetItem;
            }
            return false;
        }
    }
}
