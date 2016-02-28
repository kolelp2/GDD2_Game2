using UnityEngine;
using System.Collections;

public class Fuel : ResourceNode {
    GOTracker myGOT;

    [SerializeField]
    float stock = 3000;
    [SerializeField]
    public readonly static float harvestRange = .3f;
    static int drawDepth = -2;
    public override float HarvestRange
    {
        get { return harvestRange; }
    }
    public override ResourceType ResourceType
    {
        get { return ResourceType.FuelRaw; }
    }
    // Use this for initialization
    void Start () {
        myGOT = (GOTracker)GameObject.Find("Map").GetComponent(typeof(GOTracker));
        transform.position += new Vector3(0, 0, drawDepth);

        //we can't report in start because the GOT might not be ready, so we put it off till the second frame
        StartCoroutine(LateStart());        
    }

    IEnumerator LateStart()
    {
        yield return null;
        myGOT.Report(this, ObjectType.Fuel);
    }
	
	// Update is called once per frame
	void Update () {
        if (stock <= 0)
        {
            myGOT.ReportDeath(this, ObjectType.Fuel);
            Destroy(gameObject);
        }
    }
    //see comments in food
    public override float Harvest(float amt)
    {
        if (stock >= amt)
        {
            stock -= amt;
            return amt;
        }
        else
        {
            float returnAmt = stock;
            stock = 0;
            return returnAmt;
        }
    }
}
