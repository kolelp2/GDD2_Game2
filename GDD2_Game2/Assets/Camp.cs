using UnityEngine;
using System.Collections;

public class Camp : MonoBehaviour {
    [SerializeField]
    public readonly static float minInteractRadius = 2.0f;
    [SerializeField]
    float strengthForHalfMultiplier = 2000;
    [SerializeField]
    float multiplierStrength = 5;
    float interationRadius = 3.0f;
    float campStrength = 0;
    public float Multiplier
    {
        get
        {
            float mult = (1 - (strengthForHalfMultiplier / (campStrength + strengthForHalfMultiplier))) * multiplierStrength;
            return (mult > 1) ? mult : 1;
        }
    }
    Vector3 initialScale;
    static float drawDepth = -.8f;
    public float InteractionRadius
    {
        get
        {
            return interationRadius;
        }
    }
    GOTracker myGOT;
    SpriteRenderer mySR;

    //4 for the three raw resource node types plus ammo
    float[] inventory = new float[GOTracker.resourceNodeTypeCount + 1];
	// Use this for initialization
	void Start () {
        myGOT = (GOTracker)GameObject.Find("Map").GetComponent(typeof(GOTracker));
        mySR = (SpriteRenderer)gameObject.GetComponent(typeof(SpriteRenderer));
        initialScale = mySR.transform.localScale;
        for (int c = 0; c < inventory.Length; c++)
            inventory[c] = 0;
        //StartCoroutine(LateStart());
        transform.position += new Vector3(0, 0, drawDepth);
        myGOT.Report(this, ObjectType.Camp);
    }

    IEnumerator LateStart()
    {
        yield return null;
        myGOT.Report(this, ObjectType.Camp);
    }

    // Update is called once per frame
    void Update () {
        float mult = Multiplier;
        interationRadius = minInteractRadius * mult;
        mySR.transform.localScale = initialScale * mult;
	}

    //human asks to use a given type of raw resource, camp tells them whether it can give them one
    public float RequestToUseRawResource(ResourceType rawResourceType, float amt)
    {
        int rawResourceTypeInt = (int)rawResourceType;

        if (rawResourceTypeInt >= inventory.Length)
            return 0;
        else
        {
            //return requested amount by default
            float returnVal = amt;
            inventory[rawResourceTypeInt] -= amt;  //subtract requested amount from inventory
            if(inventory[rawResourceTypeInt]<0)
            {
                //if we brought the inventory below zero, remove the overdraw from the return value
                returnVal += inventory[rawResourceTypeInt];
                inventory[rawResourceTypeInt] = 0; //and set the inventory to 0
            }
            campStrength += returnVal;
            return returnVal;
        }
    }

    //human provides its inventory and offers one of a given resource type, camp tells them whether it takes one
    public bool DepositRawResource(float[] inv, ResourceType resourceType)
    {
        int resourceTypeInt = (int)resourceType;

        if (resourceTypeInt >= inventory.Length || inv[resourceTypeInt] <= 0)
            return false;
        else
        {
            float depositAmt = inv[resourceTypeInt];
            inventory[resourceTypeInt] += depositAmt;
            inv[resourceTypeInt] = 0;
            inventory[(int)ResourceType.Ammo] += 2 * depositAmt;
            return true;
        }
    }
}
