using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearController : MonoBehaviour
{
    public enum Rarity
    {
        Common,
        Rare,
        Unique,
        Legendary,
        Count       /*Length*/
    }

    public enum GearType
    {
        Hat,
        Sweater,
        Gloves,
        Sneakers,
        Count       /*Length*/
    }

    public GearSO Asset { get; set; }
    public GearWindowController.GearLocation Location { get; set; }

    public void Init
        (
        GearSO asset,
        GearWindowController.GearLocation location
        )
    {
        Asset = asset;
        Location = location;
    }
}
