namespace UnityVS.SpaceShooter_FSharp.FSharp
open UnityEngine
open System

module GameTypes =
    type Vector3Boundary() =
        [<SerializeField>][<DefaultValue>] val mutable xMin : float32
        [<SerializeField>][<DefaultValue>] val mutable xMax : float32
        [<SerializeField>][<DefaultValue>] val mutable yMin : float32
        [<SerializeField>][<DefaultValue>] val mutable yMax : float32
        [<SerializeField>][<DefaultValue>] val mutable zMin : float32
        [<SerializeField>][<DefaultValue>] val mutable zMax : float32

        member this.GetBoundary()=
            (this.xMin, this.xMax, this.yMin, this.yMax, this.zMin, this.zMax)
