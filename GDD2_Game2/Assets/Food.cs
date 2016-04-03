using UnityEngine;
using System.Collections;

public class Food : ResourceNode
{
    GOTracker myGOT;
    MapInfo mi;

    [SerializeField]
    float initialStock = 3000;
    [SerializeField]
    float respawnDistance = 50;
    [SerializeField]
    public readonly static float harvestRange = .7f;
    static float drawDepth = -.5f;
    float stock;

    public override ResourceType ResourceType
    {
        get { return ResourceType.FoodRaw; }
    }

    // Use this for initialization
    void Start () {
        stock = initialStock;
        GameObject map = GameObject.Find("Map");
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));
        mi = (MapInfo)map.GetComponent(typeof(MapInfo));
        transform.position += new Vector3(0, 0, drawDepth);

        //initializing random food sprites
        int foodSelectInt = Random.Range(1, 3);

        SpriteRenderer sr = (SpriteRenderer)gameObject.GetComponent(typeof(SpriteRenderer));
        sr.sprite = Resources.Load("resource_animal_" + (foodSelectInt.ToString()), typeof(Sprite)) as Sprite;
    }
	
	// Update is called once per frame
	void Update () {
        myGOT.Report(this, (int)ObjectType.Food);
        //if no stock left
        if (stock <= 0)
        {
            //we ded
            myGOT.ReportDeath(this, ObjectType.Food);
            Respawn();
        }
	}
    void Respawn()
    {
        Vector2 newPosition;
        do
            newPosition = transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * respawnDistance);
        while (mi.GetAltitudeAtPos(newPosition) != 0 || !mi.IsWorldPosOnMap(newPosition));
        transform.position = new Vector3(newPosition.x, newPosition.y, drawDepth);
        stock = initialStock;
        myGOT.Report(this, (int)ObjectType.Food);
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
    public override float GetHarvestRange()
    {
        return harvestRange;
    }
    public override float GetHarvestRange(Vector2 pos)
    {
        return harvestRange;
    }
}

public abstract class ResourceNode : MonoBehaviour
{
    //range at which this resource may be harvested
    public abstract float GetHarvestRange();
    public abstract float GetHarvestRange(Vector2 pos);

    //the type - see ResourceType enum
    public abstract ResourceType ResourceType { get; }

    //duh
    public abstract float Harvest(float amt);
}
