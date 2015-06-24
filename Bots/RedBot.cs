using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class RedBot : MonoBehaviour {
	
	//Array of all information from the tablet
	private bool[] infoArray;
	//Current and Target position for robot
	private Vector3 currentPos;
	private Vector3 targetPos;
	//The position half way through the leap
	private Vector3 midLeapPos;
	//The input values
	private int inputX = 0;
	private int inputY = 0;
	private bool jumping;
	//Current int to check
	private int checkInt = 0;
	//Is the robot on the ground?
	private bool grounded = false;
	private GameObject body;
	//The game manager object and it's script
	private GameObject gameManager;
	private GameManager managerScript;
	//Number of layers used in the map
	private int numberOfLayers;
	//How many boxes per layer, width*height
	private int boxPerLayer;
	//When the bot begins it's move
	private float startTime;
	//The distance needed to be traveled
	private float journeyLength;
	//When the robot has made it mid leap
	private bool midReached;

	//Function is called when the object becomes active
	void Awake(){
		//Find the game manager and get it's scrupt
		gameManager = GameObject.FindGameObjectWithTag("MainCamera");
		managerScript = gameManager.GetComponent<GameManager>();
		//Get the number of layers
		numberOfLayers = managerScript.layers.Count;
		//Work out how many boxes in each layer
		boxPerLayer = managerScript.width*managerScript.height;
		//Create the body tile
		body = managerScript.CreateTile(7, transform.position + new Vector3(0f,0f,-0.4f));
		body.transform.parent = transform;
	}

	//Used to recieve the information from the tablets
	public void ReciveInfo(bool[] info){
		//Save the information from the tablet
		infoArray = info;
		//Set up the function to run once per second
		InvokeRepeating("Go", 1, 1);
		//Bot starts grounded
		grounded = true;
		//Set up the position variables
		targetPos = transform.position;
		currentPos = transform.position;
		//Set the current layer to 0
		managerScript.currentLayer = 0;
		//Add an air block at bot position
		managerScript.PlaceAndSort(targetPos, 0);
	}

	//Moves the robot over the second gap
	void Update(){
		//If grounded the bot can move in severl ways
		if(grounded){
			//If the bot is not leaping, the bot will simply move it's to target position
			if(!jumping){
				float distCovered = (Time.time - startTime) * 1.5f;
				float fracJourney = distCovered / journeyLength;
				transform.position = Vector3.Lerp(currentPos, targetPos, fracJourney);
			}
			//If bot is leaping it must transitiong to a midpoint before going to final point
			else{
				//When the bot reaches it's mid point it starts moving to the end point
				if(transform.position == midLeapPos && !midReached){
					midReached = true;
					startTime = Time.time;
				}
				float distCovered = (Time.time - startTime) * 3f;
				float fracJourney = distCovered / journeyLength;
				//Move the bot to the current target depending on if mid has been reached
				if(!midReached)
					transform.position = Vector3.Lerp(currentPos, midLeapPos, fracJourney);
				else{
					transform.position = Vector3.Lerp(midLeapPos, targetPos, fracJourney);
				}
			}
		}
		//If not grounded the bot simply moves down 1 space
		else{
			float distCovered = (Time.time - startTime) * 1.5f;
			float fracJourney = distCovered / journeyLength;
			transform.position = Vector3.Lerp(currentPos, targetPos, fracJourney);
		}
	}

	//Called each second to determine next move
	void Go(){
		//Reset midReached variable at start of new move
		midReached = false;
		//Check if robot is grounded
		grounded = checkCollisions(transform.position + new Vector3(0,-1,0));
		//Set the new current position
		currentPos = targetPos;
		//Check if the bot has hit a spike or end point, if so set inactive
		if(checkSpikeStar(currentPos))
			gameObject.SetActive(false);
		//Convert the inputs from the array to directions
		CheckInputs();
		//Don't let the bot move other than falling if they are not on the ground
		if(!grounded){
			targetPos += new Vector3(0,-1,0);
		}
		else{
			//Check if jumping as it stops movement
			if(jumping){
				//Going right while leaping
				if(inputX==1){
					//Check if there is a block in the way for the leap
					if(!checkCollisions(currentPos + new Vector3(0,1,0)) &&
						!checkCollisions(currentPos + new Vector3(1,1,0)) &&
					  		!checkCollisions(currentPos + new Vector3(2,1,0)) && 
					  			!checkCollisions(currentPos + new Vector3(2,0,0))){
						//With nothing in the way, set the mid point and final target pos
						midLeapPos = currentPos + new Vector3(1,1,0);
						targetPos = currentPos + new Vector3(2,0,0);
					}
					//As there is a block in the way cancel the leap
					else
						jumping = false;
				}
				//Going left while leaping
				else{
					//Check if there is a block in the way for the leap
					if(!checkCollisions(currentPos + new Vector3(0,1,0)) &&
					   	!checkCollisions(currentPos + new Vector3(-1,1,0)) &&
					   		!checkCollisions(currentPos + new Vector3(-2,1,0)) && 
					   			!checkCollisions(currentPos + new Vector3(-2,0,0))){
						//With nothing in the way, set the mid point and final target pos
						midLeapPos = currentPos + new Vector3(-1,1,0);
						targetPos = currentPos + new Vector3(-2,0,0);
					}
					//As there is a block in the way cancel the leap
					else
						jumping = false;
				}
			}
			//Not jumping so try moving
			else{
				//Check if bot is mini jumping
				if(inputY==1){
					//Add up to the target pos
					targetPos += new Vector3(0,1,0);
					//Check for block above before setting target
					if(!checkCollisions(targetPos)){
						//Add right to the target pos
						if(inputX==1){
							targetPos += new Vector3(1,0,0);
						}
						//Add Left to the target pos
						if(inputX==-1){
							targetPos +=new Vector3(-1,0,0);
						}
					}
					//If the final target has is collidable, reset target pos
					if(checkCollisions(targetPos))
						targetPos = currentPos;
				}
				//If not jumping, check for x direction
				else{
					//Move right if no collisions
					if(inputX==1){
						if(!checkCollisions(targetPos+new Vector3(1,0,0)))
							targetPos += new Vector3(1,0,0);
					}
					//Move left if no collisions
					if(inputX==-1){
						if(!checkCollisions(targetPos+new Vector3(-1,0,0)))
							targetPos += new Vector3(-1,0,0);
					}
				}
			}
		}
		//Update the position of the bot
		startTime = Time.time;
		if(!jumping)
			journeyLength = Vector3.Distance(currentPos, targetPos);
		else
			journeyLength = Vector3.Distance(currentPos, midLeapPos);
	}
	
	//Returns true if collision is found at position on layers
	bool checkCollisions(Vector3 pos){
		//Variables used to convert Vector3 position to array position
		int width = managerScript.width;
		int height = managerScript.height;
		int arrayPos = (int)(pos.y)*width+(int)pos.x;
		//Go through all layers and check collisions in that spot
		for(int i = 0; i < numberOfLayers; i++){
			if(managerScript.blocks[managerScript.
			                        chunkSave[arrayPos+(i*boxPerLayer)]].collidable){
				return true;
			}
		}
		//Return false if no collisions are found
		return false;
	}
	
	//Returns true if bot enters spikes or star
	bool checkSpikeStar(Vector3 pos){
		//Variables used to convert Vector3 position to array position
		int width = managerScript.width;
		int height = managerScript.height;
		int layer = 1;
		int arrayPos = (int)(pos.y)*width+(int)pos.x;
		//If the position the bot entered is a spike or exit, return true
		if(managerScript.chunkSave[arrayPos+(layer*boxPerLayer)] == 11 
		   || managerScript.chunkSave[arrayPos+(layer*boxPerLayer)] == 4){
			return true;
		}
		//Return false if not entering these blocks
		return false;
	}
	
	void CheckInputs(){
		//Reset the inputs
		inputX = 0;
		inputY = 0;
		jumping = false;
		//Convert the booleans to directions
		if(infoArray[checkInt+600*(5-1)]==true)
			inputX-=1;
		if(infoArray[checkInt+600*(6-1)]==true)
			inputX+=1;
		if(infoArray[checkInt+600*(7-1)]==true)
			inputY+=1;
		if(infoArray[checkInt+600*(8-1)]==true)
			jumping = true;
		//Update the second
		checkInt++;
	}
}