using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowTarget : MonoBehaviour {

	public GameObject target;

    public bool holdStand = false;

	NavMeshAgent agent;

	// Use this for initialization
	void Start () {
		agent = GetComponent<NavMeshAgent> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        if (target != null && !holdStand) {
            agent.destination = target.transform.position;
        }		
	}
}
