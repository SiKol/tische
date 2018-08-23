(*
 * This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
 * Refer to README.md in the Tische distribution for licensing and
 * distribution terms.
 *)

open sql
open config
open output
open workq
open pdf
open System.Collections.Generic
open System.IO
open FsRegEx
open FSharp.Configuration
open Printf
open System.Data.SqlClient

// Given a list of rows and an aggregating field name, return a sequence of
// OutputDocuments where each OutputDocument contains all rows for a particular
// key.  This requires that the rows be sorted by the aggregate key already.
//
// Unlike Seq.groupBy, this does not require evaluating the entire sequence to
// generate output, and can therefore handle arbitrarily large amounts of data.
let aggregate fieldname rows =
    seq {
        let mutable last = ""
        let mutable collected = []

        for row in rows do
            let v = fetch fieldname row
            if not collected.IsEmpty && v <> last then
                yield {
                    AggregateKey = last;
                    Rows = collected;
                }
                collected <- []
            collected <- List.append collected [row]
            last <- v

        if not collected.IsEmpty then
            yield {
                AggregateKey = last;
                Rows = collected;
            }
    }

// word wrap - for error formatting
let rec wrapwords l i (s: string) (ws: string list) =
    match ws with
    | [] -> s
    | x::xs ->
        if i+1+x.Length <= l
        then 
            if s="" then wrapwords l x.Length x xs
            else wrapwords l (i+1+x.Length) (s + " " + x) xs
        else wrapwords l x.Length (s + "\n" + x) xs
and wrap l (s: string) =
    let words = s.Split([| ' '; '\t'; '\n'; '\r' |]) |> List.ofArray
    (wrapwords l 0 "" words) + "\n"

[<EntryPoint>]
let main argv =
    // Load our configuration.
    let config =
        try new Config(argv) with
        | UsageError e ->
            exit 1
        | ConfigError e ->
            eprintfn "%s" e
            exit 1

    try
        // Fetch the data.
        let (_, rows) = runQuery config config.sqlconnection config.sqlquery

        // If a grouping field is specified, aggregate into OutputDocuments by
        // that field.  Otherwise, just use a single document containing all rows.
        let documents =
            if config.isGrouped
            then aggregate config.groupField rows
            else
                Seq.singleton {
                    AggregateKey = "";
                    Rows = rows |> List.ofSeq;
                }

        // Create our PDF writer.
        let writer = new PDFWriter(config.xslstyle)

        // Create a threaded work queue to do the actual writing.
        let workq = new WorkQ<OutputDocument>(
                        config.threads * 50,    // queue length
                        config.threads,         // concurrency
                        fun doc -> writeDocument writer config doc)

        // Add all documents to the work queue and wait for processing to finish.
        documents |> Seq.iter workq.addWork
        workq.finish()
        0 // exit success
    with
    | :?System.AggregateException as excs ->
        for exc in excs.InnerExceptions do
            printfn "\n%s" (sprintf "ERROR: %s" exc.Message |> wrap 79)
        1
    | exc ->
        printfn "\n%s" (sprintf "ERROR: %s" exc.Message |> wrap 79)
        1 // exit failure
