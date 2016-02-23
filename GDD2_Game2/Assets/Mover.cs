using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour {
    public float moveSpeed = 0.2f;
    Vector2 velocity = new Vector2(0, 0);
    public Vector2 Velocity
    {
        get
        {
            return velocity;
        }
        
        set
        {
            velocity = value;
        }
    }
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.position += (new Vector3(velocity.x, velocity.y)) * moveSpeed;
	}
}
