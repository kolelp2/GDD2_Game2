using UnityEngine;
using System.Collections;
using System;

public class Stats : MonoBehaviour {
    [SerializeField]
    float rawStatMax = 5;
    [SerializeField]
    float rawStatMin = 2;
    float[] stats = new float[Enum.GetValues(typeof(StatTypes)).Length];

	// Use this for initialization
	void Start () {
	    for(int c = 0; c < stats.Length; c++)
        {
            stats[c] = UnityEngine.Random.Range(rawStatMin, rawStatMax);
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void ChangeStat(StatTypes stat, float amount)
    {
        stats[(int)stat] += amount;
    }
    public float GetStat(StatTypes stat)
    {
        return stats[(int)stat];
    }
}

public enum StatTypes { RawStrength, InheritedStrength, Skill, Stamina, Coordination, Knowledge }
