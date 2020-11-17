﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock : TilemapCarvingEntity, ITileMapElement
{
    [SerializeField] float width = 2;
    [SerializeField] float destructionSpeed;
    [SerializeField] Rigidbody2D rigidbody;
    [SerializeField] AudioSource rockFalling;

    public TileMap TileMap { get; private set; }

    protected void Start()
    {
        Carve();
        rigidbody.isKinematic = true;
    }


    public override void OnTileCrumbleNotified(int x, int y)
    {
        if (this == null)
            return;

        UnCarvePrevious();
        rigidbody.isKinematic = false;
        rigidbody.WakeUp();
        StartCoroutine(FallingRoutine());
    }

    private IEnumerator FallingRoutine()
    {
        rockFalling?.Play();

        while (!rigidbody.IsSleeping())
        {
            yield return null;
        }

        rockFalling?.Stop();

        rigidbody.isKinematic = true;
        Carve();
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent(out IEntity entity))
        {
            float speed = collision.relativeVelocity.magnitude;
            //Damage
        }
        else if (collision.collider.TryGetComponent(out TileMapElement gridElement))
        {
            if (gridElement.TileMap == null || collision.relativeVelocity.magnitude < destructionSpeed)
            {
                return;
            }

            Vector3[] offsets = { new Vector3(0,-1), new Vector3(-0.55f,-1), new Vector3(0.55f, -1) };

            foreach (var offset in offsets)
            {
                var pos = (transform.position + offset).ToGridPosition();
                Util.DebugDrawTile(pos);
                bool block = gridElement.TileMap.IsBlockAt(pos.x, pos.y);
                if (block)
                {
                    gridElement.TileMap.DamageAt(pos.x, pos.y, 100, playerCaused:false);
                }
            }

        }
    }

    public void Setup(TileMap tileMap)
    {
        TileMap = tileMap;
    }
}

