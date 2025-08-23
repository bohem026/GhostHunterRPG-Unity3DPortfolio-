using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Inst;

    public PlayerController _plyCtrl;

    void Awake()
    {
        if (!_plyCtrl) GameObject.FindObjectOfType<PlayerController>();
        if (!Inst) Inst = this;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        ChangeMouseInputMode(0);
    }

    public void ChangeMouseInputMode(int mode)
    {
        switch (mode)
        {
            case 0:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case 1:
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                break;
            default:
                break;
        }
    }
}
