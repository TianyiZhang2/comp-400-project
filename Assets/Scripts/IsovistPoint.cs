using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IsovistPoint : MonoBehaviour
{
    public int visibility;

    private void Update()
    {
        TextMeshPro t = GetComponentInChildren<TextMeshPro>();
        t.text = visibility + "";
    }
}
