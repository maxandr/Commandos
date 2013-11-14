using UnityEngine;
using System.Collections;
//Note this line, if it is left out, the script won't know that the class 'Path' exists and it will throw compiler errors
//This line should always be present at the top of scripts which use pathfinding
using Pathfinding;
public class AstarAI : MonoBehaviour {
    //The point to move to
    public Vector3 targetPosition;
    
    private Seeker seeker;
    private CharacterController controller;
 
    //The calculated path
    public Path path;
    
    //The AI's speed per second
    public float speed = 100;
    
    //The max distance from the AI to a waypoint for it to continue to the next waypoint
    public float nextWaypointDistance = 3;
 
    //The waypoint we are currently moving towards
    private int currentWaypoint = 0;
  	//Seeker seeker;// = GetComponent<Seeker>();
    public void Start () {
        seeker = GetComponent<Seeker>();
        controller = GetComponent<CharacterController>();
        
        //Start a new path to the targetPosition, return the result to the OnPathComplete function
        seeker.StartPath (transform.position,targetPosition, OnPathComplete);
    }
    
    public void OnPathComplete (Path p) {
        Debug.Log ("Yey, we got a path back. Did it have an error? "+p.error);
        if (!p.error) {
            path = p;
            //Reset the waypoint counter
            currentWaypoint = 0;
        }
    }
 
    public void FixedUpdate () {
		if (Input.GetMouseButtonUp (0)) {
			//if(Input.touches.Length==1)
			RaycastHit hit;
			Ray ray = UnityEngine.Camera.mainCamera.ScreenPointToRay (Input.mousePosition);
			if (Physics.Raycast (ray, out hit, 10000.0f)) {
       			 seeker.StartPath (transform.position,hit.point, OnPathComplete);
				//target = hit.point;
			}
		}
		
		
        if (path == null) {
            //We have no path to move after yet
            return;
        }
        
        if (currentWaypoint >= path.vectorPath.Count) {
            Debug.Log ("End Of Path Reached");
            return;
        }
        
        //Direction to the next waypoint
        Vector3 dir = (path.vectorPath[currentWaypoint]-transform.position).normalized;
        dir *= speed * Time.fixedDeltaTime;
        controller.SimpleMove (dir);
        
        //Check if we are close enough to the next waypoint
        //If we are, proceed to follow the next waypoint
        if (Vector3.Distance (transform.position,path.vectorPath[currentWaypoint]) < nextWaypointDistance) {
            currentWaypoint++;
            return;
        }
    }
} 