using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    public LineRenderer lr;
    Observer observer;

    void Start()
    {
        observer = FindObjectOfType<Observer>();
    }

    void Update()
    {
        lr.SetPositions(new Vector3[] { transform.position, observer.transform.position });
    }
}
