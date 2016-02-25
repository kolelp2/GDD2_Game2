using UnityEngine;
using System.Collections.Generic;
using System;

public class GOTracker : MonoBehaviour {
    Dictionary<System.Type, List<MonoBehaviour>[,]> objDictionary = new Dictionary<System.Type, List<MonoBehaviour>[,]>();

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
        List<MonoBehaviour> objOfType = objDictionary[type];
        List<MonoBehaviour> activeAndInRange = new List<MonoBehaviour>(objOfType.Count);
        foreach (MonoBehaviour mb in objOfType)
            //if object is active, and the squared distance from it to the given position is less than or equal to the given radius squared
            if (((Vector2)mb.transform.position - position).sqrMagnitude <= radius * radius)
                activeAndInRange.Add(mb);

        return activeAndInRange;
    }

    public void Report(MonoBehaviour mb, System.Type type, Vector2 worldPos)
    {
        //if the obj dictionary doesn't contain this type yet, add it
        if (!objDictionary.ContainsKey(type)) objDictionary.Add(type, new List<MonoBehaviour>[(int)Math.Floor(mi.MapSize.x), (int)Math.Floor(mi.MapSize.y)]);
        //get the set for this type
        List<MonoBehaviour> list = objDictionary[type];
        //add the script to it if it isn't already there
        if (!list.Contains(mb)) list.Add(mb);
    }

    public void ReportDeath<T>(MonoBehaviour deadThing)
    {
        //remove the dead thing from the object dictionary
        if (objDictionary.ContainsKey(typeof(T))) objDictionary[typeof(T)].Remove(deadThing);
    }

    //public Vector2 World
}
