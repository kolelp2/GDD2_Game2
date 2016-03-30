using UnityEngine;
using System.Collections;

public class Fuel : ResourceNode {
    GOTracker myGOT;

    [SerializeField]
    float stock = 3000;
    [SerializeField]
    public readonly static float harvestRange = .7f;
    static float drawDepth = -.5f;
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

        //initializing random fuel sprites
        int fuelSelectInt = Random.Range(1, 3);

        SpriteRenderer sr = (SpriteRenderer)gameObject.GetComponent(typeof(SpriteRenderer));
        sr.sprite = Resources.Load("resource_tree_" + (fuelSelectInt.ToString()), typeof(Sprite)) as Sprite;
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

    public override float GetHarvestRange()
    {
        return harvestRange;
    }
    public override float GetHarvestRange(Vector2 pos)
    {
        return harvestRange;
    }
}
