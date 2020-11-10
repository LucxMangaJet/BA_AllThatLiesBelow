﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngineInternal;

public class PlayerController : InventoryOwner
{
    [Header("Player")]
    [SerializeField] float groundedAngle;
    [SerializeField] float jumpVelocity;
    [SerializeField] float moveSpeed;

    [SerializeField] float jumpCooldown = 0.1f;
    [SerializeField] float timeAfterGroundedToJump = 0.1f;

    [SerializeField] Transform feet;
    [SerializeField] float feetRadius;

    [SerializeField] TestGeneration generation;
    [SerializeField] float maxDigDistance = 3;

    [SerializeField] GameObject pickaxe;
    [SerializeField] float digSpeed = 10;
    [SerializeField] Transform mouseHighlight;

    [SerializeField] SpriteAnimation an_Walk, an_Idle, an_Fall, an_Inventory;

    [SerializeField] ParticleSystem miningParticles;
    [SerializeField] int miningBreakParticlesCount;
    [SerializeField] float miningParticlesRateOverTime = 4;

    [SerializeField] AudioSource breakBlock, startMining, walking;
    [SerializeField] DirectionBasedAnimator pickaxeAnimator;

    [SerializeField] float inventoryOpenDistance;
    [SerializeField] float maxInteractableDistance;
    [SerializeField] EventSystem eventSystem;


    Rigidbody2D rigidbody;
    SpriteAnimator spriteAnimator;
    float lastGroundedTimeStamp;
    float lastJumpTimeStamp;

    private bool isGrounded;
    Vector2 rightWalkVector = Vector3.right;
    Camera camera;
    SpriteRenderer spriteRenderer;
    Vector2Int? digTarget;
    IInteractable currentInteractable;

    [ReadOnly]
    [SerializeField] bool inMining;

    protected override void Start()
    {
        base.Start();
        camera = Camera.main;
        rigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteAnimator = GetComponent<SpriteAnimator>();
    }

    private void Update()
    {

        if (Vector2Int.Distance(GetPositionInGrid(), GetClickCoordinate()) <= maxDigDistance)
        {
            Debug.DrawLine(GetPositionInGridV3(), GetClickPositionV3(), Color.yellow, Time.deltaTime);
            UpdateDigTarget();

            if (Input.GetMouseButton(0))
            {
                if (eventSystem.currentSelectedGameObject == null)
                    TryDig();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (Vector3.Distance(GetPositionInGridV3(), GetClickPositionV3()) <= inventoryOpenDistance && isGrounded)
                {
                    OpenInventory();
                }
                else
                {
                    if (currentInteractable == null)
                        TryInteract();
                    else
                        TryStopInteracting();
                }
            }
            else
            {
                if (inMining)
                    DisableMiningParticles();
            }
        }
        else
        {
            digTarget = null;
            if (inMining)
                DisableMiningParticles();
        }

        UpdateDigHighlight();
    }

    private void TryInteract()
    {
        Vector3 mousePos = Input.mousePosition;
        Ray r = camera.ScreenPointToRay(mousePos);
        var hits = Physics2D.RaycastAll(r.origin, r.direction, 100000);

        currentInteractable = null;
        foreach (var hit in hits)
        {
            if (hit.transform == transform)
                continue;

            if (hit.transform.TryGetComponent(out IInteractable interactable))
            {
                Debug.Log(hit.transform.name);
                currentInteractable = interactable;
                currentInteractable.SubscribeToForceQuit(OnInteractableForceQuit);
                currentInteractable.BeginInteracting(gameObject);
                Debug.DrawLine(GetPositionInGridV3(), hit.point, Color.green, 1f);
                break;
            }
        }
    }

    private void OnInteractableForceQuit()
    {
        TryStopInteracting();
    }

    private bool CanJump()
    {
        return Time.time - lastGroundedTimeStamp < timeAfterGroundedToJump && Time.time - lastJumpTimeStamp > jumpCooldown;
    }

    private void UpdateDigTarget()
    {
        digTarget = generation.GetClosestSolidBlock(GetPositionInGrid(), GetClickCoordinate());
        if (generation.IsAirAt(digTarget.Value.x, digTarget.Value.y))
        {
            digTarget = null;
        }
    }

    private void UpdateDigHighlight()
    {

        if (digTarget == null)
            mouseHighlight.position = new Vector3(-1000, -1000);
        else
            mouseHighlight.position = new Vector3(digTarget.Value.x, digTarget.Value.y, 0) + new Vector3(0.5f, 0.5f, 0);
    }

    private void TryPlace()
    {
        Vector2Int clickPos = GetClickCoordinate();
        if (generation.HasLineOfSight(GetPositionInGrid(), clickPos, debugVisualize: true))
            generation.PlaceAt(clickPos.x, clickPos.y);
    }

    private void TryDig()
    {
        CloseInventory();

        if (digTarget.HasValue)
        {
            bool broken = generation.DamageAt(digTarget.Value.x, digTarget.Value.y, Time.deltaTime * digSpeed);

            if (broken)
            {
                miningParticles.transform.position = (Vector3Int)digTarget + new Vector3(0.5f, 0.5f);
                miningParticles.Emit(miningBreakParticlesCount);
                breakBlock.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                breakBlock.Play();
                DisableMiningParticles();
            }
            else
            {
                UpdateMiningParticlesPositions();
            }

            if (!inMining)
            {
                StartMiningParticles();
            }
        }
        else
        {
            if (inMining)
                DisableMiningParticles();
        }
    }

    private void UpdateMiningParticlesPositions()
    {
        miningParticles.transform.position = generation.GetWorldLocationOfFreeFaceFromSource(digTarget.Value, GetPositionInGrid());
        Debug.DrawLine((Vector3Int)GetPositionInGrid(), miningParticles.transform.position, Color.yellow, 0.1f);
    }


    private void DisableMiningParticles()
    {
        inMining = false;
        var emission = miningParticles.emission;
        emission.rateOverTimeMultiplier = 0;
        startMining.Stop();
        pickaxeAnimator.Stop();
    }

    private void TryStopInteracting()
    {
        if (currentInteractable != null)
        {
            currentInteractable.EndInteracting(gameObject);
            currentInteractable.UnsubscribeToForceQuit(OnInteractableForceQuit);
            currentInteractable = null;
        }
    }

    private void StartMiningParticles()
    {
        var emission = miningParticles.emission;
        emission.rateOverTimeMultiplier = miningParticlesRateOverTime;
        inMining = true;
        startMining.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        startMining.Play();
        pickaxeAnimator.Play();
    }

    private Vector2Int GetPositionInGrid()
    {
        return new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y) + 1); //+1 to be at center of player
    }

    /// <summary>
    /// Just +1 in Y compared to transform.position
    /// </summary>
    private Vector3 GetPositionInGridV3()
    {
        return new Vector3(transform.position.x, transform.position.y + 1); //+1 to be at center of player
    }

    private Vector2Int GetClickCoordinate()
    {
        Vector3 clickPos = GetClickPositionV3();
        return new Vector2Int((int)clickPos.x, (int)clickPos.y);
    }

    private Vector3 GetClickPositionV3()
    {
        Vector3 position = Input.mousePosition + Vector3.back * camera.transform.position.z;
        return camera.ScreenToWorldPoint(position);
    }

    private void FixedUpdate()
    {
        UpdateWalk();
        UpdateJump();
    }

    private void UpdateWalk()
    {
        var horizontal = Input.GetAxis("Horizontal");

        if (Mathf.Abs(horizontal) > 0.15f)
        {
            CloseInventory();
        }

        if (currentInteractable != null)
        {
            if (Vector3.Distance(GetPositionInGridV3(), currentInteractable.gameObject.transform.position) > maxInteractableDistance)
                TryStopInteracting();
        }

        rigidbody.position += horizontal * rightWalkVector * moveSpeed * Time.fixedDeltaTime;
        rigidbody.velocity = new Vector2(0, rigidbody.velocity.y);

        if (Mathf.Abs(horizontal) > 0.2f)
            spriteRenderer.flipX = horizontal < 0;

        if (isGrounded)
        {
            if (horizontal == 0)
            {
                if (InventoryDisplayState == InventoryState.Open)
                {
                    spriteAnimator.Play(an_Inventory, false);
                    SetPickaxeVisible(false);
                }
                else
                {
                    spriteAnimator.Play(an_Idle, false);
                    SetPickaxeVisible(true);
                }
            }
            else
            {
                spriteAnimator.Play(an_Walk, false);
                SetPickaxeVisible(true);
            }
        }
        else
        {
            spriteAnimator.Play(an_Fall);
            SetPickaxeVisible(true);
        }

        UpdateWalkingSound(horizontal);
    }

    private void SetPickaxeVisible(bool isVisible = true)
    {
        if (isVisible != pickaxe.activeSelf)
            pickaxe.SetActive(isVisible);
    }

    private void UpdateJump()
    {
        var vertical = Input.GetAxis("Vertical");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(feet.position, feetRadius);
        isGrounded = colliders != null && colliders.Length > 1;

        if (isGrounded)
        {
            lastGroundedTimeStamp = Time.time;
        }

        if (CanJump() && vertical > 0)
        {
            Jump();
        }
    }

    private void Jump()
    {
        Debug.Log("Jump");
        rigidbody.velocity = new Vector2(rigidbody.velocity.x, jumpVelocity);
        lastJumpTimeStamp = Time.time;
    }

    private void UpdateWalkingSound(float horizontal)
    {
        if (isGrounded && Mathf.Abs(horizontal) > 0.01f)
        {
            if (!walking.isPlaying)
            {

                walking.Play();
            }
        }
        else
        {
            if (walking.isPlaying)
            {

                walking.Pause();
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateWalkVector(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdateWalkVector(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        rightWalkVector = Vector2.right;
    }

    private void UpdateWalkVector(Collision2D collision)
    {
        var contact = collision.contacts[0];
        float angle = Mathf.Acos(Vector3.Dot(contact.normal, Vector3.up)) * Mathf.Rad2Deg;

        Debug.DrawLine(transform.position, transform.position + (Vector3)contact.normal);

        if (angle < groundedAngle)
        {
            rightWalkVector = Vector3.Cross(contact.normal, Vector3.forward).normalized;
        }
        else
        {
            rightWalkVector = Vector3.right;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (feet != null)
            Gizmos.DrawWireSphere(feet.position, feetRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightWalkVector);

        Gizmos.DrawWireSphere((Vector3Int)GetPositionInGrid(), maxDigDistance);
        Gizmos.DrawWireSphere(GetPositionInGridV3(), inventoryOpenDistance);
    }
}
