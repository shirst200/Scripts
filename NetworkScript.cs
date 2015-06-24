using UnityEngine;
using System.Collections;

public class NetworkScript : MonoBehaviour {

	//new set of variables for controls
	private float buttonSize;
	private float gap;
	private float boarder;
	public GUISkin[] Skins;
	public Texture[] Background;
	public Texture[] ButtonBackground;

	//variables for bot selector
	private float selectorSize;
	private float selectorGap;

	//The game is running or not
	public bool running;	
	private float bttnW = 150;
	private float bttnH = 40;
	//Game manager object and it's script
	public GameObject manager;
	public GameManager gManager;
	//The camera colour for each bot
	public Color[] backgrounds;
	//The timer showing tablet users game time
	public float counter;
	//Refreshing server list
	private bool Refreshing = false;
	//Server information
	private HostData[] hostData;
	//Starting position in array for bot input values
	public int[] botStarts;
	//How many rows each bot uses
	public int[] botRows;
	//Name of the game
	private string gameName = "Code Monkeys";
	//Which map being played
	private int levelNum;
	public int currentBot;
	//The spawned bot game objects
	public GameObject[] bots = new GameObject[4];
	//The types of bot spawnable
	public GameObject[] botTypes;
	//The current window in the sequence
	public int windowNum;
	//All of the inputs
	public bool[][] Setting;
	//The number of collumns and rows per page
	public int noColumns;
	public int noRows;
	//Name of all the rows
	public Texture[] rowTextures;

	void Start(){
		//Calculates variables for control page
		//Determins the button size using screen variables
		if ( Screen.dpi == 0 ) { buttonSize = 100; }
		else { buttonSize = Screen.dpi / 3; }
		if ( Screen.height < (buttonSize + 5) * 10 )
		{	buttonSize = Screen.height / 10;	}
		//Gap size takes 20% from button size
		gap=0.2f*buttonSize;
		buttonSize=buttonSize-gap;
		//Calculates the number of buttons that will fit on the screen
		noColumns = Mathf.FloorToInt((Screen.width - 10) / (buttonSize + gap) - 1);
		boarder = -1*((buttonSize+gap)*(noColumns + 1) - Screen.width)+gap;
		//Changes text size
		Skins[0].label.fontSize=Mathf.RoundToInt(Screen.height*0.04f);
		Skins[0].button.fontSize=Mathf.RoundToInt(Screen.height*0.05f);

		//Calculates varibles for selection page
		selectorSize=Screen.height/5;
		selectorGap=selectorSize*0.1f;
		selectorSize-=selectorGap;

		//Set up the array of inputs
		Setting=new bool[4][];
		for (int i = 0; i<4; i++)
		Setting[i] = new bool[2700];
		//Get the GameManager script from the object
		gManager = manager.GetComponent<GameManager>();
	}
	
	void Update(){
		//Refresh the server list
		if (Refreshing){
			if (MasterServer.PollHostList().Length > 0){
				Debug.Log(MasterServer.PollHostList().Length.ToString());
				Refreshing = false;
				hostData = MasterServer.PollHostList();
			}
		}
		//Incremenet the counter when running game
		if(running)
			counter += Time.deltaTime;
	}

	//Starts the server
	void startServer(){
		//initialize the server with 2 possible connections
		Network.InitializeServer(2, 25001, !Network.HavePublicAddress());
		//Register the server with game name
		MasterServer.RegisterHost(gameName, "Code Monkey Server", "Code Monkey Server");
	}

	//Refresh host list
	void RefreshHostList(){
		MasterServer.RequestHostList(gameName);
		Refreshing = true;
	}

	//For when the server is created
	void OnServerInitialized(){
		Debug.Log("Server Initialized!");
	}

	//Master server events
	void OnMasterServerEvent(MasterServerEvent mse){
		//Print that server registered successfully
		if(mse == MasterServerEvent.RegistrationSucceeded){
			Debug.Log("Registered Server");
		}
	}

	//Give the bots their tasks
	[RPC]
	void setTasks(bool[] info){
		//All devices start running
		running = true;
		//Server needs to initalise the bots
		if(Network.isServer){
			//For each bot, instantiate at spawn point
			for(int i = 0; i < 4;i++){
				bots[i] = (GameObject) Instantiate(botTypes[i], 
				  				gManager.botsPos[i], Quaternion.identity);
			}
			//Send the inputs to each bot
			for(int i = 0; i < bots.Length; i++){
				bots[i].BroadcastMessage("ReciveInfo", info);
			}
		}
	}

	//Reset everything
	[RPC]
	void reset(){
		//Stop running
		running = false;
		//Reset counter
		counter = 0;
		//Destroy the bots
		for(int i = 0; i <4;i++){
			Destroy(bots[i]);
		}
		//Server reloads level to stop any changes
		if(Network.isServer){
			manager.SendMessage("Load", levelNum-1, SendMessageOptions.DontRequireReceiver);
			//Destroy bots
			for(int i = 0; i < 4;i++){
				Destroy(bots[i]);
			}
		}
	}

	//Used for creating the map
	[RPC]
	void setLevel(int info){
		levelNum = info;
		//Reset counter
		counter = 0;
		//Load the level
		if(Network.isServer){
			manager.SendMessage("Load", info-1, SendMessageOptions.DontRequireReceiver);

		}
	}

	//GUI used to render tablet interface and buttons to allow connection
	void OnGUI(){
		//When the game is already running
		if(running){
			//Only client has GUI interface when running
			if(Network.isClient){
				//Display the counter
				GUI.Label(new Rect(Screen.width/2-50, Screen.height/2-50, 100,100),(Mathf.FloorToInt(counter)).ToString());
				//Button to stop running the game
				if(GUI.Button(new Rect(Screen.width/2-Screen.width/10,0,Screen.width/5,Screen.height/(noRows+1)),"Stop")){
					//Reset the level
					networkView.RPC("reset", RPCMode.AllBuffered);
				}
			}
		}
		//When not running
		else{
			//Tablet interface for inputs
			if(Network.isClient){
				//Main menu
				if(levelNum==0){
					GUI.skin=Skins[0];

					GUI.DrawTexture(new Rect(0,Screen.height/5,Screen.width,Screen.height-(Screen.height/5)*2),Background[0]);
					GUI.DrawTexture(new Rect(0,Screen.height-Screen.height/5,Screen.width,Screen.height/5),Background[1]);

					GUI.DrawTexture(new Rect(boarder/2,gap/2,buttonSize,buttonSize),ButtonBackground[2]);
					GUI.DrawTexture(new Rect(boarder/2,gap/2,buttonSize,buttonSize),Background[6]);

					GUI.DrawTexture(new Rect(Screen.width/2-selectorSize-selectorGap/2,Screen.height/2-selectorSize-selectorGap/2,selectorSize,selectorSize),ButtonBackground[5]);
					if(GUI.Button(new Rect(Screen.width/2-selectorSize-selectorGap/2,Screen.height/2-selectorSize-selectorGap/2,selectorSize,selectorSize),""))
					{
						currentBot = 0;
					}
					GUI.DrawTexture(new Rect(Screen.width/2+selectorGap/2,Screen.height/2-selectorSize-selectorGap/2,selectorSize,selectorSize),ButtonBackground[6]);
					if(GUI.Button(new Rect(Screen.width/2+selectorGap/2,Screen.height/2-selectorSize-selectorGap/2,selectorSize,selectorSize),""))
					{
						currentBot = 1;
					}
					GUI.DrawTexture(new Rect(Screen.width/2-selectorSize-selectorGap/2,Screen.height/2+selectorGap/2,selectorSize,selectorSize),ButtonBackground[7]);
					if(GUI.Button(new Rect(Screen.width/2-selectorSize-selectorGap/2,Screen.height/2+selectorGap/2,selectorSize,selectorSize),""))
					{
						currentBot = 3;
					}
					GUI.DrawTexture(new Rect(Screen.width/2+selectorGap/2,Screen.height/2+selectorGap/2,selectorSize,selectorSize),ButtonBackground[8]);
					if(GUI.Button(new Rect(Screen.width/2+selectorGap/2,Screen.height/2+selectorGap/2,selectorSize,selectorSize),""))
					{
						currentBot = 2;
					}

					Skins[0].label.fontSize=Mathf.RoundToInt(Screen.height*0.075f);
					GUI.Label(new Rect(0,0,Screen.width,Screen.height/5),"Character Selection");
					Skins[0].label.fontSize=Mathf.RoundToInt(Screen.height*0.05f);
					GUI.Label(new Rect(0,Screen.height-Screen.height/10,Screen.width,Screen.height/10),"My Character");
					Skins[0].label.fontSize=Mathf.RoundToInt(Screen.height*0.04f);

					GUI.DrawTexture(new Rect(Screen.width-(selectorSize+selectorGap/2),Screen.height-(selectorSize+selectorGap/2),selectorSize,selectorSize),ButtonBackground[4]);
					if(GUI.Button(new Rect(Screen.width-(selectorSize+selectorGap/2),Screen.height-(selectorSize+selectorGap/2),selectorSize,selectorSize),"Confirm"))
						//Start the level and move to midi sequencer
						networkView.RPC("setLevel", RPCMode.AllBuffered, 1);
				}
				//Sequencer menu
				else{
					GUI.skin=Skins[0];
					GUI.DrawTexture(new Rect((gap+boarder)/2+buttonSize,0,Screen.width,buttonSize+gap),Background[0]);	
					GUI.DrawTexture(new Rect(0,0,(gap+boarder)/2+buttonSize,Screen.height-(2* buttonSize+2*gap)),Background[0]);
					GUI.DrawTexture(new Rect(0,Screen.height-(2* buttonSize+2*gap),Screen.width,2* buttonSize+2.5f*gap),Background[1]);
					//Number of rows to display depending on selected bot
					noRows = botRows[currentBot];
					//Set the background colour to the colour of the bot
					Camera.main.backgroundColor = backgrounds[currentBot];
					//Go back to main menu
					GUI.DrawTexture(new Rect(boarder/2,gap/2,buttonSize,buttonSize),ButtonBackground[2]);
					GUI.DrawTexture(new Rect(boarder/2,gap/2,buttonSize,buttonSize),Background[6]);
					if(GUI.Button(new Rect(boarder/2,gap/2,buttonSize,buttonSize),"")){
						levelNum = 0;
					}
					//The string for the start/stop button
					string startStop = "Start";
					//When running, button allows to stop running
					if(running)
						startStop = "Stop";
					//When not running, button allows to starrt running
					else
						startStop = "Ready";
					//Display the stop start button
					GUI.DrawTexture(new Rect(boarder/2, Screen.height-buttonSize*2-1.5f*gap, buttonSize*2+gap, buttonSize*2+gap),ButtonBackground[4]);
					if(GUI.Button(new Rect(boarder/2, Screen.height-buttonSize*2-1.5f*gap, buttonSize*2+gap, buttonSize*2+gap), startStop))
					{
						//Start the game and send inputs
						if(!running)
							networkView.RPC("setTasks", RPCMode.AllBuffered, Setting[currentBot],currentBot);
						//Stop the game and reset
						else
							networkView.RPC("reset", RPCMode.AllBuffered);
					}
					//Clears all inputs in array
					GUI.DrawTexture(new Rect((buttonSize+gap)*(noColumns -1)+boarder/2,Screen.height-buttonSize-0.5f*gap,buttonSize*2+gap,buttonSize),ButtonBackground[3]);
					if(GUI.Button(new Rect((buttonSize+gap)*(noColumns -1)+boarder/2,Screen.height-buttonSize-0.5f*gap,buttonSize*2+gap,buttonSize),"Clear")){
						for(int n = 0; n<4;n++){
							for(int i = 0; i < Setting[n].Length;i++)
						{Setting[n][i] = false;}
						}
					}
					GUI.DrawTexture(new Rect((buttonSize+gap)*noColumns+boarder/2,Screen.height-buttonSize*2-1.5f*gap,buttonSize,buttonSize),ButtonBackground[2]);
					GUI.DrawTexture(new Rect((buttonSize+gap)*noColumns+boarder/2,Screen.height-buttonSize*2-1.5f*gap,buttonSize,buttonSize),Background[5]);
					//Advances the current page
					if(GUI.Button(new Rect((buttonSize+gap)*noColumns+boarder/2,Screen.height-buttonSize*2-1.5f*gap,buttonSize,buttonSize),"")){
						if(windowNum<39)
							windowNum++;
					}
					GUI.DrawTexture(new Rect((buttonSize+gap)*(noColumns -1)+boarder/2,Screen.height-buttonSize*2-1.5f*gap,buttonSize,buttonSize),ButtonBackground[2]);
					GUI.DrawTexture(new Rect((buttonSize+gap)*(noColumns -1)+boarder/2,Screen.height-buttonSize*2-1.5f*gap,buttonSize,buttonSize),Background[4]);
					//Go back a page
					if(GUI.Button(new Rect((buttonSize+gap)*(noColumns -1)+boarder/2,Screen.height-buttonSize*2-1.5f*gap,buttonSize,buttonSize),"")){
						if(windowNum>0)
							windowNum--; 
					}
					GUI.skin=Skins[1];
					//Goes through each input button
					for (int n=0;n<noRows;n++){
						for(int i=0;i<noColumns;i++){
							//Starts the inputs at the right array position
							int botBuffer = botStarts[currentBot];
							Texture active;
							//The string for if the input is active or not
							if(Setting[currentBot][(windowNum*15)+i+n*600]==true){
								//input is true
								active=ButtonBackground[1];}
							else{
								//Input is not true
								active=ButtonBackground[0];}
							//Display the input options
							if(GUI.Button(new Rect((buttonSize+gap)*(i+1)+boarder/2,(buttonSize+gap)*(n+1)+0.5f*gap,buttonSize,buttonSize),""))
							{
								//True turns false when clicked and vice versa
								if(Setting[currentBot][(windowNum*15)+i+n*600]==true)
									Setting[currentBot][(windowNum*15)+i+n*600]=false;
								else
									Setting[currentBot][(windowNum*15)+i+n*600]=true;
							}
							GUI.DrawTexture(new Rect((buttonSize+gap)*(i+1)+boarder/2,(buttonSize+gap)*(n+1)+0.5f*gap,buttonSize,buttonSize),active);
						}
					}	
					GUI.skin=Skins[0];
					//Display the names of each row
					for(int n=0;n<noRows;n++){
						int botBuffer = botStarts[currentBot]/600;
						GUI.DrawTexture(new Rect(boarder/2,(buttonSize+gap)*(n+1)+gap/2,buttonSize,buttonSize),rowTextures[n]);
					}
					for(int i=0;i<noColumns;i++){
						GUI.Label(new Rect((buttonSize+gap)*(i+1)+boarder/2,gap/2,buttonSize,buttonSize),""+(windowNum*noColumns+1+i));
					}
					//Show each bot and allow the user to change between them
					GUI.DrawTexture(new Rect(Screen.width/2-buttonSize-gap/2,Screen.height-(buttonSize+gap)*2+0.5f*gap,buttonSize,buttonSize),ButtonBackground[5]);
					if(GUI.Button(new Rect(Screen.width/2-buttonSize-gap/2,Screen.height-(buttonSize+gap)*2+0.5f*gap,buttonSize,buttonSize),""))
					{
						currentBot = 0;
					}
					GUI.DrawTexture(new Rect(Screen.width/2+gap/2,Screen.height-(buttonSize+gap)*2+0.5f*gap,buttonSize,buttonSize),ButtonBackground[6]);
					if(GUI.Button(new Rect(Screen.width/2+gap/2,Screen.height-(buttonSize+gap)*2+0.5f*gap,buttonSize,buttonSize),""))
					{
						currentBot = 1;
					}
					GUI.DrawTexture(new Rect(Screen.width/2-buttonSize-gap/2,Screen.height-buttonSize-0.5f*gap,buttonSize,buttonSize),ButtonBackground[7]);
					if(GUI.Button(new Rect(Screen.width/2-buttonSize-gap/2,Screen.height-buttonSize-0.5f*gap,buttonSize,buttonSize),""))
					{
						currentBot = 3;
					}
					GUI.DrawTexture(new Rect(Screen.width/2+gap/2,Screen.height-buttonSize-0.5f*gap,buttonSize,buttonSize),ButtonBackground[8]);
					if(GUI.Button(new Rect(Screen.width/2+gap/2,Screen.height-buttonSize-0.5f*gap,buttonSize,buttonSize),""))
					{
						currentBot = 2;
					}
				}
			}
		}
		//When the device has not started or joined a server yet
		if (!Network.isClient && !Network.isServer){
			//Remove comments below to seperate start and join per device
			//if (Application.platform != RuntimePlatform.Android){
				//The button used by the PC to start the server
				if (GUI.Button(new Rect(50, 50, bttnW, bttnH), "Start Server")){
					startServer();
				}
			//}
			//else{
				//The button used by the tablets to find the server, updates the list
				if (GUI.Button(new Rect(50, 100, bttnW, bttnH), "Find Server")){
					Debug.Log("Refreshing...");
					RefreshHostList();
				}
				GUILayout.BeginArea(new Rect(Screen.width - 500, 0, 500, Screen.height), "Server List");
				GUILayout.Space(20);
				//List all of the servers
				foreach (HostData hostedGame in MasterServer.PollHostList())
				{
					//Show the servers name and allow players to connect
					GUILayout.BeginHorizontal("Box");
					GUILayout.Label(hostedGame.gameName);
					if (GUILayout.Button("Connect")){
						Network.Connect(hostedGame);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndArea();
			//}
		}
	}
}