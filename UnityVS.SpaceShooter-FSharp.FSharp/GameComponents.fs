namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

type PlayerController() = 
    inherit MonoBehaviour()

    [<SerializeField>]
    [<DefaultValue>] val mutable speed : float32

    let startEvt = Event<_>()
    let fixedEvt = Event<_>()

    member this.Start() =
        startEvt.Trigger()

    member this.FixedUpdate() =
        fixedEvt.Trigger(Time.deltaTime)

    member this.StartAsync() = Async.AwaitEvent startEvt.Publish
    member this.FixedUpdateAsync() = Async.AwaitEvent fixedEvt.Publish

    member this.Awake() =
        let transform = this.GetComponent<Transform>()
        let rigidbody = this.GetComponent<Rigidbody>()
        let workflow = async {
            do! this.StartAsync()
            let rec gameloop() = async {
                let! deltaTime = this.FixedUpdateAsync()
                let moveHorizontal = Input.GetAxis("Horizontal")
                let moveVertical = Input.GetAxis("Vertical")
                let movement = new Vector3(moveHorizontal, 0.0f, moveVertical)

                rigidbody.velocity <- (movement * this.speed * deltaTime)
                return! gameloop()
            }
            return! gameloop()
        }
        workflow |> Async.StartImmediate |> ignore