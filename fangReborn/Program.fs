module main

open System
open System.IO
open System.Threading

open ircTypes
open ircConnection

[<EntryPoint>]
let main argv = 
    let connection = IRCConnection("luna.red", 44444, "fang", "bokunopico911", ["#squad"])

    let wordOfGod = 
        connection.PrivateMessages
        |> Observable.filter(fun m -> m.Nick.Equals("joss"))
        
    let wordOfPlebs = 
        connection.PrivateMessages
        |> Observable.filter(fun m -> not (m.Nick.Equals("joss")))
    
    let quitters = 
        connection.QuitMessages

    connection.PrivateMessages
    |> Observable.

    connection.Run()
    
    0 // return an integer exit code
