using UnityEngine;
using System.Collections.Generic;

public class GOTracker : MonoBehaviour {
    List<GameObject> objList = new List<GameObject>();
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public List<GameObject> GetObjsWithinRange(Vector2 position, float radius)
    {
        List<GameObject> activeAndInRange = new List<GameObject>(objList.Count);
        foreach (GameObject go in objList)
            //if object is active, and the squared distance from it to the given position is less than or equal to the given radius squared
            if (go.activeInHierarchy && ((Vector2)go.transform.position - position).sqrMagnitude <= radius * radius)
                activeAndInRange.Add(go);

        return activeAndInRange;
    }

    public void Report(GameObject go)
    {
        if (!objList.Contains(go)) objList.Add(go);
    }
}
