namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

module GameLogic =
    open System.Collections

    let playerControl (rigidbody : Rigidbody) (boundary : GameTypes.Vector3Boundary) speed tilt deltaTime =
        let moveHorizontal = Input.GetAxis("Horizontal")
        let moveVertical = Input.GetAxis("Vertical")
        let movement = new Vector3(moveHorizontal, 0.0f, moveVertical)
        let (xMin, xMax, _, _, zMin, zMax) = boundary.GetBoundary()
        let position = new Vector3(Mathf.Clamp(rigidbody.position.x, xMin, xMax), 0.0f, Mathf.Clamp(rigidbody.position.z, zMin, zMax))
        let velocity = (movement * speed * deltaTime)
        let rotation = Quaternion.Euler(0.0f, 0.0f, rigidbody.velocity.x * -tilt)
        (position, velocity, rotation)

    let playerFire (shot : GameObject) (shotSpawn: Transform) fireRate (time : float32) nextFire = 
        if time >= nextFire then 
            GameObject.Instantiate(shot, shotSpawn.position, Quaternion.identity) |> ignore
            time + fireRate
        else nextFire

    let randomSpawner (hazard : GameObject) (speed : float32) (xRange : float32) (zValue : float32) =
        let pos = new Vector3(Random.Range(-xRange,xRange),0.0f, zValue)
        GameObject.Instantiate(hazard, pos, Quaternion.identity) :?> GameObject
        
    let spawnWaves (spawner : unit -> unit) count startWait spawnWait waveWait (isGameOver : unit -> bool) (restart : unit -> unit) =
        seq {
            yield WaitForSeconds (startWait)
            while not (isGameOver()) do
                for i in 1 .. count do
                    spawner()
                    yield WaitForSeconds (spawnWait)
                yield WaitForSeconds (waveWait)
            restart()
        } :?> IEnumerator
