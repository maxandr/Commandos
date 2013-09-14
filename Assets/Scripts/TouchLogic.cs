/*/
 * Script by Devin Curry
 * www.Devination.com
 * www.youtube.com/user/curryboy001
 * Please like and subscribe if you found my tutorials helpful :D
/*/
using UnityEngine;
using System.Collections;
 
public class TouchLogic : MonoBehaviour
{
	public static int currTouch = 0;//so other scripts can know what touch is currently on screen
	private Ray ray;//this will be the ray that we cast from our touch into the scene
	private RaycastHit rayHitInfo = new RaycastHit ();//return the info of the object that was hit by the ray
 
	void Update ()
	{
		//is there a touch on screen?
		if (Input.touches.Length <= 0) {
			//if no touches then execute this code
		} else { //if there is a touch
			//loop through all the the touches on screen
			for (int i = 0; i < Input.touchCount; i++) {
				currTouch = i;
				//executes this code for current touch (i) on screen
				if (this.guiTexture != null && (this.guiTexture.HitTest (Input.GetTouch (i).position))) {
					//if current touch hits our guitexture, run this code
					if (Input.GetTouch (i).phase == TouchPhase.Began) {
						//need to send message b/c function is not present in this script
						this.SendMessage ("OnTouchBegan");
						//OnTouchBegan(); //cant do this b/c the function is not defined in this script
					}
					if (Input.GetTouch (i).phase == TouchPhase.Ended) {
						this.SendMessage ("OnTouchEnded");
					}
					if (Input.GetTouch (i).phase == TouchPhase.Moved) {
						this.SendMessage ("OnTouchMoved");
					}
				}
    
				//outside so it doesn't require the touch to be over the guitexture
				if (Input.GetTouch (i).phase == TouchPhase.Began) {
					this.SendMessage ("OnTouchBeganAnywhere");
				}
				if (Input.GetTouch (i).phase == TouchPhase.Ended) {
					this.SendMessage ("OnTouchEndedAnywhere");
				}
				if (Input.GetTouch (i).phase == TouchPhase.Moved) {
					this.SendMessage ("OnTouchMovedAnywhere");
				}
				if (Input.GetTouch (i).phase == TouchPhase.Stationary) {
					this.SendMessage ("OnTouchStayedAnywhere");
				}
				/*
    //this is for 3d objects with colliders
    if(Input.GetTouch(i).phase == TouchPhase.Began)
    {
     ray = Camera.mainCamera.ScreenPointToRay(Input.GetTouch(i).position);//creates ray from screen point position
     if(Physics.Raycast(ray, out rayHitInfo))
     {
      rayHitInfo.transform.gameObject.SendMessage("OnTouchBegan3D");
     }
    }
    */
			}
		}
	}
}