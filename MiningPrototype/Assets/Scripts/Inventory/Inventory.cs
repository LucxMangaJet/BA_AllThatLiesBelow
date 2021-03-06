﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class Inventory
{
    [UnityEngine.Serialization.FormerlySerializedAs("allowStoreing")]
    [SerializeField] bool canDeposit = true;
    [SerializeField] int maxSize = 0;
    [SerializeField] List<ItemAmountPair> content = new List<ItemAmountPair>();

    public int Count { get => content.Count; }
    public bool HasSpace
    {
        get
        {
            return maxSize <= 0 || content.Count < maxSize;
        }
    }
    public bool CanDeposit { get => canDeposit && HasSpace; }


    //Added delegate to make bool arguments understandable
    public delegate void InventoryChangedDelegate(bool add, ItemAmountPair element, bool playsound);

    [field: NonSerialized]
    public event InventoryChangedDelegate InventoryChanged;

    public ItemAmountPair this[int index]
    {
        get
        {
            if (index < 0 || index >= content.Count)
                return ItemAmountPair.Nothing;

            return content[index];
        }
    }

    public void Add(ItemType type, int amount, bool playSound = true)
    {
        bool isReadable = ItemsData.GetItemInfo(type).AmountIsUniqueID;

        if (content.Count > 0 && !isReadable)
        {
            for (int i = 0; i < content.Count; i++)
            {
                var item = content[i];

                if (item.type == type)
                {
                    content[i] = new ItemAmountPair(item.type, item.amount + amount);
                    InventoryChanged?.Invoke(true, new ItemAmountPair(type, amount), playSound);
                    return;
                }
            }
        }

        ItemAmountPair pair = new ItemAmountPair(type, amount);

        content.Add(pair);
        InventoryChanged?.Invoke(true, pair, playSound);
    }

    public void Add(ItemAmountPair pair)
    {
        Add(pair.type, pair.amount);
    }

    public bool Contains(ItemAmountPair pair)
    {
        int id = GetStackIdFor(pair);

        if (id >= 0)
        {
            if (content[id].amount >= pair.amount)
                return true;
        }

        return false;
    }

    public ItemAmountPair Pop()
    {
        if (IsEmpty())
        {
            return ItemAmountPair.Nothing;
        }
        else
        {
            var el = content[0];
            content.RemoveAt(0);
            return el;
        }
    }

    public bool IsEmpty()
    {
        return content.Count == 0;
    }

    public bool TryRemove(ItemAmountPair pair)
    {
        var info = ItemsData.GetItemInfo(pair.type);
        if (info.AmountIsUniqueID)
        {
            int i = content.FindIndex(0, (x) => x == pair);
            if (i >= 0 && i < content.Count)
            {
                content.RemoveAt(i);
                InventoryChanged?.Invoke(false, pair, true);
                return true;
            }
        }
        else
        {
            int id = GetStackIdFor(pair);

            if (id >= 0)
            {
                if (content[id].amount > pair.amount)
                {
                    var newPair = new ItemAmountPair(pair.type, content[id].amount - pair.amount);
                    content[id] = newPair;

                    InventoryChanged?.Invoke(false, pair, true);
                    return true;
                }
                else if (content[id].amount == pair.amount)
                {
                    content.RemoveAt(id);

                    InventoryChanged?.Invoke(false, pair, true);
                    return true;
                }
            }
        }

        return false;
    }

    private int GetStackIdFor(ItemAmountPair pair)
    {
        if (ItemsData.GetItemInfo(pair.type).AmountIsUniqueID)
            return content.FindIndex((x) => x.type == pair.type && x.amount == pair.amount);
        else
            return content.FindIndex((x) => x.type == pair.type);
    }

    public ItemAmountPair RemoveStack(int index)
    {
        if (index < 0 || index >= content.Count)
            return ItemAmountPair.Nothing;

        var c = content[index];

        if (c.IsNull())
        {
            content.RemoveAt(index);
            InventoryChanged?.Invoke(false, c, true);
            return c;
        }

        return ItemAmountPair.Nothing;
    }

    public int GetTotalWeight()
    {
        return content.Sum((x) => x.GetTotalWeight());
    }


    public ItemAmountPair[] GetContent()
    {
        return content.ToArray();
    }
}

[System.Serializable]
public struct ItemAmountPair
{
    public ItemType type;
    public int amount;

    public ItemAmountPair(ItemType itemType, int itemAmount)
    {
        type = itemType;
        amount = itemAmount;
    }

    public static bool TryParse(string typeS, string amountS, out ItemAmountPair pair)
    {
        if (int.TryParse(amountS, out int amount))
        {
            if (System.Enum.TryParse(typeS, out ItemType type))
            {
                pair = new ItemAmountPair(type, amount);
                return true;
            }
        }
        pair = ItemAmountPair.Nothing;
        return false;
    }


    public override string ToString()
    {
        return amount + " " + type.ToString();
    }

    public bool IsNull()
    {
        return amount <= 0 || type == ItemType.None;
    }

    public bool IsValid()
    {
        return !IsNull();
    }

    public static ItemAmountPair Nothing
    {
        get => new ItemAmountPair(ItemType.None, -1);
    }

    public static ItemAmountPair One(ItemType type)
    {
        return new ItemAmountPair(type, 1);
    }

    public int GetTotalWeight()
    {
        return amount * ItemsData.GetItemInfo(type).Weight;
    }

    public static bool operator ==(ItemAmountPair i1, ItemAmountPair i2)
    {
        return i1.type == i2.type && i1.amount == i2.amount;
    }

    public static bool operator !=(ItemAmountPair i1, ItemAmountPair i2)
    {
        return i1.type != i2.type || i1.amount != i2.amount;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
