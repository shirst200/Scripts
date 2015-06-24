using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class GreenBot : MonoBehaviour {
	
	//Array of all information from the tablet
	private bool[] infoArray;
	//Current and Target position for robot
	private Vector3 currentPos;
	private Vector3 targetPos;
	//Current and Target position for arm
	private Vector3 armPos;
	private Vector3 targetArmPos;
	//The input values
	private int inputX = 0;
	private int inputY = 0;
	private bool drilling;
	//Current int to check
	private int checkInt = 0;
	//Is the robot on the ground?
	private bool grounded = false;
	//Body and arm gameobjects
	private GameObject body;
	private GameObject band;
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
	//When the bots arm has reached it's drill point
	private bool armReached;

	//Function is called when the object becomes active
	void Awake(){
		//Find the game manager and get it's script
		gameManager = GameObject.FindGameObjectWithTag("MainCamera");
		managerScript = gameManager.GetComponent<GameManager>();
		//Get the number of layers
		numberOfLayers = managerScript.layers.Count;
		//Work out how many boxes in each layer
		boxPerLayer = managerScript.width*managerScript.height;
		//Create the body tile
		body = managerScript.CreateTile(5, transform.position + new Vector3(0f,0f,-0.4f));
		body.transform.parent = transform;
		//Create the arm tile
		band = managerScript.CreateTile(14, transform.position + new Vector3(0f,0f,-0.5f));
		band.transform.parent = transform;
		//Set up the start position for the arm
		armPos = band.transform.position;
		//Set up the start target position for arm
		targetArmPos = armPos;
	}

	//Used to recieve the information from the tablets
	public void ReciveInfo(bool[] info){
		//Save the information from the tablet
		infoArray = info;
		//Set up the function to run once per second
		InvokeRepeating("Go", 1, 1);
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
		//If grounded move normally
		if(grounded){
			//Move towards target if not drilling this second
			if(!drilling){
				float distCovered = (Time.time - startTime) * 1.5f;
				float fracJourney = distCovered / journeyLength;
				transform.position = Vector3.Lerp(currentPos, targetPos, fracJourney);
			}
			//The bot is drilling therefore only the arm moves
			else{
				//When the arm reaches the target set armReached to true
				if(band.transform.position == targetArmPos && !armReached){
					armReached = true;
					startTime = Time.time;
				}
				float distCovered = (Time.time - startTime) * 2.4f;
				float fracJourney = distCovered / journeyLength;
				//Move towards target when arm has not reached it yet
				if(!armReached)
					band.transform.position = Vector3.Lerp(armPos, targetArmPos, fracJourney);
				//Move back to body if arm has reached target
				else{
					band.transform.position = Vector3.Lerp(targetArmPos, armPos, fracJourney);
				}
			}
		}
		//If not grounded fall down one space
		else{
			float distCovered = (Time.time - startTime) * 1.5f;
			float fracJourney = distCovered / journeyLength;
			transform.position = Vector3.Lerp(currentPos, targetPos, fracJourney);
		}
	}

	//Called each second to determine next move
	void Go(){
		//Reset the arm 
		armReached = false;
		//Check if robot is grounded
		grounded = checkCollisions(transform.position + new Vector3(0,-1,0));
		//Set the new current position
		currentPos = targetPos;
		//Check if the bot has hit a spike or end point, if so set inactive
		if(checkSpikeStar(currentPos))
			gameObject.SetActive(false);
		//Fix arm position
		armPos = currentPos + new Vector3(0,0,-0.5f);
		//Reset arm target
		targetArmPos = armPos;
		//Convert the inputs from the array to directions
		CheckInputs();
		//Don't let the bot move other than falling if they are not on the ground
		if(!grounded){
			targetPos += new Vector3(0,-1,0);
		}
		else{
			//Check if drilling as it stops movement
			if(drilling){
				//Drilling horizontally
				if(inputY==0){
					//Drilling right
					if(inputX==1){
						//If there is a block, break it and spawn air
						if(checkBreak(currentPos + new Vector3(1,0,0))){
							targetArmPos = currentPos + new Vector3(1,0,0);
							managerScript.currentLayer = 0;
							managerScript.PlaceAndSort(targetArmPos, 0);
						}
						//No block therefore stop drilling
						else
							drilling = false;
					}
					//Drilling left
					else{
						if(inputX==-1){
							//If there is a block, break it and spawn air
							if(checkBreak(currentPos + new Vector3(-1,0,0))){
								targetArmPos = currentPos + new Vector3(-1,0,0);
								managerScript.currentLayer = 0;
								managerScript.PlaceAndSort(targetArmPos, 0);
							}
							//No block therefore stop drilling
							else
								drilling = false;
						}
						//Drilling without direction, therefore stop
						else
							drilling = false;
					}
				}
				//Drilling vertically
				else{
					//Drilling up
					if(inputY==1){
						//If there is a block, break it and spawn air
						if(checkBreak(currentPos + new Vector3(0,1,0))){
							targetArmPos = currentPos + new Vector3(0,1,0);
							managerScript.currentLayer = 0;
							managerScript.PlaceAndSort(targetArmPos, 0);
						}
						//No block therefore stop drilling
						else
							drilling = false;
					}
					//Drilling down
					else{
						//If there is a block, break it and spawn air
						if(checkBreak(currentPos + new Vector3(0,-1,0))){
							targetArmPos = currentPos + new Vector3(0,-1,0);
							managerScript.currentLayer = 0;
							managerScript.PlaceAndSort(targetArmPos, 0);
						}
						//No block therefore stop drilling
						else
							drilling = false;
					}
				}
			}
			//Not drilling so try moving
			else{
				//Check if bot is jumping
				if(inputY==1){
					//Check for block above
					targetPos += new Vector3(0,1,0);
					if(!checkCollisions(targetPos)){
						//Add right to target
						if(inputX==1){
							targetPos += new Vector3(1,0,0);
						}
						//Add left to target
						if(inputX==-1){
							targetPos +=new Vector3(-1,0,0);
						}
					}
					//If target has collision, reset target
					if(checkCollisions(targetPos))
						targetPos = currentPos;
				}
				//If not jumping, check for x direction
				else{
					//Moving right
					if(inputX==1){
						//With no collision add right
						if(!checkCollisions(targetPos+new Vector3(1,0,0)))
							targetPos += new Vector3(1,0,0);
					}
					//Moving left
					if(inputX==-1){
						//With no collision add left
						if(!checkCollisions(targetPos+new Vector3(-1,0,0)))
							targetPos += new Vector3(-1,0,0);
					}
				}
			}
		}
		//Update the position of the bot
		startTime = Time.time;
		//If not drilling the distance is between bot and bots target
		if(!drilling)
			journeyLength = Vector3.Distance(currentPos, targetPos);
		//If drilling distance is between arm and arm target
		else
			journeyLength = Vector3.Distance(armPos, targetArmPos);
	}
	
	//Returns true if collision is found at position on layers
	bool checkCollisions(Vector3 pos){
		//Converts Vector3 position to array position
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
	
	//Returns true if a breakable block is found
	bool checkBreak(Vector3 pos){
		//Converts Vector3 position to array position
		int width = managerScript.width;
		int height = managerScript.height;
		int layer = 0;
		int arrayPos = (int)(pos.y)*width+(int)pos.x;
		//3 is the block number for a breakable block, if found return true
		if(managerScript.chunkSave[arrayPos+(layer*boxPerLayer)] == 3){
			return true;
		}
		//Return false if no breakable blocks are found
		return false;
	}
	
	//Returns true if bot enters spikes or star
	bool checkSpikeStar(Vector3 pos){
		//Converts Vector3 position to array position
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
		drilling = false;
		//Convert the booleans to directions
		if(infoArray[checkInt+600*(14-1)]==true)
			inputX-=1;
		if(infoArray[checkInt+600*(15-1)]==true)
			inputX+=1;
		if(infoArray[checkInt+600*(16-1)]==true)
			inputY+=1;
		if(infoArray[checkInt+600*(17-1)]==true)
			inputY-=1;
		if(infoArray[checkInt+600*(18-1)]==true)
			drilling = true;
		//Update the second
		checkInt++;
	}
}