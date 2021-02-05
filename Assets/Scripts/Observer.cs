using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Observer : MonoBehaviour
{
    public GameObject path;
    public Rigidbody2D rb;
    public float speed;

    Transform[] waypoints;
    int current;

    void Start()
    {
        waypoints = path.GetComponentsInChildren<Transform>();
    }

    void Update()
    {
        if(current < waypoints.Length - 1)
        {
            transform.position = Vector2.MoveTowards(transform.position, waypoints[current + 1].position, speed * Time.deltaTime);
            if(transform.position == waypoints[current + 1].position)
            {
                current++;
            }
        }
        else
        {
            FindObjectOfType<HidingAgent>().EndExperiment();
        }
        /*
        Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        rb.velocity = movement * speed;
        */
    }
}
