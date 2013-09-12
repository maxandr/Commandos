using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour {
Vector3 hit_position = Vector3.zero;
Vector3 current_position = Vector3.zero;
Vector3 camera_position = Vector3.zero;
	
	//Zoom start
public int speed = 4;
public float MINSCALE = 2.0F;
public float MAXSCALE = 5.0F; 
public float minPinchSpeed = 5.0F;
public float varianceInDistances = 5.0F;
private float touchDelta = 0.0F; 
private Vector2 prevDist = new Vector2(0,0);
private Vector2 curDist = new Vector2(0,0);
private float speedTouch0 = 0.0F;
private float speedTouch1 = 0.0F;
	//Zoom end
// Use this for initialization
void Start () {
}

void Update(){
#if UNITY_EDITOR
    if(Input.GetMouseButtonDown(0)){
        hit_position = Input.mousePosition;
        camera_position = transform.position;

    }
    if(Input.GetMouseButton(0)){
        current_position = Input.mousePosition;
        LeftMouseDrag();    
    }
	
	
#else 
	if(Input.touchCount==1 && Input.GetTouch(0).phase==TouchPhase.Began) {
		hit_position = Input.mousePosition;
        camera_position = transform.position;
	}
	else if(Input.touchCount==1 && Input.GetTouch(0).phase==TouchPhase.Moved) {
		current_position = Input.mousePosition;
        LeftMouseDrag();    
	}
/*	else if (Input.touchCount == 2 && Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved) 
    {
 
        curDist = Input.GetTouch(0).position - Input.GetTouch(1).position; //current distance between finger touches
        prevDist = ((Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition) - (Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition)); //difference in previous locations using delta positions
        touchDelta = curDist.magnitude - prevDist.magnitude;
        speedTouch0 = Input.GetTouch(0).deltaPosition.magnitude / Input.GetTouch(0).deltaTime;
        speedTouch1 = Input.GetTouch(1).deltaPosition.magnitude / Input.GetTouch(1).deltaTime;
 
 
        if ((touchDelta + varianceInDistances <= 1) && (speedTouch0 > minPinchSpeed) && (speedTouch1 > minPinchSpeed))
        {
			Vector3 tTrans = camera.transform.position;
			tTrans.y = Mathf.Clamp(camera.transform.position.y + (1 * speed),10,50);
            camera.transform.position = tTrans;
        }
 
        if ((touchDelta +varianceInDistances > 1) && (speedTouch0 > minPinchSpeed) && (speedTouch1 > minPinchSpeed))
        {
			Vector3 tTrans = camera.transform.position;
			tTrans.y = Mathf.Clamp(camera.transform.position.y - (1 * speed),10,50);
            camera.transform.position = tTrans;
        }
 
    }	
    */
#endif
}

void LeftMouseDrag(){
    // From the Unity3D docs: "The z position is in world units from the camera."  In my case I'm using the y-axis as height
    // with my camera facing back down the y-axis.  You can ignore this when the camera is orthograhic.
    current_position.z = hit_position.z = camera_position.y;

    // Get direction of movement.  (Note: Don't normalize, the magnitude of change is going to be Vector3.Distance(current_position-hit_position)
    // anyways.  
    Vector3 direction = Camera.main.ScreenToWorldPoint(current_position) - Camera.main.ScreenToWorldPoint(hit_position);

    // Invert direction to that terrain appears to move with the mouse.
    direction = direction * -1;
	direction.y=0.0f;
    Vector3 position = camera_position + direction;

    transform.position = position;
}

}
