namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open UnityEngine.UI
open System


type DestroyByTime() =
    inherit MonoBehaviour()

    [<SerializeField>]
    let mutable lifetime = Unchecked.defaultof<float32>
    
    member this.Start()=
        GameObject.Destroy(this.gameObject, lifetime)

type RandomRotator() =
    inherit MonoBehaviour()

    [<SerializeField>]
    [<DefaultValue>] val mutable tumble : float32

    member this.Start() =
        this.GetComponent<Rigidbody>().angularVelocity <- Random.insideUnitSphere * this.tumble

type Mover() = 
    inherit MonoBehaviour()

    [<SerializeField>]
    [<DefaultValue>] val mutable speed : float32

    member this.Start() =
        this.GetComponent<Rigidbody>().velocity <- this.transform.forward * this.speed

type DestroyByBoundary() =
    inherit MonoBehaviour()

    member this.OnTriggerExit(other : Collider) =
        GameObject.Destroy(other.gameObject)

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

    member this.StartAsync() = Async.AwaitEvent startEvt.Publish
    member this.FixedUpdateAsync() = Async.AwaitEvent fixedEvt.Publish
    member this.FireAsync() = Async.AwaitEvent fireEvt.Publish

    member this.Awake() =
        let transform = this.GetComponent<Transform>()
        let rigidbody = this.GetComponent<Rigidbody>()
        //start the fixed update workflow
        let fixedWF = async {
            do! this.StartAsync()
            let playerCtrlr = GameLogic.playerControl rigidbody this.boundary this.speed this.tilt
            let rec gameloop() = async {
                let! deltaTime = this.FixedUpdateAsync()
                let (pos, vel, rot) = playerCtrlr deltaTime
                rigidbody.position <- pos
                rigidbody.velocity <- vel
                rigidbody.rotation <- rot
                return! gameloop()
            }
            return! gameloop()
        }
        //start the Firing workflow
        let fireWF = async {
            do! this.StartAsync()
            let fireCtrlr = GameLogic.playerFire this.shot this.shotSpawn this.fireRate
            let rec fireloop(nextFire) = async {
                let! time = this.FireAsync()
                let nextfiretime = fireCtrlr time nextFire
                return! fireloop(nextfiretime)
            }
            return! fireloop(0.0f)
        }
        fixedWF |> Async.StartImmediate |> ignore
        fireWF  |> Async.StartImmediate |> ignore


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


type GameController() =
    inherit MonoBehaviour()

    [<SerializeField>]
    [<DefaultValue>] val mutable hazard : GameObject

    [<SerializeField>]
    [<DefaultValue>] val mutable spawnPosition : Vector3

    [<SerializeField>]
    let mutable speed = Unchecked.defaultof<float32>

    [<SerializeField>]
    let mutable hazardCount = Unchecked.defaultof<int>

    [<SerializeField>]
    let mutable spawnWait = Unchecked.defaultof<float32>

    [<SerializeField>]
    let mutable startWait = Unchecked.defaultof<float32>

    [<SerializeField>]
    let mutable waveWait = Unchecked.defaultof<float32>
    
    [<SerializeField>]
    let mutable canvas = Unchecked.defaultof<Canvas>

    let mutable uiHelper = Unchecked.defaultof<UIHelper>
    let mutable gameState = Unchecked.defaultof<GameState>
    
    member this.Awake() =
        uiHelper <- canvas.GetComponent<UIHelper>()

    member this.Start() =
        gameState <- Scoring(0)
        uiHelper.UpdateScore("0")
        this.SpawnWaves()

    member this.Update() =
        match gameState with
        | Restarting when Input.GetKeyDown(KeyCode.R) -> this.Restart()
        | _ -> ()

    member this.SpawnWaves() =
        let spawner() = 
            GameLogic.randomSpawner this.hazard speed this.spawnPosition.x this.spawnPosition.z |> ignore

        let restart() =
            uiHelper.SetRestartActive(true)
            gameState <- Restarting

        GameLogic.spawnWaves spawner hazardCount startWait spawnWait waveWait (fun () -> gameState = GameOver) restart

    member this.GetScore(state) =
        match state with
        | Scoring(x) -> x
        | _ -> 0

    member this.Restart() : unit =
        Application.LoadLevel(Application.loadedLevel)

    member public this.DestroyedHazard(scoredPoints) =
        let score = this.GetScore(gameState) + scoredPoints
        gameState <- Scoring(score)
        uiHelper.UpdateScore(score.ToString())

    member public this.PlayerDestroyed() =
        uiHelper.SetGameOverActive(true)
        gameState <- GameOver


type DestroyByContact() =
    inherit MonoBehaviour()

    [<SerializeField>]
    [<DefaultValue>] val mutable explosion : GameObject

    [<SerializeField>]
    [<DefaultValue>] val mutable playerExplosion : GameObject

    [<SerializeField>]
    [<DefaultValue>] val mutable scorePoints : int


    let mutable gameController = Unchecked.defaultof<GameController>

    member this.Awake() =
        let gcObject = GameObject.FindWithTag("GameController")
        if not (gcObject = null) then gameController <- gcObject.GetComponent<GameController>()

    member this.OnTriggerEnter(other : Collider) =
        match other.tag with
        | "Boundary" -> ()
        | "Hazard" -> ()
        | "Player" ->
            gameController.PlayerDestroyed()
            GameObject.Instantiate(this.playerExplosion, other.transform.position, other.transform.rotation) |> ignore
            GameObject.Instantiate(this.explosion, this.transform.position, this.transform.rotation) |> ignore
            GameObject.Destroy(other.gameObject)
            GameObject.Destroy(this.gameObject)
        | _ ->
            gameController.DestroyedHazard(this.scorePoints)
            GameObject.Instantiate(this.explosion, this.transform.position, this.transform.rotation) |> ignore
            GameObject.Destroy(other.gameObject)
            GameObject.Destroy(this.gameObject)

