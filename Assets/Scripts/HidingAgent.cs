using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HidingAgent : MonoBehaviour
{
    public Rigidbody2D rb;
    public LineRenderer lr;
    public GameObject ghostPrefab;
    public GameObject obsMarkerPrefab;
    Observer observer;
    IsovistManager isovistManager;

    public float speedPercent;
    public HidingAlgorithm algorithm;
    public int lookAheadDepth; // For DeepLookAhead algorithm

    List<Vector2> directions;
    float speed;
    float prevTime; // Allow movement once per second

    // For metric
    int timesSpotted = 0;
    int timesHidden = 0;
    int timesMoved = 0;
    int seenIndex = 0;

    void Start()
    {
        observer = FindObjectOfType<Observer>();

        directions = new List<Vector2>(new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right,
             Diagonal(45), Diagonal(135), Diagonal(225), Diagonal(315) });

        speed = observer.speed * speedPercent;

        if (algorithm == HidingAlgorithm.IsovistMetric || algorithm == HidingAlgorithm.IsovistLookAhead)
        {
            isovistManager = FindObjectOfType<IsovistManager>();
            isovistManager.SetupIsovists();
        }

        prevTime = Time.time;
    }

    void Update()
    {
        switch(algorithm)
        {
            case HidingAlgorithm.Baseline: Baseline(); break;
            case HidingAlgorithm.LookAhead: LookAhead(); break;
            case HidingAlgorithm.IsovistMetric: IsovistMetric(); break;
            case HidingAlgorithm.IsovistLookAhead: IsovistLookAhead(); break;
            case HidingAlgorithm.DeepLookAhead: DeepLookAhead(); break;
        }
        
        // Update line renderer
        lr.SetPositions(new Vector3[] { rb.position, observer.rb.position });
    }

    #region Hiding Algorithms

    void Baseline()
    {
        // Can only move one space every 1/speed seconds
        if (Time.time - prevTime >= 1/speed)
        {
            // Check if observer can see you
            RaycastHit2D hit = Physics2D.Linecast(rb.position, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
            if(hit.collider.gameObject.CompareTag("Player"))
            {
                List<Vector2> validMoves = new List<Vector2>();
                List<Vector2> safeMoves = new List<Vector2>();
                // Add to each list above
                foreach (Vector2 direction in directions)
                {
                    Vector2 newPos = rb.position + direction;
                    // Detect obstacle at newPos
                    bool posObstructed = Physics2D.OverlapCircle(newPos, 0.5f, LayerMask.GetMask("Wall"));
                    if (!posObstructed)
                    {
                        validMoves.Add(newPos);
                        // Cast ray from newPos (check if observer can see)
                        hit = Physics2D.Linecast(newPos, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
                        if (hit.collider.gameObject.CompareTag("Wall"))
                        {
                            safeMoves.Add(newPos);
                        }
                    }
                }
                // Move to a safe spot at random
                if (safeMoves.Count > 0)
                {
                    Move(safeMoves[Random.Range(0, safeMoves.Count)]);
                    timesHidden++;
                }
                // Flail randomly if no safe spot
                else if (validMoves.Count > 0)
                {
                    Move(validMoves[Random.Range(0, validMoves.Count)]);
                    timesSpotted++;
                    seenIndex = timesMoved;
                }
                else
                {
                    timesSpotted++;
                    seenIndex = timesMoved;
                }
            }
            // If observer can't see you, hidden
            else
            {
                timesHidden++;
            }
            prevTime = Time.time;
        }
    }

    // search all possible spaces the observer could move next
    // pick next location based on safety from future observers
    void LookAhead()
    {
        // Can only move one space every 1/speed seconds
        if (Time.time - prevTime >= 1/speed)
        {
            // Check if observer can see you
            RaycastHit2D hit = Physics2D.Linecast(rb.position, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                // value: #observers that see newPos (lower is safer)
                Dictionary<Vector2, int> safeMoves = new Dictionary<Vector2, int>();
                List<Vector2> validMoves = new List<Vector2>();
                // Get safemoves, validmoves based on observer's next step
                (safeMoves, validMoves) = GetNextMoves(observer.rb.position, rb.position);
                // Move to a safe spot based on safest metric
                if (safeMoves.Count > 0)
                {
                    Vector2 pos = safeMoves.Aggregate((x, y) => x.Value < y.Value ? x : y).Key;
                    Move(pos);
                    timesHidden++;
                }
                // Flail randomly if no safe spot
                else if (validMoves.Count > 0)
                {
                    Move(validMoves[Random.Range(0, validMoves.Count)]);
                    timesSpotted++;
                    seenIndex = timesMoved;
                }
                else
                {
                    timesSpotted++;
                    seenIndex = timesMoved;
                }
            }
            // If observer can't see you, hidden
            else
            {
                timesHidden++;
            }
            prevTime = Time.time;
        }
    }

    void IsovistMetric()
    {
        // Can only move one space every 1/speed seconds
        if (Time.time - prevTime >= 1 / speed)
        {
            List<IsovistPoint> points = isovistManager.GetAdjacent(transform.position);
            // Remove obstructed points
            for(int i = 0; i < points.Count; i++)
            {
                IsovistPoint pt = points[i];
                // Detect obstacle at pt
                bool posObstructed = Physics2D.OverlapCircle(pt.transform.position, 0.5f, LayerMask.GetMask("Wall"));
                if(posObstructed)
                {
                    points.Remove(pt);
                    i--;
                    continue;
                }
            }
            points = points.OrderBy(x => x.visibility).ToList();
            if(points.Count > 0)
            {
                // Remove points that can be seen by observer
                List<IsovistPoint> safePoints = new List<IsovistPoint>(points);
                for(int i = 0; i < safePoints.Count; i++)
                {
                    IsovistPoint pt = safePoints[i];
                    RaycastHit2D hit = Physics2D.Linecast(pt.transform.position, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
                    if(hit.collider.gameObject.CompareTag("Player"))
                    {
                        safePoints.Remove(pt);
                        i--;
                        continue;
                    }
                }
                // Pick the lowest visibility safe point
                if(safePoints.Count > 0)
                {
                    Vector2 newPos = safePoints[0].transform.position;
                    if(newPos != rb.position)
                    {
                        Move(newPos);
                    }
                    timesHidden++;
                }
                else
                {
                    RaycastHit2D hit = Physics2D.Linecast(rb.position, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
                    // Check if current position is safe
                    if (!hit.collider.gameObject.CompareTag("Player"))
                    {
                        timesHidden++;
                    }
                    // No position is safe - move to lowest visibility point
                    else
                    {
                        Vector2 newPos = points[0].transform.position;
                        if (newPos != rb.position)
                        {
                            Move(newPos);
                        }
                        timesSpotted++;
                        seenIndex = timesMoved;
                    }
                }
            }
            // No valid moves -> This shouldn't happen
            else
            {
                
            }
            prevTime = Time.time;
        }
    }

    // Isovist metric, but weight points based on visibility from observer
    void IsovistLookAhead()
    {
        // Can only move one space every 1/speed seconds
        if (Time.time - prevTime >= 1 / speed)
        {
            isovistManager.ComputeLookAheadIsovists();
            List<IsovistPoint> points = isovistManager.GetAdjacent(transform.position);
            // Remove obstructed points
            for (int i = 0; i < points.Count; i++)
            {
                IsovistPoint pt = points[i];
                // Detect obstacle at pt
                bool posObstructed = Physics2D.OverlapCircle(pt.transform.position, 0.5f, LayerMask.GetMask("Wall"));
                if (posObstructed)
                {
                    points.Remove(pt);
                    i--;
                    continue;
                }
            }
            points = points.OrderBy(x => x.visibility).ToList();
            if (points.Count > 0)
            {
                // Remove points that can be seen by observer
                List<IsovistPoint> safePoints = new List<IsovistPoint>(points);
                for (int i = 0; i < safePoints.Count; i++)
                {
                    IsovistPoint pt = safePoints[i];
                    RaycastHit2D hit = Physics2D.Linecast(pt.transform.position, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
                    if (hit.collider.gameObject.CompareTag("Player"))
                    {
                        safePoints.Remove(pt);
                        i--;
                        continue;
                    }
                }
                // Pick the lowest visibility safe point
                if (safePoints.Count > 0)
                {
                    Vector2 newPos = safePoints[0].transform.position;
                    if (newPos != rb.position)
                    {
                        Move(newPos);
                    }
                    timesHidden++;
                }
                else
                {
                    RaycastHit2D hit = Physics2D.Linecast(rb.position, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
                    // Check if current position is safe
                    if (!hit.collider.gameObject.CompareTag("Player"))
                    {
                        timesHidden++;
                    }
                    // No position is safe - move to lowest visibility point
                    else
                    {
                        Vector2 newPos = points[0].transform.position;
                        if (newPos != rb.position)
                        {
                            Move(newPos);
                        }
                        timesSpotted++;
                        seenIndex = timesMoved;
                    }
                }
            }
            // No valid moves -> This shouldn't happen
            else
            {

            }
            prevTime = Time.time;
        }
    }

    // Predict a few steps into the future
    void DeepLookAhead()
    {
        // Can only move one space every 1/speed seconds
        if (Time.time - prevTime >= 1 / speed)
        {
            // value: sum of 1 / #observers that can see the point (higher is better)
            Dictionary<Vector2, int> safeMoves;
            List<Vector2> validMoves;
            (safeMoves, validMoves) = GetNextMoves(observer.rb.position, rb.position);

            // Get heuristic values for each safe point
            List<Vector2> moves = new List<Vector2>(safeMoves.Keys);
            foreach(Vector2 move in moves)
            {
                safeMoves[move] = (int) DeepLookAheadStep(rb.position, observer.rb.position, lookAheadDepth);
            }
            // Move to safest point
            if (safeMoves.Count > 0)
            {
                Vector2 pos = safeMoves.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                Move(pos);
                timesHidden++;
            }
            else
            {
                RaycastHit2D hit = Physics2D.Linecast(rb.position, observer.rb.position, LayerMask.GetMask("Player", "Wall"));
                // Check if current position is safe
                if (!hit.collider.gameObject.CompareTag("Player"))
                {
                    timesHidden++;
                }
                // Flail randomly if no safe spot
                else if (validMoves.Count > 0)
                {
                    Move(validMoves[Random.Range(0, validMoves.Count)]);
                    timesSpotted++;
                    seenIndex = timesMoved;
                }
                else
                {
                    timesSpotted++;
                    seenIndex = timesMoved;
                }
            }
            prevTime = Time.time;
        }
    }

    #endregion

    public void EndExperiment()
    {
        Debug.Log("Spotted " + timesSpotted + " times");
        Debug.Log("Spotted " + 100.0 * timesSpotted / (timesSpotted + timesHidden) + "% of the time");
        Debug.Log("Steps until first spotted: " + seenIndex);
        Debug.Break();
    }

    Vector2 Diagonal(float degree)
    {
        return new Vector2(Mathf.Cos(degree / 180 * Mathf.PI), Mathf.Sin(degree / 180 * Mathf.PI));
    }

    void Move(Vector2 pos)
    {
        /*/ Move ghost
        Ghost ghost = FindObjectOfType<Ghost>();
        if(ghost != null)
        {
            Destroy(ghost.gameObject);
        }
        Instantiate(ghostPrefab, transform.position, Quaternion.identity);
        */
        transform.position = pos;
        timesMoved++;
    }

    // Returns possible next moves for agent based on observer's next move
    (Dictionary<Vector2, int>, List<Vector2>) GetNextMoves(Vector2 observerPos, Vector2 myPos)
    {
        RaycastHit2D hit;
        // value: #observers that see newPos (lower is safer)
        Dictionary<Vector2, int> safeMoves = new Dictionary<Vector2, int>();
        List<Vector2> validMoves = new List<Vector2>();
        // Add to dictionary
        foreach (Vector2 direction in directions)
        {
            Vector2 newPos = myPos + direction;
            // Detect obstacle at newPos
            bool posObstructed = Physics2D.OverlapCircle(newPos, 0.5f, LayerMask.GetMask("Wall"));
            if (!posObstructed)
            {
                // Cast ray from newPos
                hit = Physics2D.Linecast(newPos, observerPos, LayerMask.GetMask("Player", "Wall"));
                if (hit.collider != null && hit.collider.gameObject.CompareTag("Wall"))
                {
                    safeMoves.Add(newPos, 0);
                    // Cast ray from newPos to each possible next spot for observer
                    foreach (Vector2 d in directions)
                    {
                        Vector2 markerPos = observerPos + d;
                        // Create temporary observer marker for raycast
                        GameObject obsMarker = Instantiate(obsMarkerPrefab, markerPos, Quaternion.identity);
                        hit = Physics2D.Linecast(newPos, markerPos, LayerMask.GetMask("Player", "Wall"));
                        if (hit.collider.gameObject.CompareTag("Player"))
                        {
                            // Move is less safe
                            safeMoves[newPos]++;
                        }
                        Destroy(obsMarker);
                    }
                }
                else
                {
                    validMoves.Add(newPos);
                }
            }
        }
        return (safeMoves, validMoves);
    }

    // Calculate heuristic for DeepLookAhead algorithm
    float DeepLookAheadStep(Vector2 agentPos, Vector2 observerPos, int depth)
    {
        if(depth == 0)
        {
            return 0;
        }
        Dictionary<Vector2, int> safeMoves;
        (safeMoves, _) = GetNextMoves(observerPos, agentPos);
        float result = 0;
        // Compute heuristic: 1 / #observers that can see the point (higher is better)
        foreach(int value in safeMoves.Values)
        {
            result += 1.0f / value;
        }
        foreach(Vector2 move in safeMoves.Keys)
        {
            foreach(Vector2 direction in directions)
            {
                result += DeepLookAheadStep(move, observerPos + direction, depth - 1);
            }
        }
        return result;
    }
}

public enum HidingAlgorithm
{
    Baseline, LookAhead, IsovistMetric, IsovistLookAhead, DeepLookAhead
}
