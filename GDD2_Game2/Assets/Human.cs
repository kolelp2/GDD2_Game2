using UnityEngine;
using System.Collections;

public class Human : MonoBehaviour {
    Mover myMover;
    GOTracker myGOT;
    // Use this for initialization
    void Start () {
        myMover = (Mover)GetComponent(typeof(Mover));
        GameObject map = GameObject.Find("Map");
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));
    }
	
	// Update is called once per frame
	void Update () {
        myMover.SetVelocity(new Vector2(0, 1), .7f);
        if (Time.frameCount % 3 == 0)
            myGOT.Report(this, typeof(Human));
	}

    public void Tag()
    {
        //change color
        SpriteRenderer sr = (SpriteRenderer)gameObject.GetComponent(typeof(SpriteRenderer));
        sr.color = new Color(189.0f / 255.0f, 189.0f / 255.0f, 189.0f / 255.0f);
        //report death to GO tracker
        myGOT.Report(this, typeof(Human));
        myGOT.ReportDeath<Human>(this);
        //add zombie script
        gameObject.AddComponent(typeof(Zombie));
        //destroy this script
        Destroy(this);
    }
}
