namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

type Controller1()=
    inherit MonoBehaviour()

    member this.Start() =
        Debug.Log("Component Started!")

