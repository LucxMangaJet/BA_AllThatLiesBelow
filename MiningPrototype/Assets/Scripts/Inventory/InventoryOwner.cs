﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInventoryOwner
{
    Inventory Inventory { get; }
}

public class InventoryOwner : MonoBehaviour, IInventoryOwner, IInteractable
{
    [Header("Inventory Owner")]
    [SerializeField] Inventory inventory;
    [SerializeField] protected Canvas canvas;
    [SerializeField] InventoryVisualizer inventoryVisualizerPrefab;
    [SerializeField] AudioSource openSource;
    [SerializeField] bool inventoryVisualizerUpdates;

    InventoryVisualizer inventoryVisualizer;
    InventoryState state = InventoryState.Closed;
    public Inventory Inventory { get => inventory; }
    public InventoryState InventoryDisplayState { get => state; }

    public virtual bool IsFlipped { get => false;}

    public void SetInventory(Inventory newInventory)
    {
        inventory = newInventory;
    }

    public Inventory GetInventory()
    {
        return inventory;
    }

    protected virtual void Start()
    {
        inventory.InventoryChanged += OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        if (state == InventoryState.Open && inventoryVisualizer != null)
            inventoryVisualizer.RefreshInventoryDisplay();
    }

    public virtual void OpenInventory()
    {
        if (state == InventoryState.Closed)
        {
            state = InventoryState.Open;
            if (inventoryVisualizer == null)
            {
                if (openSource != null)
                {
                    openSource.pitch = 1;
                    openSource.Play();
                }

                inventoryVisualizer = Instantiate(inventoryVisualizerPrefab, canvas.transform);
                inventoryVisualizer.Init(transform, inventory);
                inventoryVisualizer.SetFollowOnUpdate(inventoryVisualizerUpdates);
            }

            InventoryManager.NotifyInventoryOpen(this);
        }
    }

    public virtual void CloseInventory()
    {
        if (state == InventoryState.Open)
        {
            state = InventoryState.Closed;
            if (openSource != null)
            {
                openSource.pitch = 0.66f;
                openSource.Play();
            }

            if (inventoryVisualizer != null)
            {
                inventoryVisualizer.Close();
                inventoryVisualizer = null;
            }
        }

        InventoryManager.NotifyInventoryClosed(this);
    }

    public void BeginInteracting(GameObject interactor)
    {
        OpenInventory();
    }

    public void EndInteracting(GameObject interactor)
    {
        CloseInventory();
    }

    public void SubscribeToForceQuit(Action action)
    {
    }

    public void UnsubscribeToForceQuit(Action action)
    {
    }
}

public enum InventoryState
{
    Closed,
    Open
}
