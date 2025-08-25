using UnityEngine;

/// <summary>
/// 장비 능력치 정렬용 식별자 컴포넌트.
/// ResourceUtility.SortByEnum 에서 GearSO.StatType 순서 기준으로
/// UI 요소(예: 텍스트 행)를 정렬할 때 사용됩니다.
/// </summary>
public class GearAbilitySorter : MonoBehaviour
{
    public GearSO.StatType Type;
}
