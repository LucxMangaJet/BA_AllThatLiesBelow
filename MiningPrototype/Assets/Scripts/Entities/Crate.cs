﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crate : MineableObject, INonPersistantSavable
{
    [SerializeField] SpriteRenderer spriteRenderer;

    [SerializeField] CrateType crateType;

    [SerializeField] Sprite[] crateSprites;

    private void Start()
    {
        SetupCrate();
    }

    public void SetCrateType(CrateType newType)
    {
        crateType = newType;
    }

    private void SetupCrate()
    {
        var rot = transform.rotation;
        transform.rotation = Quaternion.identity;
        spriteRenderer.sprite = crateSprites[(int)crateType];
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.size = spriteRenderer.bounds.size;
        boxCollider.offset = (spriteRenderer.bounds.center-transform.position);
        transform.rotation = rot;
    }

    public void Pack(ItemAmountPair toPack)
    {
        contains = toPack;
    }
    public override Vector2 GetPosition()
    {
        if (overlayAnimator != null)
            return overlayAnimator.transform.position;
        else
            return transform.position + (spriteRenderer.size.y / 2) * transform.up;
    }

    public SpawnableSaveData ToSaveData()
    {
        var data = new CrateSaveData();
        data.SpawnableIDType = SpawnableIDType.Crate;
        data.Position = new SerializedVector3(transform.position);
        data.Rotation = new SerializedVector3(transform.eulerAngles);
        data.Type = crateType;
        data.Content = contains;
        return data;
    }

    public void Load(SpawnableSaveData dataOr)
    {
        if(dataOr is CrateSaveData data)
        {
            transform.position = data.Position.ToVector3();
            transform.eulerAngles = data.Rotation.ToVector3();
            crateType = data.Type;
            contains = data.Content;
            SetupCrate();
        }
    }

    [System.Serializable]
    public class CrateSaveData : SpawnableSaveData
    {
        public ItemAmountPair Content;
        public CrateType Type;
    }
}


public enum CrateType
{
    Mini,
    Small,
    Tall,
    Wider,
    Higher
}