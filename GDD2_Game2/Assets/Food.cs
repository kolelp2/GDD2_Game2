using UnityEngine;
using System.Collections;

public class Food : ResourceNode
{
    GOTracker myGOT;

    [SerializeField]
    float stock = 3000;
    [SerializeField]
    public readonly static float harvestRange = .3f;
    static float drawDepth = -.5f;

    public override float HarvestRange
    {
        get { return harvestRange; }
    }
    public override ResourceType ResourceType
    {
        get { return ResourceType.FoodRaw; }
    }

    // Use this for initialization
    void Start () {
        myGOT = (GOTracker)GameObject.Find("Map").GetComponent(typeof(GOTracker));
        transform.position += new Vector3(0, 0, drawDepth);
    }
	
	// Update is called once per frame
	void Update () {
        myGOT.Report(this, (int)ObjectType.Food);
        //if no stock left
        if (stock <= 0)
        {
            //we ded
            myGOT.ReportDeath(this, ObjectType.Food);
            Destroy(gameObject);
        }
	}

    //returns a number of resources to the caller as a float. will match the requested amount until stock is depleted
    public override float Harvest(float amt)
    {
        //if we have enough stock to cover the request
        if (stock >= amt)
        {       
            //subtract amount from stock and return amount     
            stock -= amt;
            return amt;
        }
        //if we don't
        else
        {
            //return what's left of our stock and set stock to 0
            float returnAmt = stock;
            stock = 0;
            return returnAmt;
        }
    }
}

public abstract class ResourceNode : MonoBehaviour
{
    //range at which this resource may be harvested
    public abstract float HarvestRange { get; }

    //the type - see ResourceType enum
    public abstract ResourceType ResourceType { get; }

    //duh
    public abstract float Harvest(float amt);
}
