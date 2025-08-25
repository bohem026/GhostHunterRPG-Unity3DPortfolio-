using UnityEngine;

public class GearController : MonoBehaviour
{
    public enum Rarity
    {
        Common,
        Rare,
        Unique,
        Legendary,
        Count /*Length*/
    }

    public enum GearType
    {
        Hat,
        Sweater,
        Gloves,
        Sneakers,
        Count /*Length*/
    }

    public GearSO Asset { get; set; }
    public GearWindowController.GearLocation Location { get; set; }

    /// <summary>
    /// 장비 데이터와 장착 위치를 바인딩한다.
    /// </summary>
    public void Init(GearSO asset, GearWindowController.GearLocation location)
    {
        Asset = asset;
        Location = location;
    }
}
