using UnityEngine;
using System.Collections;

public class Water : MonoBehaviour {
    GOTracker myGOT;
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
        myGOT.Report(this, (int)ObjectType.Water);
    }

    // Update is called once per frame
    void Update () {
        
	}

    public bool Harvest()
    {
        return true;
    }
}
