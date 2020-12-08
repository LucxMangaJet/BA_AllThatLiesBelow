﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemPlacingHandler : MonoBehaviour
{
    [Zenject.Inject] PlayerInteractionHandler player;
    [Zenject.Inject] CameraController cameraController;
    [Zenject.Inject] Zenject.DiContainer diContainer;
    [Zenject.Inject] InventoryManager inventoryManager;
    [Zenject.Inject] EventSystem eventSystem;

    [SerializeField] AudioSource placingSound;

    bool holdingPlacable;
    ItemAmountPair currentHeld;
    Inventory currentOrigin;

    Transform previewTransform;
    IItemPreview preview;

    IDropReceiver currentReceiver;

    public void Hide()
    {
        player.SetHeldItem(setToPickaxe: true);

        if (previewTransform != null)
            Destroy(previewTransform.gameObject);
        preview = null;

        if (currentReceiver != null)
            currentReceiver.EndHover();
    }

    public void Show(ItemAmountPair pair, Inventory origin)
    {
        currentHeld = pair;
        currentOrigin = origin;
        var info = ItemsData.GetItemInfo(pair.type);

        if (info.CanBePlaced)
        {
            if (info.PickupPreviewPrefab != null)
            {
                player.SetHeldItem(setToPickaxe: false);
                player.SetHeldItemSprite(info.PickupHoldSprite);

                var go = diContainer.InstantiatePrefab(info.PickupPreviewPrefab);
                previewTransform = go.transform;
                preview = previewTransform.GetComponent<IItemPreview>();
            }
            holdingPlacable = true;
        }
        else
        {
            holdingPlacable = false;
        }
    }

    public void TryPlace(ItemType type, Vector3 tryplacePosition)
    {
        Debug.Log("tried receive "+ currentReceiver +  " . " + currentOrigin);
        if (currentReceiver != null && currentOrigin != null)
        {
            Debug.Log(currentReceiver + " tried receive" + currentHeld.type + " from " + currentOrigin);
            if (currentReceiver.WouldTakeDrop(currentHeld))
            {
                Debug.Log(currentReceiver + " received" + currentHeld.type + " from " + currentOrigin);
                currentReceiver.ReceiveDrop(currentHeld, currentOrigin);
                return;
            }
        }

        if (holdingPlacable)
        {
            if (preview != null)
            {
                var info = ItemsData.GetItemInfo(type);
                if (info.CanBePlaced && info.Prefab != null && preview.WouldPlaceSuccessfully())
                {
                    if (inventoryManager.PlayerTryPay(type, 1))
                    {
                        placingSound?.Play();
                        cameraController.Shake(preview.GetPlacePosition(tryplacePosition),CameraShakeType.hill,0.1f,10f);
                        var go = diContainer.InstantiatePrefab(info.Prefab, preview.GetPlacePosition(tryplacePosition), Quaternion.identity, null);
                    }
                }
            }
        }
    }

    public void UpdatePosition(Vector3 position)
    {
        if (holdingPlacable)
        {
            if (preview != null)
                preview.UpdatePreview(position);
        }

        var hits = Util.RaycastFromMouse(cameraController.Camera);
        IDropReceiver dropReceiver = null;
        foreach (var hit in hits)
        {
            if (hit.transform.TryGetComponent(out IDropReceiver receiver))
            {
                dropReceiver = receiver;
                break;
            }
        }

        if (dropReceiver == null)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            foreach (RaycastResult item in results)
            {
                if (dropReceiver == null)
                    dropReceiver = item.gameObject.GetComponentInParent<IDropReceiver>();
            }
        }
        

        if (dropReceiver != currentReceiver)
        {
            if (currentReceiver != null)
                currentReceiver.EndHover();

            if (dropReceiver != null)
                dropReceiver.BeginHoverWith(currentHeld);
            currentReceiver = dropReceiver;
        }
        else
        {
            if (currentReceiver != null)
                currentReceiver.HoverUpdate(currentHeld);
        }

    }

    public bool IsAboveReceiver()
    {
        return currentReceiver != null && currentReceiver.WouldTakeDrop(currentHeld);
    }
}
