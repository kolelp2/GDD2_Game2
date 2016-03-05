using UnityEngine;
using System.Collections;

public class buttonScript : MonoBehaviour {

    public string startLevel = "Game";

    void Awake()
    {
        DontDestroyOnLoad(this);
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void StartButtonClick ()
    {
        Application.LoadLevel(startLevel);
    }

    public void ExitButtonClick ()
    {
        Application.Quit();
    }
}
