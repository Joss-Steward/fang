module ircTypes
open System.Text.RegularExpressions

let (|Match|_|) (pat:string) (inp:string) =
    let m = Regex.Match(inp, pat) in
    if m.Success then
        Some (List.tail [for g in m.Groups -> g.Value])
    else None

type PrivateMesg = {
    Nick: string;
    User: string;
    Host: string;
    Dest: string;
    Mesg: string
    }

type JoinMesg = {
    Nick: string;
    User: string;
    Host: string;
    Chan: string
    }

type inMessage =
    | PING of string                        // challenge
    | ERRO of string                        // description
    | NOTE of string                        // server notices
    | COMD of string * string * string      // number, who, text
    | PRIV of PrivateMesg                   // private message (to channel, usually)
    | JOIN of JoinMesg                      // user, channel
    | QUIT of string * string               // user, reason
    | OTHR

type outMessage = 
    | DM of string * string                 // dest, message    

// IRC Messages we might see
// PING :<number>                                   // Reply with PONG <number>
// ERROR :<message>: NICK[HOST] (NICK)              // ERROR message
// :SERVER NOTICE * :<notice_text>                  // Server notices
// :SERVER <DDD> NICK :<command_text>               // Commands?
// :NICK!USER@HOST PRIVMSG DEST :<message_text>     // Messages to channel or user
// :NICK!USER@HOST JOIN :<channel>                  // NICK joins channel
// :SERVER <DDD> NICK LEAVE :<reason>               // User parts channel
let ParseMessage (line: string) =
    match line with
    | Match @"PING :(.+)$" [what] ->    
        PING(what)
    | Match @"ERROR :(.+)$" [desc] -> 
        ERRO(desc)
    | Match @":[^ ]+ NOTICE [^ ]+ :(.+)$" [text] -> 
        NOTE(text)
    | Match @":[^ ]+ (\d\d\d) ([^:]+)(?: :(.+))?$" [number; who; text] -> 
        COMD(number, who, text)
    | Match @":([^!]+)!([^@]+)@([^ ]+) PRIVMSG ([^ ]+) :(.+)$" [nick; user; host; dest; mesg] -> 
        PRIV({Nick=nick; User=user; Host=host; Dest=dest; Mesg=mesg})
    | Match @":([^!]+)!([^@]+)@([^ ]+) JOIN ([^ ]+)$" [nick; user; host; chan] -> 
        JOIN({Nick=nick; User=user; Host=host; Chan=chan})
    | Match @":([^!]+)[^ ]+ QUIT :(.+)$" [user; reason] -> 
        QUIT(user, reason)
    | _ -> 
        OTHR