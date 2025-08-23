using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GearFilterButtonController : MonoBehaviour
{
    [SerializeField] private GearController.GearType Type;
    [SerializeField] private Sprite sprite_Select;
    [SerializeField] private Sprite sprite_Normal;

    public void Select(bool command)
    {
        GetComponent<Image>().sprite
            = command ? sprite_Select : sprite_Normal;
    }

    public GearController.GearType Sort()
    {
        int LEN = (int)GearController.GearType.Count + 1;

        return (GearController.GearType)(((int)Type + 1) % LEN);
    }
}
