using UnityEngine;
using System.Collections.Generic;
using System;

public class GOTracker : MonoBehaviour {
    Dictionary<Vector2,List<MonoBehaviour>>[] objDictionary;
    Dictionary<MonoBehaviour, Vector2> unitWorldPositions = new Dictionary<MonoBehaviour, Vector2>();

    public readonly static int resourceNodeTypeCount = 3;

    MapInfo mi;

    [SerializeField]
    int mapPrecision = 1;

    void Awake()
    {
        objDictionary = new Dictionary<Vector2, List<MonoBehaviour>>[Enum.GetNames(typeof(ObjectType)).Length];
        for (int c = 0; c < objDictionary.Length; c++)
            objDictionary[c] = new Dictionary<Vector2, List<MonoBehaviour>>();
        mi = (MapInfo)gameObject.GetComponent(typeof(MapInfo));
    }

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    public List<MonoBehaviour> GetObjsInRange(Vector2 pos, float radius, ObjectType objType)
    {
        return GetObjsInRange(pos, radius, (int)objType);
    }

    public List<MonoBehaviour> GetObjsInRange(Vector2 position, float radius, int objType)
    {
        if (objType>objDictionary.Length) return new List<MonoBehaviour>();

        var objsOfType = objDictionary[objType];

        Vector2 rangeMin = mi.WorldToGridIndex(new Vector2(position.x - radius, position.y - radius), mapPrecision);
        Vector2 rangeMax = mi.WorldToGridIndex(new Vector2(position.x + radius, position.y + radius), mapPrecision);

        List<MonoBehaviour> activeAndInRange = new List<MonoBehaviour>((int)(rangeMax-rangeMin).sqrMagnitude/2);//initial capacity is totally spitballed

        for (int row = (int)rangeMin.x; row <= rangeMax.x; row++)
        {
            for (int col = (int)rangeMin.y; col <= rangeMax.y; col++)
            {
                List<MonoBehaviour> mapSquareList;
                if (!objsOfType.TryGetValue(new Vector2(row, col), out mapSquareList)) continue;
                for(int c = 0;c<mapSquareList.Count;c++)
                {
                    MonoBehaviour mb = mapSquareList[c];
                    //if object is active, and the squared distance from it to the given position is less than or equal to the given radius squared
                    if (((Vector2)mb.transform.position - position).sqrMagnitude <= radius * radius)
                        activeAndInRange.Add(mb);
                }
            }
        }

        return activeAndInRange;
        //return new List<MonoBehaviour>();
    }

    public void Report(MonoBehaviour mb, ObjectType objType)
    {
        Report(mb, (int)objType);
    }

    public void Report(MonoBehaviour mb, int objType)
    {
        //if the obj dictionary doesn't contain this type yet, add it
        if (objType > objDictionary.Length) return;

        Vector2 gridPos = mi.WorldToGridIndex(mb.gameObject.transform.position, mapPrecision);

        //get the set for this map square for this type
        Dictionary<Vector2, List<MonoBehaviour>> typeList = objDictionary[objType];
        if (!typeList.ContainsKey(gridPos)) typeList.Add(gridPos, new List<MonoBehaviour>());
        List<MonoBehaviour> squareList = typeList[gridPos];
        //add the script to it if it isn't already there
        if (!squareList.Contains(mb)) squareList.Add(mb);

        Vector2 unitWorldPos;
        if (unitWorldPositions.TryGetValue(mb, out unitWorldPos))
        {
            Vector2 prevGridIndex = mi.WorldToGridIndex(unitWorldPos, mapPrecision);

            //...and the current map square is different than the previous map square
            if (prevGridIndex != gridPos)
            {
                //remove the unit from the previous map square
                List<MonoBehaviour> prevSquareList = new List<MonoBehaviour>();
                if (typeList.TryGetValue(prevGridIndex, out prevSquareList))
                    prevSquareList.Remove(mb);

                //if the previous map square is empty, trim empty elements
                if (prevSquareList.Count == 0) prevSquareList.TrimExcess();
            }
        }
        unitWorldPositions[mb] = mb.gameObject.transform.position;
    }

    public void ReportDeath(MonoBehaviour dt, ObjectType dtType)
    {
        ReportDeath(dt, (int)dtType);
    }

    public void ReportDeath(MonoBehaviour deadThing, int deadThingType)
    {
        //report first to make sure we have the latest info
        Report(deadThing, deadThingType);

        //remove the dead thing from the object dictionary and the position dictionary
        if (deadThingType<objDictionary.Length)
        {
            Vector2 deadThingGridPos = mi.WorldToGridIndex(deadThing.gameObject.transform.position, mapPrecision);
            //Vector2 deadThingPrevGridPos = mi.WorldToGridIndex(unitWorldPositions[deadThing], mapPrecision);
            var typeList = objDictionary[deadThingType];
            typeList[deadThingGridPos].Remove(deadThing);
        }
        unitWorldPositions.Remove(deadThing);
    }

    //public Vector2 World
}

//it's very important that resource node types be first by index
public enum ObjectType { Food = 0, Fuel = 1, Water = 2, Human = 3, Zombie = 4, Camp = 5 };
