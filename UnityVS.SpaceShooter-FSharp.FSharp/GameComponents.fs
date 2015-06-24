namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open UnityEngine.UI
open System

// ------------------------------------------------------------------------------------------------
// Destory a gameobject after a given lifetime in seconds
// ------------------------------------------------------------------------------------------------
type DestroyByTime() =
    inherit MonoBehaviour()

    [<SerializeField>]
    let mutable lifetime = Unchecked.defaultof<float32>
    
    member this.Start()=
        GameObject.Destroy(this.gameObject, lifetime)

// ------------------------------------------------------------------------------------------------
// Randomly set the rigidbody's angularVelocity
// ------------------------------------------------------------------------------------------------
type RandomRotator() =
    inherit MonoBehaviour()

    [<SerializeField>][<DefaultValue>] val mutable tumble : float32

    member this.Start() =
        this.GetComponent<Rigidbody>().angularVelocity <- Random.insideUnitSphere * this.tumble

// ------------------------------------------------------------------------------------------------
// Set the rigidbody velocity based on speed in the FORWARD direction
// ------------------------------------------------------------------------------------------------
type Mover() = 
    inherit MonoBehaviour()

    [<SerializeField>][<DefaultValue>] val mutable speed : float32

    member this.Start() =
        this.GetComponent<Rigidbody>().velocity <- this.transform.forward * this.speed

// ------------------------------------------------------------------------------------------------
// Destroy the object if it goes outside the boundary
// ------------------------------------------------------------------------------------------------
type DestroyByBoundary() =
    inherit MonoBehaviour()

    member this.OnTriggerExit(other : Collider) =
        GameObject.Destroy(other.gameObject)

// ------------------------------------------------------------------------------------------------
// Handle player movement and fireing
// ------------------------------------------------------------------------------------------------
type PlayerController() = 
    inherit MonoBehaviour()

    [<SerializeField>][<DefaultValue>] val mutable speed : float32
    [<SerializeField>][<DefaultValue>] val mutable tilt : float32
    [<SerializeField>][<DefaultValue>] val mutable fireRate : float32
    [<SerializeField>][<DefaultValue>] val mutable boundary : GameTypes.Vector3Boundary
    [<SerializeField>][<DefaultValue>] val mutable shot : GameObject
    [<SerializeField>][<DefaultValue>] val mutable shotSpawn : Transform

    let startEvt = Event<_>()
    let fixedEvt = Event<_>()
    let fireEvt  = Event<_>()

    member this.Start() =
        startEvt.Trigger()  

    member this.Update() =
        if Input.GetButton("Fire1") then fireEvt.Trigger(Time.time)

    member this.FixedUpdate() =
        fixedEvt.Trigger(Time.deltaTime)

    member this.Awake() =
        let transform = this.GetComponent<Transform>()
        let rigidbody = this.GetComponent<Rigidbody>()
        //start the fixed update workflow
        let fixedWF = async {
            do! Async.AwaitEvent startEvt.Publish
            let rec gameloop() = async {
                let! deltaTime = Async.AwaitEvent fixedEvt.Publish
                rigidbody.position <- GameLogic.playerPosition rigidbody this.boundary
                rigidbody.velocity <- GameLogic.playerVelocity rigidbody this.speed deltaTime
                rigidbody.rotation <- GameLogic.playerRotation rigidbody this.tilt
                return! gameloop()
            }
            return! gameloop()
        }
        //start the Firing workflow
        let fireWF = async {
            do! Async.AwaitEvent startEvt.Publish
            let fireCtrlr = GameLogic.playerFire this.shot this.shotSpawn this.fireRate
            let rec fireloop(nextFire) = async {
                let! time = Async.AwaitEvent fireEvt.Publish
                let nextfiretime = fireCtrlr time nextFire
                return! fireloop(nextfiretime)
            }
            return! fireloop(0.0f)
        }
        fixedWF |> Async.StartImmediate |> ignore
        fireWF  |> Async.StartImmediate |> ignore

// ------------------------------------------------------------------------------------------------
// Update scoring and game text on the screen
// ------------------------------------------------------------------------------------------------
type UIHelper() =
    inherit MonoBehaviour()

    [<SerializeField>]
    let mutable scoreText = Unchecked.defaultof<Text>
    [<SerializeField>]
    let mutable gameOverText = Unchecked.defaultof<Text>
    [<SerializeField>]
    let mutable restartText = Unchecked.defaultof<Text>

    member this.Start() =
        gameOverText.enabled <- false
        restartText.enabled <- false

    member public this.UpdateScore(score) =
        scoreText.text <- "Score: " + score

    member public this.SetGameOverActive(isActive) =
        gameOverText.enabled <- isActive

    member public this.SetRestartActive(isActive) =
        restartText.enabled <- isActive

// ------------------------------------------------------------------------------------------------
// Main game state controller
// ------------------------------------------------------------------------------------------------
type GameController() =
    inherit MonoBehaviour()

    [<SerializeField>][<DefaultValue>] val mutable hazard : GameObject
    [<SerializeField>][<DefaultValue>] val mutable spawnPosition : Vector3
    [<SerializeField>][<DefaultValue>] val mutable speed : float32
    [<SerializeField>][<DefaultValue>] val mutable hazardCount : int
    [<SerializeField>][<DefaultValue>] val mutable spawnWait : float32
    [<SerializeField>][<DefaultValue>] val mutable startWait : float32
    [<SerializeField>][<DefaultValue>] val mutable waveWait : float32
    [<SerializeField>][<DefaultValue>] val mutable canvas : Canvas
    [<SerializeField>][<DefaultValue>] val mutable uiHelper : UIHelper
    [<SerializeField>][<DefaultValue>] val mutable gameState : GameState
    
    member this.Awake() =
        this.uiHelper <- this.canvas.GetComponent<UIHelper>()

    member this.Start() =
        this.gameState <- Scoring(0)
        this.uiHelper.UpdateScore("0")
        this.SpawnWaves()

    member this.Update() =
        match this.gameState with
        | Restarting when Input.GetKeyDown(KeyCode.R) -> this.Restart()
        | _ -> ()

    member this.SpawnWaves() =
        let spawner() = 
            GameLogic.randomSpawner this.hazard this.speed this.spawnPosition.x this.spawnPosition.z |> ignore

        let restart() =
            this.uiHelper.SetRestartActive(true)
            this.gameState <- Restarting

        GameLogic.spawnWaves spawner this.hazardCount this.startWait this.spawnWait this.waveWait (fun () -> this.gameState = GameOver) restart

    member this.GetScore(state) =
        match state with
        | Scoring(x) -> x
        | _ -> 0

    member this.Restart() : unit =
        Application.LoadLevel(Application.loadedLevel)

    member public this.DestroyedHazard(scoredPoints) =
        let score = this.GetScore(this.gameState) + scoredPoints
        this.gameState <- Scoring(score)
        this.uiHelper.UpdateScore(score.ToString())

    member public this.PlayerDestroyed() =
        this.uiHelper.SetGameOverActive(true)
        this.gameState <- GameOver

// ------------------------------------------------------------------------------------------------
// Handle explosions and scoring between player-hazard and shot-hazard collisions. 
// ------------------------------------------------------------------------------------------------
type DestroyByContact() =
    inherit MonoBehaviour()

    [<SerializeField>][<DefaultValue>] val mutable explosion : GameObject
    [<SerializeField>][<DefaultValue>] val mutable playerExplosion : GameObject
    [<SerializeField>][<DefaultValue>] val mutable scorePoints : int

    let mutable gameController = Unchecked.defaultof<GameController>

    member this.Awake() =
        let gcObject = GameObject.FindWithTag("GameController")
        if not (gcObject = null) then gameController <- gcObject.GetComponent<GameController>()

    member this.OnTriggerEnter(other : Collider) =
        match other.tag with
        | "Boundary" -> ()
        | "Hazard" -> ()
        | "Player" -> this.playerHazardCollision(other)
        | _ -> this.shotHazardCollision(other)

    member this.shotHazardCollision(other : Collider) =
        gameController.DestroyedHazard(this.scorePoints)
        GameObject.Instantiate(this.explosion, this.transform.position, this.transform.rotation) |> ignore
        GameObject.Destroy(other.gameObject)
        GameObject.Destroy(this.gameObject)

    member this.playerHazardCollision(other : Collider) =
        gameController.PlayerDestroyed()
        GameObject.Instantiate(this.playerExplosion, other.transform.position, other.transform.rotation) |> ignore
        GameObject.Instantiate(this.explosion, this.transform.position, this.transform.rotation) |> ignore
        GameObject.Destroy(other.gameObject)
        GameObject.Destroy(this.gameObject)