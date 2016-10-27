module ircConnection

open ircTypes
open System.IO
open System.Net.Sockets

type IRCConnection(server: string, port: int, nick: string, password: string, channels: list<string>) =
    let tcpClient = new TcpClient(server, port)

    let iStream = new StreamReader(tcpClient.GetStream())
    let oStream = new StreamWriter(tcpClient.GetStream())

    let recv = fun() ->
        ParseMessage (iStream.ReadLine())
        
    let sendraw (message: string) =
        oStream.WriteLine(message)

    let send (message: outMessage) =
        match message with
        | DM(dest, message) ->
            oStream.WriteLine(sprintf "PRIVMSG %s :%s" dest message)

    let outbox = MailboxProcessor.Start(fun inbox ->
            async {
                while true do
                    let! msg = inbox.Receive()
                    send msg
            })    

    let allMessages = new Event<inMessage>()
    let privateMessages = new Event<PrivateMesg>()     
    let joinMessages = new Event<JoinMesg>()        
    let quitMessages = new Event<string * string>()        

    member this.AllMessages = allMessages.Publish
    member this.PrivateMessages = privateMessages.Publish    
    member this.JoinMessages = joinMessages.Publish  
    member this.QuitMessages = quitMessages.Publish

    member this.SendMessage msg = outbox.Post msg
    member this.Run() = 
        oStream.AutoFlush <- true
        sendraw(sprintf "PASS %s" password)
        sendraw(sprintf "USER %s %s %s %s" nick nick nick nick)
        sendraw(sprintf "NICK %s" nick)

        let mutable botConnected = false
        while not botConnected do
            match (recv()) with
            | PING(value) -> sendraw(sprintf "PONG %s" value)
            | COMD("001", _, _) -> 
                channels
                |> List.iter(fun channel ->
                    sendraw(sprintf "JOIN %s" channel))
                botConnected <- true
            | _ -> ()
        
        while tcpClient.Connected do
            match (recv()) with
            | PING(value) -> sendraw(sprintf "PONG %s" value)
            | message -> 
                // There is one event stream that has EVERYTHING
                allMessages.Trigger(message)

                // And then there are individual event streams for private messages, joins, and quits
                match message with 
                | PRIV(priv) -> privateMessages.Trigger(priv)
                | JOIN(join) -> joinMessages.Trigger(join)
                | QUIT(who, why) -> quitMessages.Trigger(who, why)
                | _ -> ()