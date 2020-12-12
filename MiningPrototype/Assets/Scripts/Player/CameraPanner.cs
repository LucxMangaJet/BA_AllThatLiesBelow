﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraPanner : MonoBehaviour
{
    [SerializeField] AnimationCurve curve;
    [SerializeField] float overWorldOffset;
    [SerializeField] float transitionSpeed;
    [SerializeField] RectTransform topImage, botImage;
    [SerializeField] float barOpeningSpeed;

    [Zenject.Inject] PlayerStateMachine player;
    [Zenject.Inject] PlayerInteractionHandler interactionHandler;

    float yOffset;
    bool cinematicMode;

    float barhalfHeight;

    private void Start()
    {
        barhalfHeight = Screen.height / 12;
        topImage.sizeDelta = new Vector2(0, barhalfHeight * 2);
        botImage.sizeDelta = new Vector2(0, barhalfHeight * 2);
        botImage.anchoredPosition = new Vector2(0, -barhalfHeight);
        topImage.anchoredPosition = new Vector2(0, barhalfHeight);
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    [NaughtyAttributes.Button]
    public void EnterCinematicMode()
    {
        cinematicMode = true;
        StopAllCoroutines();
        StartCoroutine(TransitionCutscenebars(open: true));
    }

    [NaughtyAttributes.Button]
    public void ExitCinematicMode()
    {
        cinematicMode = false;
        StopAllCoroutines();
        StartCoroutine(TransitionCutscenebars(open: false));
    }

    public IEnumerator TransitionCutscenebars(bool open)
    {
        float currentY = topImage.anchoredPosition.y;
        float dir = open ? -1 : 1;

        while ((open) ? topImage.anchoredPosition.y > -barhalfHeight : topImage.anchoredPosition.y < barhalfHeight)
        {
            yield return null;
            topImage.anchoredPosition += new Vector2(0, dir * Time.deltaTime * barOpeningSpeed);
            botImage.anchoredPosition += new Vector2(0, -dir * Time.deltaTime* barOpeningSpeed);
        }
        botImage.anchoredPosition = new Vector2(0, -dir * barhalfHeight);
        topImage.anchoredPosition = new Vector2(0, dir * barhalfHeight);
    }

    public void UpdatePosition()
    {
        if (Time.timeScale == 0)
            return;

        Vector3 dir = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);

        dir = dir - new Vector3(0.5f, 0.5f);
        dir = new Vector3(dir.x.Sign() * curve.Evaluate(dir.x.Abs()), dir.y.Sign() * curve.Evaluate(dir.y.Abs()));


        if (player.InOverworld() || cinematicMode)
            yOffset += transitionSpeed * Time.deltaTime;
        else
            yOffset -= transitionSpeed * Time.deltaTime;

        yOffset = Mathf.Clamp(yOffset, 0, overWorldOffset);

        transform.position = player.transform.position + (cinematicMode ? Vector3.zero : dir) + new Vector3(0, yOffset);
    }
}
