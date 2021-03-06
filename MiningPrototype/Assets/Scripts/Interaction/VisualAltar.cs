﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualAltar : MonoBehaviour, IDialogUser
{

    [SerializeField] AltarSkin skin;

    [SerializeField] SpriteAnimator spriteAnimator;
    [SerializeField] AudioSource audioSource;

    [SerializeField] AltarSkinInfo[] skins;

    [Zenject.Inject] PlayerManager playerManager;

    AudioClip last;
    DialogVisualizer dialogVisualizer;

    public void Setup(INodeServiceProvider services, AltarBaseNode node)
    {
        dialogVisualizer = (DialogVisualizer)services.DialogVisualizer;

        dialogVisualizer.OnChangeState += OnChangeState;

        if (node is AltarDialogRootNode root)
        {
            if (root.Skin != null)
            {
                if (Enum.TryParse(root.Skin.SkinName, out AltarSkin newSkin))
                {
                    skin = newSkin;
                    Debug.Log("Changed skin to " + skin);
                }
                else
                {
                    Debug.LogError("Could not parse Skin " + root.Skin.SkinName);
                }
            }
        }
    }

    private void Start()
    {
        OnChangeState(AltarState.Passive);
    }

    private void OnDestroy()
    {
        if (dialogVisualizer)
            dialogVisualizer.OnChangeState -= OnChangeState;
    }

    private void OnChangeState(AltarState altarState)
    {
        AltarVisualStateInfo info = GetInfoForState(altarState);

        if (info == null)
        {
            Debug.LogWarning("no skin and state info defined for " + skin + " and " + altarState);
        }
        else
        {
            spriteAnimator.Renderer.flipX = info.lookAtPlayer ? playerManager.GetPlayerPosition().x < transform.position.x : false;

            if (spriteAnimator.Animation != info.Animation)
            {
                spriteAnimator.Play(info.Animation);
            }

            if (info.IsTalking)
            {
                audioSource.loop = false;
                audioSource.clip = info.GetTalkingAudioByCharacterLength(dialogVisualizer.SentenceCharacterLength, last);
                last = audioSource.clip;
            }
            else
            {
                audioSource.loop = true;
                if (audioSource.clip != info.AudioClip)
                    audioSource.clip = info.AudioClip;
            }

            if (audioSource.clip != null)
            {
                audioSource.Play();
            }
        }
    }

    private AltarVisualStateInfo GetInfoForState(AltarState altarState)
    {
        foreach (var skinInfo in skins)
        {
            if (skin == skinInfo.Skin)
            {
                foreach (var stateInfo in skinInfo.States)
                {
                    if (stateInfo.State == altarState)
                        return stateInfo;
                }
            }
        }

        return null;
    }


}

public enum AltarState
{
    Passive,
    Talking,
    Idle
}

public enum AltarSkin
{
    Miner,
    Archeologist,
    Hunter,
}

[System.Serializable]
public class AltarSkinInfo
{
    public AltarSkin Skin;
    public AltarVisualStateInfo[] States;
}

[System.Serializable]
public class AltarVisualStateInfo
{
    public AltarState State;
    public SpriteAnimation Animation;
    public AudioClip AudioClip;
    public bool lookAtPlayer;

    public bool IsTalking;
    [SerializeField] private AudioCharacterLengthPair[] talkingAudios;

    public AudioClip GetTalkingAudioByCharacterLength(int length, AudioClip clipToNotUse)
    {
        if (length == 0)
            return null;

        foreach (AudioCharacterLengthPair pair in talkingAudios)
        {
            if (pair.CharacterLength > (length / 2) && pair.AudioClip != clipToNotUse)
                return pair.AudioClip;
        }

        return talkingAudios[talkingAudios.Length - 1].AudioClip;
    }
}

[System.Serializable]
public class AudioCharacterLengthPair
{
    public AudioClip AudioClip;
    public int CharacterLength;
}
