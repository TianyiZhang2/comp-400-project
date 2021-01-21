using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HidingAgent : MonoBehaviour
{
    public Rigidbody2D rb;
    public LineRenderer lr;
    public Ghost ghostPrefab;
    Observer observer;

    public float speedPercent;
    List<Vector2> directions;
    float speed;

    float prevTime; // Allow movement once per second

    void Start()
    {
        observer = FindObjectOfType<Observer>();

        directions = new List<Vector2>(new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right,
             Diagonal(45), Diagonal(135), Diagonal(225), Diagonal(315) });

        speed = observer.speed * speedPercent;
        prevTime = Time.time;
    }

    void Update()
    {
        BaselineAlgorithm();
        
        // Update line renderer
        lr.SetPositions(new Vector3[] { rb.position, observer.rb.position });
    }

    void BaselineAlgorithm()
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, observer.rb.position - rb.position);
        if (Time.time - prevTime >= 1 && hit.collider.gameObject.CompareTag("Player"))
        {
            // Hiding strategy
            foreach (Vector2 direction in directions)
            {
                Vector2 newPos = rb.position + speed * direction;
                // Cast ray from new pos
                hit = Physics2D.Raycast(newPos, observer.rb.position - newPos);
                // Detect obstacle at new pos
                bool posObstructed = Physics2D.OverlapCircle(newPos, 0.5f);

                if (!hit.collider.gameObject.CompareTag("Player") && !posObstructed)
                {
                    Move(newPos);
                    break;
                }
            }
            prevTime = Time.time;
        }
    }

    Vector2 Diagonal(float degree)
    {
        return new Vector2(Mathf.Cos(degree / 180 * Mathf.PI), Mathf.Sin(degree / 180 * Mathf.PI));
    }

    void Move(Vector2 pos)
    {
        Ghost ghost = FindObjectOfType<Ghost>();
        if(ghost != null)
        {
            Destroy(ghost.gameObject);
        }
        Instantiate(ghostPrefab, transform.position, Quaternion.identity);
        transform.position = pos;
    }
}
