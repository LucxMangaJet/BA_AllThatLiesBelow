﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InventoryManager
{
    private static InventoryManager instance;
    public static InventoryManager Instance { get => GetInstance(); }

    private Inventory playerInventory;
    private PlayerController player;
    private Inventory otherInventory;
    bool playerInventoryOpen;

    public event System.Action<ItemAmountPair> PlayerCollected;

    private static InventoryManager GetInstance()
    {
        if (instance == null)
        {
            instance = new InventoryManager();
            instance.player = GameObject.FindObjectOfType<PlayerController>();
            if (instance.player != null)
            {
                instance.playerInventory = instance.player.Inventory;
            }
            else
            {
                Debug.LogError("No PlayerController found.");
            }
        }
        return instance;
    }

    public static void NotifyInventoryOpen(IInventoryOwner owner)
    {
        if (owner.Inventory == Instance.playerInventory)
        {
            Instance.playerInventoryOpen = true;
        }
        else
        {
            Instance.otherInventory = owner.Inventory;
        }
    }

    public static void NotifyInventoryClosed(IInventoryOwner owner)
    {
        if (owner.Inventory == Instance.playerInventory)
        {
            Instance.playerInventoryOpen = false;
        }
        else if (owner.Inventory == Instance.otherInventory)
        {
            Instance.otherInventory = null;
        }
    }

    public static void PlayerCollects(ItemType itemType, int amount)
    {
        if (Instance.playerInventory != null)
            Instance.playerInventory.Add(itemType, amount);

        Instance.PlayerCollected?.Invoke(new ItemAmountPair(itemType, amount));
    }

    public static bool PlayerTryPay(ItemType itemType, int amount)
    {
        return Instance.playerInventory.TryRemove(new ItemAmountPair(itemType, amount));
    }

    public static bool PlayerHas(ItemType type, int amount)
    {
        return Instance.playerInventory.Contains(new ItemAmountPair(type, amount));
    }

    public static void ForcePlayerInventoryClose()
    {
        instance.player.CloseInventory();
    }

}
