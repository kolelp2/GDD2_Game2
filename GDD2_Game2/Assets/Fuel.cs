using UnityEngine;
using System.Collections;

public class Fuel : ResourceNode {
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
        get { return ResourceType.FuelRaw; }
    }

    // Use this for initialization
    void Start () {
        stock = initialStock;
        GameObject map = GameObject.Find("Map");
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));
        mi = (MapInfo)map.GetComponent(typeof(MapInfo));
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

    void Respawn()
    {
        Vector2 newPosition;
        do
            newPosition = transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * respawnDistance);
        while (mi.GetAltitudeAtPos(newPosition) != 0 || !mi.IsWorldPosOnMap(newPosition));
        transform.position = new Vector3(newPosition.x, newPosition.y, drawDepth);
        stock = initialStock;
        myGOT.Report(this, (int)ObjectType.Fuel);
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
