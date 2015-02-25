namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

type PlayerController() = 
    inherit MonoBehaviour()

    [<SerializeField>]
    [<DefaultValue>] val mutable speed : float32

    [<SerializeField>]
    [<DefaultValue>] val mutable tilt : float32

    [<SerializeField>]
    [<DefaultValue>] val mutable fireRate : float32

    [<SerializeField>]
    [<DefaultValue>] val mutable boundary : GameTypes.Vector3Boundary

    [<SerializeField>]
    [<DefaultValue>] val mutable shot : GameObject

    [<SerializeField>]
    [<DefaultValue>] val mutable shotSpawn : Transform

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
        
        let fixedWF = async {
            do! this.StartAsync()
            let rec gameloop() = async {
                let! deltaTime = this.FixedUpdateAsync()
                let (pos, vel, rot) = GameLogic.playerControl rigidbody this.boundary this.speed this.tilt deltaTime
                rigidbody.position <- pos
                rigidbody.velocity <- vel
                rigidbody.rotation <- rot
                return! gameloop()
            }
            return! gameloop()
        }
        fixedWF |> Async.StartImmediate |> ignore

        let fireWF = async {
            do! this.StartAsync()
            let rec fireloop(nextFire) = async {
                let! time = this.FireAsync()
                let nextfiretime =
                    if time >= nextFire then 
                        GameObject.Instantiate(this.shot, this.shotSpawn.position, Quaternion.identity) |> ignore
                        time + this.fireRate
                    else nextFire
                return! fireloop(nextfiretime)
            }
            return! fireloop(0.0f)
        }
        fireWF |> Async.StartImmediate |> ignore


type Mover() = 
    inherit MonoBehaviour()

    [<SerializeField>]
    [<DefaultValue>] val mutable speed : float32

    member this.Start() =
        this.rigidbody.velocity <- this.transform.forward * this.speed