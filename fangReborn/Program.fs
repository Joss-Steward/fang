module main

open System
open System.IO
open System.Threading
open System.Collections.Generic
open System.Reactive
open FSharp.Control
open FSharpx.Control.Observable
open System.Text.RegularExpressions

open ircTypes
open ircConnection

let monitoredWords = dict[
                        "werc", new Dictionary<string, int>();
                        "gnu", new Dictionary<string, int>();
                        "excel", new Dictionary<string, int>();
                        "design pattern", new Dictionary<string, int>();
                        "rpas", new Dictionary<string, int>();
                        "vue", new Dictionary<string, int>();
                        "ember", new Dictionary<string, int>();
                        "angular", new Dictionary<string, int>();
                        "react", new Dictionary<string, int>();
                        ]

[<EntryPoint>]
let main argv = 
    let connection = IRCConnection("luna.red", 44444, "fang", "bokunopico911", ["#squad"])

    let wordOfGod = 
        connection.PrivateMessages
        |> Observable.filter(fun m -> m.Nick.Equals("joss") || m.Nick.Equals("j0ss"))

    let directCommands = 
        wordOfGod        
        |> Observable.filter(fun m -> m.Dest.Equals("fang"))
        
    let wordOfPlebs = 
        connection.PrivateMessages
        |> Observable.filter(fun m -> not (m.Nick.Equals("joss") || m.Nick.Equals("j0ss")))
    
    let joiners = 
        connection.JoinMessages

    let quitters = 
        connection.QuitMessages

    joiners.Subscribe(fun m ->
        if not (m.Nick = "fang") then connection.SendMessage(DM(m.Chan, "Hi " + m.Nick + "!"))
    ) |> ignore
    
    connection.PrivateMessages.Subscribe(fun m ->
        for forbiddenWord in monitoredWords do
            if m.Mesg.ToLower().Contains(forbiddenWord.Key) then 
                if forbiddenWord.Value.ContainsKey(m.Nick) then
                    let times = forbiddenWord.Value.Item(m.Nick) + 1
                    forbiddenWord.Value.Remove(m.Nick) |> ignore
                    forbiddenWord.Value.Add(m.Nick, times)
                else
                    forbiddenWord.Value.Add(m.Nick, 1)
                
                connection.SendMessage(DM(m.Dest, "'" + forbiddenWord.Key + "' is a forbidden word! You have been warned " + forbiddenWord.Value.Item(m.Nick).ToString() + " times, " + m.Nick))
    ) |> ignore

    let irisu =
        connection.PrivateMessages
        |> Observable.filter(fun m -> m.Nick.Equals("irisu"))
        |> Observable.filter(fun m -> m.Mesg.Contains("Time"))

    
    connection.PrivateMessages
    |> Observable.filter(fun m -> m.Nick.Equals("irisu"))
    |> Observable.filter(fun m -> m.Mesg.Contains("checked"))
    |> Observable.filter(fun m -> not (m.Mesg.Contains("fang")))
    |> Observable.subscribe(fun m -> connection.SendMessage(DM("#squad", "!checkem - dubs for me too")))
    |> ignore
    
    
//    let threadA = new Thread(fun () -> 
//        Thread.Sleep(500)
//        connection.SendMessage(DM("#squad", "!checkem"))
//
//        let r = Async.AwaitObservable(irisu) 
//        let res = Async.RunSynchronously r
//
//        let m = Regex.Match(res.Mesg, @"Time: (\d+)")
//        if m.Success then
//            let timeStamp = m.Groups.Item(1).Value
//            let nextTime = Int64.Parse(timeStamp.Substring(0, 6) + (String.replicate 4 (timeStamp.Substring(6).Chars(0).ToString())))
//            Console.WriteLine("Waiting until: {0}", nextTime)
//
//            let delta = (int)(nextTime - Int64.Parse(timeStamp))
//            Thread.Sleep(delta * 1000)
//            connection.SendMessage(DM("#squad", "!checkem"))
//    )
//
//    threadA.Start()
    connection.Run()
//    threadA.Join()

    0 // return an integer exit code
