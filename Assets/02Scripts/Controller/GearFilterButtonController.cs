using UnityEngine;
using UnityEngine.UI;

public class GearFilterButtonController : MonoBehaviour
{
    [SerializeField] private GearController.GearType Type;
    [SerializeField] private Sprite sprite_Select;
    [SerializeField] private Sprite sprite_Normal;

    /// <summary>
    /// 버튼의 선택/비선택 상태에 따라 스프라이트를 전환한다.
    /// </summary>
    public void Select(bool command)
    {
        GetComponent<Image>().sprite = command ? sprite_Select : sprite_Normal;
    }

    /// <summary>
    /// 현재 설정된 장비 타입을 기준으로 다음 필터 타입을 계산해 반환한다.
    /// 열거형의 끝을 넘어가면 처음으로 순환한다(Count 포함 순환).
    /// </summary>
    public GearController.GearType Sort()
    {
        int LEN = (int)GearController.GearType.Count + 1;
        return (GearController.GearType)(((int)Type + 1) % LEN);
    }
}
