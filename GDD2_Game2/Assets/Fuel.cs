using UnityEngine;
using System.Collections;

public class Fuel : MonoBehaviour {
    GOTracker myGOT;
    [SerializeField]
    int capacity = 100;
    public readonly static float harvestRange = 5.0f;
    [SerializeField]
    float interationRadius = 10.0f;
    public float InteractionRadius
    {
        get { return interationRadius; }
    }
    // Use this for initialization
    void Start () {
        myGOT = (GOTracker)GameObject.Find("Map").GetComponent(typeof(GOTracker));
        StartCoroutine(LateStart());        
    }

    IEnumerator LateStart()
    {
        yield return null;
        myGOT.Report(this, ObjectType.Fuel);
    }
	
	// Update is called once per frame
	void Update () {
        if (capacity <= 0)
        {
            myGOT.ReportDeath(this, ObjectType.Fuel);
            Destroy(this);
        }
    }
    //if capacity isn't 0, decrement it and send true to indicate that the harvester got a resource
    public bool Harvest()
    {
        if (capacity <= 0)
            return false;
        else
        {
            capacity--;
            return true;
        }
    }
}
