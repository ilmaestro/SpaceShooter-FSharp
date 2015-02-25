namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

module GameLogic =
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