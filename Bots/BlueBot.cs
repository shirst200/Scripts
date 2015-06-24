using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BlueBot : MonoBehaviour {

	//Array of all information from the tablet
	private bool[] infoArray;
	//Current and Target position for robot
	private Vector3 currentPos;
	private Vector3 targetPos;
	//Current and Target position for arm and cart
	private Vector3 armPos;
	private Vector3 targetArmPos;
	private Vector3 newCartPos;
	//The input values
	private int inputX = 0;
	private int inputY = 0;
	private bool pushing;
	//Current int to check
	private int checkInt = 0;
	//Is the robot on the ground?
	private bool grounded = false;
	//Has the game started?
	private bool started = false;
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
	//When the cart begins it's move
	private float cartTime;
	//The distance needed to be traveled
	private float journeyLength;
	private float cartJourney;
	//When the bots arm has reached it's push point
	private bool armReached;
	//The position of the temporary cart
	private Vector3 tempCartPos;
	//The cart gameobject when it's moving
	private GameObject temptCart;

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
		body = managerScript.CreateTile(1, transform.position + new Vector3(0f,0f,-0.4f));
		body.transform.parent = transform;
		//Create the arm tile
		band = managerScript.CreateTile(13, transform.position + new Vector3(0f,0f,-0.5f));
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
		//Start the simulation
		started = true;
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
		//Check if a car has been spawned to move
		if(temptCart!=null){
			//Move the cart to where it was pushed to
			float distCovered = (Time.time - cartTime) * 1.5f;
			float fracJourney = distCovered / cartJourney;
			temptCart.transform.position = Vector3.Lerp(tempCartPos, newCartPos, fracJourney);
		}
		//If grounded move normally
		if(grounded){
			//Move towards target if not pushing this second
			if(!pushing){
				float distCovered = (Time.time - startTime) * 1.5f;
				float fracJourney = distCovered / journeyLength;
				transform.position = Vector3.Lerp(currentPos, targetPos, fracJourney);
			}
			//The bot is pushing therefore only the arm moves
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
		if(targetArmPos!=armPos && tempCartPos!=newCartPos){
			targetArmPos = armPos;
			//Check if there is a temp cart
			if(newCartPos!= Vector3.zero && newCartPos!= null){
				//If the cart is not floating, spawn cart block and destroy tile
				if(checkCollisions(newCartPos + new Vector3(0,-1,0))){
					managerScript.currentLayer = 1;
					managerScript.PlaceAndSort(newCartPos, 2);
					Destroy(temptCart);
				}
				//As cart is floating, move it down over the next second
				else{
					tempCartPos = newCartPos;
					newCartPos += new Vector3(0,-1,0);
				}
			}
		}
		//Convert the inputs from the array to directions
		CheckInputs();
		//Don't let the bot move other than falling if they are not on the ground
		if(!grounded){
			targetPos += new Vector3(0,-1,0);
		}
		else{
			//Check if pushing as it stops movement
			if(pushing){
				//Push right
				if(inputX==1){
					//If there is a cart several things need to happen
					if(checkCart(currentPos + new Vector3(1,0,0))){
						//Arm is set to move towards target
					  	targetArmPos = currentPos + new Vector3(1,0,0);
						//Cart is set to move one further along
						newCartPos = targetArmPos + new Vector3(1,0,0);
						//Spawn air to replace old cart
						managerScript.currentLayer = 1;
						managerScript.PlaceAndSort(targetArmPos, 0);
						//Create a cart tile for animation
						temptCart = managerScript.CreateTile(2, targetArmPos);
						tempCartPos = targetArmPos;
					}
					//Pushing without a cart therefore stop
					else
						pushing = false;
				}
				else{
					//Push left
					if(inputX==-1){
						//If there is a cart several things need to happen
						if(checkCart(currentPos + new Vector3(-1,0,0))){
							//Arm is set to move towards target
							targetArmPos = currentPos + new Vector3(-1,0,0);
							//Cart is set to move one further along
							newCartPos = targetArmPos + new Vector3(-1,0,0);
							//Spawn air to replace old cart
							managerScript.currentLayer = 1;
							managerScript.PlaceAndSort(targetArmPos, 0);
							//Create a cart tile for animation
							temptCart = managerScript.CreateTile(2, targetArmPos);
							tempCartPos = targetArmPos;
						}
						//Pushing without a cart therefore stop
						else
							pushing = false;
					}
					//Pushing without direction therefore stop
					else
						pushing = false;
				}
			}
			//Not pushing so try moving
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
		cartTime = Time.time;
		//If not pushing the distance is between bot and bots target
		if(!pushing)
			journeyLength = Vector3.Distance(currentPos, targetPos);
		//If pushing distance is between arm and arm target
		else
			journeyLength = Vector3.Distance(armPos, targetArmPos);
		//If there is a cart set its distance between old positon and new
		if(temptCart!=null)
			cartJourney = Vector3.Distance(tempCartPos, newCartPos);
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

	//Returns true if cart is found in pushing position
	bool checkCart(Vector3 pos){
		//Converts Vector3 position to array position
		int width = managerScript.width;
		int height = managerScript.height;
		int layer = 1;
		int arrayPos = (int)(pos.y)*width+(int)pos.x;
		//2 is the block number for a cart, if found return true
		if(managerScript.chunkSave[arrayPos+(layer*boxPerLayer)] == 2){
			return true;
		}
		//Return false if no carts are found
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
		pushing = false;
		//Convert the booleans to directions
		if(infoArray[checkInt+600*(1-1)]==true)
			inputX-=1;
		if(infoArray[checkInt+600*(2-1)]==true)
			inputX+=1;
		if(infoArray[checkInt+600*(3-1)]==true)
			inputY+=1;
		if(infoArray[checkInt+600*(4-1)]==true)
			pushing = true;
		//Update the second
		checkInt++;
	}
}