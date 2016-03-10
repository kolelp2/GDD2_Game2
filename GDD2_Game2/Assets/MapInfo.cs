using UnityEngine;
using System.Collections;
using System;

public class MapInfo : MonoBehaviour {
    [SerializeField]
    public readonly static int DayLengthInSeconds = 600;
    int dayNumber = 0;
    public static float WorldTimeInDays
    {
        get
        {
            return Time.time / DayLengthInSeconds;
        }
    }

    public static event EventHandler<DayEndEventArgs> DayEndEvent;

    Vector2 mapSize;
    public Vector2 MapSize
    {
        get { return mapSize; }
    }
    Vector2 mapPos;
    public Vector2 MapPos
    {
        get { return mapPos; }
    }
    // Use this for initialization
    void Start () {
        BoxCollider2D mapBox = (BoxCollider2D)gameObject.GetComponent(typeof(BoxCollider2D));
        mapSize = mapBox.size;

        mapPos = new Vector2(mapBox.transform.position.x - mapBox.size.x / 2, mapBox.transform.position.y - mapBox.size.y / 2);
	}
	
	// Update is called once per frame
	void Update () {
        //if the current day (running time divided by day length floored) doesn't equal the recorded current day,
        if ((int)Math.Floor(Time.time / DayLengthInSeconds) != dayNumber)
        {
            //increment day number and emit day end event
            dayNumber++;
            DayEndEvent(this, new DayEndEventArgs());
        }
	}

    //takes a vector representing a point in the world and returns a vector representing the corresponding point in a grid of the given precision within the map
    public Vector2 WorldToGridSpace(Vector2 worldPos, int gridPrecision)
    {
        return (worldPos - mapPos) / gridPrecision;
    }

    //same as above, but from grid space to world space
    public Vector2 GridToWorldSpace(Vector2 vfPos, int gridPrecision)
    {
        return (vfPos * gridPrecision) + mapPos;
    }

    //same as WorldToGridSpace, but only affects x and y
    public Vector3 WorldToGridSpace(Vector3 worldPos, int gridPrecision)
    {
        //save Z, transform X and Y
        float z = worldPos.z;
        Vector2 conv = WorldToGridSpace((Vector2)worldPos, gridPrecision);

        //return new X and Y with old Z
        return new Vector3(conv.x, conv.y, z);
    }

    //same as above, but from grid space to world space
    public Vector3 GridToWorldSpace(Vector3 vfPos, int gridPrecision)
    {
        //save Z, transform X and Y
        float z = vfPos.z;
        Vector2 conv = GridToWorldSpace((Vector2)vfPos, gridPrecision);

        //return new X and Y with old Z
        return new Vector3(conv.x, conv.y, z);
    }

    //floors the vector components so they can be used as a 2d array index
    public Vector2 PosToIndex(Vector2 pos)
    {
        return new Vector2((int)Math.Floor(pos.x), (int)Math.Floor(pos.y));
    }

    public Vector2 WorldToGridIndex(Vector2 worldPos, int gridPrecision)
    {
        return PosToIndex(WorldToGridSpace(worldPos, gridPrecision));
    }

    public bool IsWorldPosOnMap(Vector2 worldPos)
    {
        //worldPos = WorldToGridSpace(worldPos, gridPrecision);
        worldPos = worldPos - mapPos;
        return !(worldPos.x > mapSize.x || worldPos.x < 0 || worldPos.y > mapSize.y || worldPos.y < 0);
    }

    public bool IsGridPosOnMap(Vector2 gridPos, int gridPrecision)
    {
        gridPos = GridToWorldSpace(gridPos, gridPrecision);
        return IsWorldPosOnMap(gridPos);
    }

    //returns the centroid of an array of points
    public static Vector2 GetCentroid(Vector2[] points)
    {
        Vector2 sum = Vector2.zero;
        for (int c = 0; c < points.Length; c++)
        {
            sum += points[c];
        }
        return sum / points.Length;

    }
}

public class DayEndEventArgs : EventArgs
{
    public DayEndEventArgs()
    {
    }
}
