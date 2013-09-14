/*/
 * Script by Devin Curry
 * www.Devination.com
 * www.youtube.com/user/curryboy001
 * Please like and subscribe if you found my tutorials helpful :D
/*/
using UnityEngine;
using System.Collections;
 //test
public class PinchZoom : TouchLogic
{
	public float zoomSpeed = 5.0f;
	public float MIN_ZOOM = 10.0f;
	public float MAX_ZOOM = 50.0f;
	//buckets for caching our touch positions
	private Vector2 currTouch1 = Vector2.zero;
	private Vector2 lastTouch1 = Vector2.zero;
	private Vector2 currTouch2 = Vector2.zero;
	private Vector2  lastTouch2 = Vector2.zero;
	//used for holding our distances and calculating our zoomFactor
	private float currDist = 0.0f;
	private float lastDist = 0.0f;
	private float zoomFactor = 0.0f;
	Vector3 hit_position = Vector3.zero;
	Vector3 current_position = Vector3.zero;
	Vector3 camera_position = Vector3.zero;
	private float current_camera_y_position;

	void OnTouchMovedAnywhere ()
	{
		if (Input.touchCount == 2) {
			Zoom ();
		} else if (Input.touchCount == 1) {
			current_position = Input.GetTouch (0).position;
			current_camera_y_position = Camera.mainCamera.transform.position.y;
			MoveTheCamera ();
		}
	}

	void OnTouchStayedAnywhere ()
	{
		if (Input.touchCount == 2) {
			Zoom ();
		}
	}

	void OnTouchBeganAnywhere ()
	{
		if (Input.touchCount == 1) {
			hit_position = Input.GetTouch (0).position;
			camera_position = Camera.mainCamera.transform.position;
			current_camera_y_position = camera_position.y;		
		}
	}

	void OnTouchEndedAnywhere ()
	{
		if (Input.touchCount == 2) {
			current_position = Input.GetTouch (Mathf.Abs (currTouch - 1)).position;
			camera_position = Camera.mainCamera.transform.position;
			hit_position = Input.GetTouch (Mathf.Abs (currTouch - 1)).position;
		}
	}
	
	void MoveTheCamera ()
	{
		// From the Unity3D docs: "The z position is in world units from the camera."  In my case I'm using the y-axis as height
		// with my camera facing back down the y-axis.  You can ignore this when the camera is orthograhic.
		current_position.z = hit_position.z = camera_position.y;

		// Get direction of movement.  (Note: Don't normalize, the magnitude of change is going to be Vector3.Distance(current_position-hit_position)
		// anyways.  
		Vector3 direction = Camera.mainCamera.ScreenToWorldPoint (current_position) - Camera.mainCamera.ScreenToWorldPoint (hit_position);

		// Invert direction to that terrain appears to move with the mouse.
		direction = direction * -1;
		direction.y = 0.0f;
		Vector3 position = camera_position + direction;
		position.y = current_camera_y_position;
		Camera.mainCamera.transform.position = position;
	}
	//find distance between the 2 touches 1 frame before & current frame
	//if the delta distance increased, zoom in, if delta distance decreased, zoom out
	void Zoom ()
	{
		
		Vector3 CameraOldPosition = Camera.mainCamera.transform.position;
		//finds the distance between your moved touches
		//we dont want to find the distance between 1 finger and nothing
		//Caches touch positions for each finger
		switch (TouchLogic.currTouch) {
		case 0://first touch
			currTouch1 = Input.GetTouch (0).position;
			lastTouch1 = currTouch1 - Input.GetTouch (0).deltaPosition;
			break;
		case 1://second touch
			currTouch2 = Input.GetTouch (1).position;
			lastTouch2 = currTouch2 - Input.GetTouch (1).deltaPosition;
			break;
		}
		if (TouchLogic.currTouch >= 1) {
			currDist = Vector2.Distance (currTouch1, currTouch2);
			lastDist = Vector2.Distance (lastTouch1, lastTouch2);
		} else {
			currDist = 0.0f;
			lastDist = 0.0f;
		}
		//Calculate the zoom magnitude
		zoomFactor = Mathf.Clamp (lastDist - currDist, -30.0f, 30.0f);
		//apply zoom to our camera
		Camera.mainCamera.transform.Translate (Vector3.back * zoomFactor * zoomSpeed * Time.deltaTime);
		Vector3 _temp = Camera.mainCamera.transform.position;
		if(_temp.y<MIN_ZOOM) {
			CameraOldPosition.y = MIN_ZOOM;
			Camera.mainCamera.transform.position = CameraOldPosition;
		}
		else if(_temp.y>MAX_ZOOM) {
			CameraOldPosition.y = MAX_ZOOM;
			Camera.mainCamera.transform.position = CameraOldPosition;
		}
		else {
		//float _y = Mathf.Clamp (_temp.y, MIN_ZOOM, MAX_ZOOM);
		//_temp.y = _y;
		Camera.mainCamera.transform.position = _temp;
		}
	}
}
