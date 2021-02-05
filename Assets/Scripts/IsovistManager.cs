using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsovistManager : MonoBehaviour
{
    public int x;
    public int y;
    public IsovistPoint point;
    public int weight; //Weight for points visible by observer in IsovistLookAhead

    IsovistPoint[,] points;
    Observer observer;

    private void Start()
    {
        observer = FindObjectOfType<Observer>();
    }

    public IsovistPoint[,] SetupIsovists()
    {
        points = new IsovistPoint[x, y];
        for(int i = 0; i < x; i++)
        {
            for(int j = 0; j < y; j++)
            {
                points[i, j] = Instantiate(point, transform.position + new Vector3(i, j, 0), Quaternion.identity);
            }
        }
        // For each point, calculate visibility from every other point
        RaycastHit2D hit;
        foreach (IsovistPoint pt in points)
        {
            foreach(IsovistPoint other in points)
            {
                if(pt.Equals(other))
                {
                    continue;
                }
                hit = Physics2D.Linecast(pt.transform.position, other.transform.position, LayerMask.GetMask("Wall"));
                if(hit.collider == null)
                {
                    pt.visibility++;
                }
            }
        }
        return points;
    }

    // For the IsovistLookAhead algorithm, computes isovist for all points
    // weighted against points that are visible to observer
    public void ComputeLookAheadIsovists()
    {
        // For each point, calculate visibility from every other point
        RaycastHit2D hit;
        foreach (IsovistPoint pt in points)
        {
            pt.visibility = 0;
            foreach (IsovistPoint other in points)
            {
                if (pt.Equals(other))
                {
                    continue;
                }
                hit = Physics2D.Linecast(pt.transform.position, other.transform.position, LayerMask.GetMask("Wall"));
                if (hit.collider == null)
                {
                    // Weight pt if it can be seen by observer
                    if(Vector2.Distance(other.transform.position, observer.rb.position) <= 1.5)
                    {
                        pt.visibility += weight;
                    }
                    else
                    {
                        pt.visibility++;
                    }
                }
            }
        }
    }

    public List<IsovistPoint> GetAdjacent(Vector2 pos)
    {
        IsovistPoint nearest = points[0, 0];
        int x = 0;
        int y = 0;
        for(int i = 0; i < points.GetLength(0); i++)
        {
            for(int j = 0; j < points.GetLength(1); j++)
            {
                IsovistPoint pt = points[i, j];
                if (Vector2.Distance(pt.transform.position, pos) < Vector2.Distance(nearest.transform.position, pos))
                {
                    nearest = pt;
                    x = i;
                    y = j;
                }
            }
        }
        List<IsovistPoint> result = new List<IsovistPoint>();
        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                if(i < 0 || i >= points.GetLength(0) || j < 0 || j >= points.GetLength(1))
                {
                    continue;
                }
                result.Add(points[i, j]);
            }
        }
        return result;
    }
}
