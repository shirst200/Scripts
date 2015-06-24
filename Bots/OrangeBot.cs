using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class OrangeBot : MonoBehaviour {
	
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
	private bool gripped;
	private int grab;
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
	//When the bots arm has reached it's clamp point
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
		body = managerScript.CreateTile(10, transform.position + new Vector3(0f,0f,-0.4f));
		body.transform.parent = transform;
		//Create the arm tile
		band = managerScript.CreateTile(15, transform.position + new Vector3(0f,0f,-0.5f));
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
		//Always move the bot to it's target position
		float distCovered = (Time.time - startTime) * 1.5f;
		float fracJourney = distCovered / journeyLength;
		transform.position = Vector3.Lerp(currentPos, targetPos, fracJourney);
		//If the bot is grabbing a flag
		if(gripped){
			//When the arm reaches the target set armReached to true
			if(band.transform.position == targetArmPos && !armReached){
				armReached = true;
			}
			//Move the arm to the flag
			float adistCovered = (Time.time - startTime) * 5f;
			float afracJourney = adistCovered / 4f;
			band.transform.position = Vector3.Lerp(armPos, targetArmPos, afracJourney);
			//Set the arm position to being exactly right when arm has reached
			if(armReached)
				band.transform.position = targetArmPos;
		}
		//If trying to let go of flag reset the arm
		if(!gripped && grab==-1){
			float adistCovered = (Time.time - startTime) * 5f;
			float afracJourney = adistCovered / 4f;
			band.transform.position = Vector3.Lerp(targetArmPos, armPos, afracJourney);
		}
	}

	//Called each second to determine next move
	void Go(){
		//If not gripped, reset the arm
		if(!gripped)
			armReached = false;
		//Check if robot is grounded
		grounded = checkCollisions(transform.position + new Vector3(0,-1,0));
		//Set the current new position
		currentPos = targetPos;
		//Check if the bot has hit a spike or end point, if so set inactive
		if(checkSpikeStar(currentPos))
			gameObject.SetActive(false);
		//Fix arm position
		armPos = currentPos + new Vector3(0,0,-0.5f);
		//When not gripped, reset targetArmPos
		if(!gripped)
			targetArmPos = armPos;
		//Convert the inputs from the array to directions
		CheckInputs();
		//When the bot has a flag gripped it's movement works differently, ignoring gravity
		if(gripped){
			//When grab is -1 the bot has to let go
			if(grab==-1){
				targetArmPos = currentPos;
				gripped = false;
			}
			//When not grabbed, check fore movement
			else{
				//Move right if there are no collisions
				if(inputX==1){
					if(!checkCollisions(targetPos+new Vector3(1,0,0)))
						targetPos += new Vector3(1,0,0);
				}
				//Move left if there are no collisions
				if(inputX==-1){
					if(!checkCollisions(targetPos+new Vector3(-1,0,0)))
						targetPos += new Vector3(-1,0,0);
				}
			}
		}
		//Not grabbing a flag it moves the same as a normal bot
		else{
			//Don't let the bot move other than falling if they are not on the ground
			if(!grounded){
				targetPos += new Vector3(0,-1,0);
			}
			//Not in the air so move normally
			else{
				//Check if the bot is trying to grab this second
				if(grab==1){
					//Grab right
					if(inputX==1){
						//If a flag is found in the grip position, grab it
						if(checkFlag(currentPos + new Vector3(4,0,0))){
							targetArmPos = currentPos + new Vector3(4,0,0);
							gripped = true;
						}
						//No flag so set gripped to false
						else
							gripped = false;
					}
					//Grab left
					else{
						//If a flag is found in the grip position, grab it
						if(inputX==-1){
							if(checkFlag(currentPos + new Vector3(-4,0,0))){
								targetArmPos = currentPos + new Vector3(-4,0,0);
								gripped = true;
							}
							else
								gripped = false;
						}
						//No flag so set gripped to false
						else
							gripped = false;
					}
				}
				else{
					//Check if bot is jumping
					if(inputY==1){
						//Check for block above
						targetPos += new Vector3(0,1,0);
						if(!checkCollisions(targetPos)){
							//Add right to target position
							if(inputX==1){
								targetPos += new Vector3(1,0,0);
							}
							//Add left to target position
							if(inputX==-1){
								targetPos +=new Vector3(-1,0,0);
							}
						}
						//If there is a block in the target position, reset target
						if(checkCollisions(targetPos))
							targetPos = currentPos;
					}
					//If not jumping, check for x direction
					else{
						//Go right if there is no block in the way
						if(inputX==1){
							if(!checkCollisions(targetPos+new Vector3(1,0,0)))
								targetPos += new Vector3(1,0,0);
						}
						//Go left if there is no block in the way
						if(inputX==-1){
							if(!checkCollisions(targetPos+new Vector3(-1,0,0)))
								targetPos += new Vector3(-1,0,0);
						}
					}
				}
			}
		}
		//Update the position of the bot
		startTime = Time.time;
		journeyLength = Vector3.Distance(currentPos, targetPos);
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
	
	//Returns true if flag is found
	bool checkFlag(Vector3 pos){
		//Converts Vector3 position to array position
		int width = managerScript.width;
		int height = managerScript.height;
		int layer = 1;
		int arrayPos = (int)(pos.y)*width+(int)pos.x;
		//6 is the block number for a flag, if found return true
		if(managerScript.chunkSave[arrayPos+(layer*boxPerLayer)] == 6){
			return true;
		}
		//Return false if no flags are found
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
		grab = 0;
		//Convert the booleans to directions
		if(infoArray[checkInt+600*(9-1)]==true)
			inputX-=1;
		if(infoArray[checkInt+600*(10-1)]==true)
			inputX+=1;
		if(infoArray[checkInt+600*(11-1)]==true)
			inputY+=1;
		if(infoArray[checkInt+600*(12-1)]==true)
			grab += 1;
		if(infoArray[checkInt+600*(13-1)]==true)
			grab -= 1;
		//Update the second
		checkInt++;
	}
}