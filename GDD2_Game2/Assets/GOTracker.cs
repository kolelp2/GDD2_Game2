using UnityEngine;
using System.Collections.Generic;
using System;

public class GOTracker : MonoBehaviour {
    Dictionary<System.Type, Dictionary<Vector2,List<MonoBehaviour>>> objDictionary = new Dictionary<Type, Dictionary<Vector2, List<MonoBehaviour>>>();
    Dictionary<MonoBehaviour, Vector2> unitWorldPositions = new Dictionary<MonoBehaviour, Vector2>();

    MapInfo mi;

    [SerializeField]
    int mapPrecision = 1;

	// Use this for initialization
	void Start () {
        mi = (MapInfo)gameObject.GetComponent(typeof(MapInfo));
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    public List<MonoBehaviour> GetObjsInRange(Vector2 position, float radius, System.Type type)
    {
        Dictionary<Vector2,List<MonoBehaviour>> objsOfType;
        if (!objDictionary.TryGetValue(type, out objsOfType)) return new List<MonoBehaviour>();

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

    public void Report(MonoBehaviour mb, System.Type type)
    {
        //if the obj dictionary doesn't contain this type yet, add it
        if (!objDictionary.ContainsKey(type))
        {
            objDictionary.Add(type, new Dictionary<Vector2, List<MonoBehaviour>>());
        }

        Vector2 gridPos = mi.WorldToGridIndex(mb.gameObject.transform.position, mapPrecision);

        //get the set for this map square for this type
        Dictionary<Vector2, List<MonoBehaviour>> typeList = objDictionary[type];
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

    public void ReportDeath<T>(MonoBehaviour deadThing)
    {
        Vector2 deadThingGridPos = mi.WorldToGridIndex(deadThing.gameObject.transform.position, mapPrecision);
        Vector2 deadThingPrevGridPos = mi.WorldToGridIndex(unitWorldPositions[deadThing], mapPrecision);
        //remove the dead thing from the object dictionary and the position dictionary
        if (objDictionary.ContainsKey(typeof(T)))
        {
            var typeList = objDictionary[typeof(T)];
            typeList[deadThingGridPos].Remove(deadThing);
        }
        unitWorldPositions.Remove(deadThing);
    }

    //public Vector2 World
}
