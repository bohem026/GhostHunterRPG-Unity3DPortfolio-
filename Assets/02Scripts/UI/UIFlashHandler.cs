using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFlashHandler : MonoBehaviour
{
    [SerializeField] private AudioClip sfxClip;

    void OnEnable()
    {
        if (!sfxClip) return;

        StartCoroutine(PlaySFX());
    }

    IEnumerator PlaySFX()
    {
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce
            (AudioPlayerPoolManager.SFXPoolType.UI, sfxClip);
    }

    public void OnFlashAnimFinished()
    {
        gameObject.SetActive(false);
    }
}
