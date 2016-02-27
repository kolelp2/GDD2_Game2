using UnityEngine;
using System.Collections;

public class Camp : MonoBehaviour {
    [SerializeField]
    static float minInteractRadius = 10.0f;
    float interationRadius = 10.0f;
    public float InteractionRadius
    {
        get { return interationRadius; }
    }
    GOTracker myGOT;

    //4 for the three raw resource node types plus ammo
    int[] inventory = new int[GOTracker.resourceNodeTypeCount + 1];
	// Use this for initialization
	void Start () {
        myGOT = (GOTracker)GameObject.Find("Map").GetComponent(typeof(GOTracker));
        StartCoroutine(LateStart());
    }

    IEnumerator LateStart()
    {
        yield return null;
        myGOT.Report(this, ObjectType.Camp);
    }

    // Update is called once per frame
    void Update () {
	
	}

    //human asks to use a given type of raw resource, camp tells them whether it can give them one
    bool RequestToUseRawResource(ResourceTypeAll rawResourceType)
    {
        int rawResourceTypeInt = (int)rawResourceType;

        if (rawResourceTypeInt >= inventory.Length || inventory[rawResourceTypeInt] <= 0)
            return false;
        else
        {
            inventory[rawResourceTypeInt]--;
            return true;
        }
    }

    //human provides its inventory and offers one of a given resource type, camp tells them whether it takes one
    bool DepositRawResource(int[] inv, ResourceTypeAll resourceType)
    {
        int resourceTypeInt = (int)resourceType;

        if (resourceTypeInt >= inventory.Length || inv[resourceTypeInt] <= 0)
            return false;
        else
        {
            inv[resourceTypeInt]--;
            return true;
        }
    }
}
