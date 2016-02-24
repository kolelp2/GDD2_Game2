using UnityEngine;
using System.Collections;

public class Human : MonoBehaviour {
    Mover myMover;
    GOTracker myGOT;
    // Use this for initialization
    void Start () {
        myMover = GetComponent<Mover>();
        GameObject GM = GameObject.Find("GameManager");
        myGOT = GM.GetComponent<GOTracker>();
    }
	
	// Update is called once per frame
	void Update () {
        myMover.SetVelocity(new Vector2(0, 1), .7f);
        myGOT.Report(gameObject);
	}

    public void Tag()
    {
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        sr.color = new Color(189.0f / 255.0f, 189.0f / 255.0f, 189.0f / 255.0f);
        gameObject.AddComponent<Zombie>();
        Destroy(this);
    }
}
