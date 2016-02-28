using UnityEngine;
using System.Collections;

public class Water : ResourceNode
{
    GOTracker myGOT;
    public readonly static float harvestRange = .3f;
    public override float HarvestRange
    {
        get { return harvestRange; }
    }
    public override ResourceType ResourceType
    {
        get { return ResourceType.WaterRaw; }
    }
    // Use this for initialization
    void Start () {
        myGOT = (GOTracker)GameObject.Find("Map").GetComponent(typeof(GOTracker));

        //we can't report in start because the GOT might not be ready, so we put it off till the second frame
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

    public override float Harvest(float amt)
    {
        return amt;
    }

    public float? GetStock(GameObject go)
    {
        return float.MaxValue;
    }
}
