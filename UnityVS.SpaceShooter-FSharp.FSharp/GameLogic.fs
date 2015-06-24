namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

module GameLogic =
    open System.Collections
    /// Clamp the position within the defined x & z boundary
    let clampPosition (position : Vector3) (xMin, xMax, _, _, zMin, zMax) =
        new Vector3(Mathf.Clamp(position.x, xMin, xMax), 0.0f, Mathf.Clamp(position.z, zMin, zMax))
    
    /// Take current input and return a Vector3
    let getMovement() =
        let moveHorizontal = Input.GetAxis("Horizontal")
        let moveVertical = Input.GetAxis("Vertical")
        new Vector3(moveHorizontal, 0.0f, moveVertical)
    
    /// boundary to current position
    let playerPosition (rigidbody : Rigidbody) (boundary : GameTypes.Vector3Boundary) =
        boundary.GetBoundary()
        |> clampPosition rigidbody.position
    
    /// speed and deltaTime to Input axis'
    let playerVelocity (rigidbody : Rigidbody) speed deltaTime =
        getMovement() * speed * deltaTime

    /// rotation based on x velocity and tilt amount
    let playerRotation (rigidbody : Rigidbody) tilt =
        Quaternion.Euler(0.0f, 0.0f, rigidbody.velocity.x * -tilt)

    /// Instantiate SHOT and calculate next fire time
    let playerFire (shot : GameObject) (shotSpawn: Transform) fireRate (time : float32) nextFire = 
        if time >= nextFire then 
            GameObject.Instantiate(shot, shotSpawn.position, Quaternion.identity) |> ignore
            time + fireRate
        else nextFire

    /// Randomly instantiate a hazard within a given random range
    let randomSpawner (hazard : GameObject) (speed : float32) (xRange : float32) (zValue : float32) =
        let pos = new Vector3(Random.Range(-xRange,xRange),0.0f, zValue)
        GameObject.Instantiate(hazard, pos, Quaternion.identity) :?> GameObject
    
    /// Lazily evaluate the game state and spawn waves based on timing
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
