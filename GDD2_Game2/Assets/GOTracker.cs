using UnityEngine;
using System.Collections.Generic;
using System;

public class GOTracker : MonoBehaviour {
    Dictionary<System.Type, List<MonoBehaviour>[,]> objDictionary = new Dictionary<System.Type, List<MonoBehaviour>[,]>();
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
        if (!objDictionary.ContainsKey(type)) return new List<MonoBehaviour>();
        //return objDictionary[type];
        Vector2 rangeMin = mi.WorldToGridIndex(new Vector2(position.x - radius, position.y - radius), mapPrecision);
        Vector2 rangeMax = mi.WorldToGridIndex(new Vector2(position.x + radius, position.y + radius), mapPrecision);
        List<MonoBehaviour>[,] objOfType = objDictionary[type];
        List<MonoBehaviour> activeAndInRange = new List<MonoBehaviour>((int)(rangeMax-rangeMin).sqrMagnitude/2);//initial capacity is totally spitballed

        for (int row = (int)rangeMin.x; row <= rangeMax.x; row++)
            for (int col = (int)rangeMin.y; col <= rangeMax.y; col++)
                foreach (MonoBehaviour mb in objOfType[row, col])
                    //if object is active, and the squared distance from it to the given position is less than or equal to the given radius squared
                    if (((Vector2)mb.transform.position - position).sqrMagnitude <= radius * radius)
                        activeAndInRange.Add(mb);

        return activeAndInRange;
    }

    public void Report(MonoBehaviour mb, System.Type type)
    {
        //if the obj dictionary doesn't contain this type yet, add it
        if (!objDictionary.ContainsKey(type)) objDictionary.Add(type, new List<MonoBehaviour>[(int)Math.Floor(mi.MapSize.x), (int)Math.Floor(mi.MapSize.y)]);

        Vector2 gridPos = mi.WorldToGridIndex(mb.gameObject.transform.position, mapPrecision);

        //get the set for this type
        List<MonoBehaviour> list = objDictionary[type][(int)gridPos.x, (int)gridPos.y];
        //add the script to it if it isn't already there
        if (!list.Contains(mb)) list.Add(mb);

        //if the position dictionary has an entry for this unit...
        if (unitWorldPositions.ContainsKey(mb))
        {
            Vector2 prevGridPos = mi.WorldToGridIndex(unitWorldPositions[mb], mapPrecision);

            //...and the current map square is different than the previous map square
            if (prevGridPos != gridPos)
                //remove the unit from the previous map square
                objDictionary[type][(int)prevGridPos.x, (int)prevGridPos.y].Remove(mb);
        }
    }

    public void ReportDeath<T>(MonoBehaviour deadThing)
    {
        Vector2 deadThingGridPos = mi.WorldToGridIndex(deadThing.gameObject.transform.position, mapPrecision);
        //remove the dead thing from the object dictionary and the position dictionary
        if (objDictionary.ContainsKey(typeof(T))) objDictionary[typeof(T)][(int)deadThingGridPos.x, (int)deadThingGridPos.y].Remove(deadThing);
        if (unitWorldPositions.ContainsKey(deadThing)) unitWorldPositions.Remove(deadThing);
    }

    //public Vector2 World
}
