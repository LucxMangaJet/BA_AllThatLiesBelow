﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bed : MonoBehaviour, IInteractable
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Hut hut;
    [SerializeField] Image nightFadeToBlack;

    public void BeginInteracting(GameObject interactor)
    {
        PlayerStateMachine player = interactor.GetComponent<PlayerStateMachine>();

        if (hut.IsOpen())
            EnterBed(player);
    }

    public void EndInteracting(GameObject interactor)
    {
        PlayerStateMachine player = interactor.GetComponent<PlayerStateMachine>();

        LeaveBed(player);
    }

    public void SubscribeToForceQuit(Action action)
    {
        //action += LeaveBed;
    }

    public void UnsubscribeToForceQuit(Action action)
    {
        //action -= LeaveBed;
    }

    private void EnterBed(PlayerStateMachine playerToHide)
    {
        playerToHide.Disable();
        spriteRenderer.enabled = true;
        StartCoroutine(SleepCoroutine(playerToHide));
    }

    private void LeaveBed(PlayerStateMachine playerToEnableAgain)
    {
        playerToEnableAgain.Enable();
        spriteRenderer.enabled = false;
    }

    IEnumerator SleepCoroutine(PlayerStateMachine playerToEnableAgain)
    {
        if (nightFadeToBlack != null)
        {
            float nightOpacity = 0f;

            while (nightOpacity < 1f)
            {
                nightOpacity += Time.deltaTime;
                nightFadeToBlack.color = new Color(0, 0, 0, nightOpacity);

                yield return null;
            }

            yield return new WaitForSeconds(0.25f);
            ProgressionHandler.Instance.StartNextDay();
            yield return new WaitForSeconds(0.25f);

            while (nightOpacity > 0f)
            {
                nightOpacity -= Time.deltaTime;
                nightFadeToBlack.color = new Color(0, 0, 0, nightOpacity);

                yield return null;
            }

        }

        LeaveBed(playerToEnableAgain);
    }
}
