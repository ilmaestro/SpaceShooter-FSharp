namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

type GameState =
    | Scoring of int
    | Restarting
    | GameOver
    