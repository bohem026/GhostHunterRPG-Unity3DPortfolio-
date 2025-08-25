using UnityEngine;
using UnityEngine.UI;

public class GearFilterButtonController : MonoBehaviour
{
    [SerializeField] private GearController.GearType Type;
    [SerializeField] private Sprite sprite_Select;
    [SerializeField] private Sprite sprite_Normal;

    /// <summary>
    /// ��ư�� ����/���� ���¿� ���� ��������Ʈ�� ��ȯ�Ѵ�.
    /// </summary>
    public void Select(bool command)
    {
        GetComponent<Image>().sprite = command ? sprite_Select : sprite_Normal;
    }

    /// <summary>
    /// ���� ������ ��� Ÿ���� �������� ���� ���� Ÿ���� ����� ��ȯ�Ѵ�.
    /// �������� ���� �Ѿ�� ó������ ��ȯ�Ѵ�(Count ���� ��ȯ).
    /// </summary>
    public GearController.GearType Sort()
    {
        int LEN = (int)GearController.GearType.Count + 1;
        return (GearController.GearType)(((int)Type + 1) % LEN);
    }
}
