using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashEffectController : MonoBehaviour
{
    private const float SLASH_EFFECT_SPEED = 10f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * SLASH_EFFECT_SPEED * Time.deltaTime, Space.Self);
    }
}
