using UnityEngine;
using System.Collections.Generic;

public class GOTracker : MonoBehaviour {
    Dictionary<System.Type, HashSet<MonoBehaviour>> objDictionary = new Dictionary<System.Type, HashSet<MonoBehaviour>>();
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public HashSet<MonoBehaviour> GetObjsWithinRange(Vector2 position, float radius, System.Type type)
    {
        HashSet<MonoBehaviour> activeAndInRange = new HashSet<MonoBehaviour>();
        if (!objDictionary.ContainsKey(type)) return activeAndInRange;
        foreach (MonoBehaviour mb in objDictionary[type])
            //if object is active, and the squared distance from it to the given position is less than or equal to the given radius squared
            if (((Vector2)mb.transform.position - position).sqrMagnitude <= radius * radius)
                activeAndInRange.Add(mb);

        return activeAndInRange;
    }

    public void Report(MonoBehaviour mb, System.Type type)
    {
        //if the obj dictionary doesn't contain this type yet, add it
        if (!objDictionary.ContainsKey(type)) objDictionary.Add(type, new HashSet<MonoBehaviour>());
        //get the set for this type
        HashSet<MonoBehaviour> list = objDictionary[type];
        //add the script to it if it isn't already there
        if (!list.Contains(mb)) list.Add(mb);
    }

    public void ReportDeath<T>(MonoBehaviour deadThing)
    {
        //remove the dead thing from the object dictionary
        if (objDictionary.ContainsKey(typeof(T))) objDictionary[typeof(T)].Remove(deadThing);
    }
}
