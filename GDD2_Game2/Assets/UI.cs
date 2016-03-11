using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UI : MonoBehaviour {
    Text popText;
    GOTracker got;
	// Use this for initialization
	void Start () {
        popText = (Text)GameObject.Find("PopulationText").GetComponent(typeof(Text));
        got = (GOTracker)GameObject.Find("Map").GetComponent(typeof(GOTracker));
	}
	
	// Update is called once per frame
	void Update () {
        if (Time.frameCount % 120 == 0)
        {
            int humans = got.GetUnitCount(ObjectType.Human);
            int zombies = got.GetUnitCount(ObjectType.Zombie);
            popText.text = "Humans: " + humans + " Zombies: " + zombies;
        }
	}
}
