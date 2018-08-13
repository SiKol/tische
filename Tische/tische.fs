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

type Resources = ResXProvider<"Resources.resx">

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
    let writer = new PDFWriter(Resources.style)

    // Create a threaded work queue to do the actual writing.
    let workq = new WorkQ<OutputDocument>(
                    config.threads * 50,    // queue length
                    config.threads,         // concurrency
                    fun doc -> writeDocument writer config doc)

    try
        // Add all documents to the work queue and wait for processing to finish.
        documents |> Seq.iter workq.addWork
        workq.finish()
        0 // exit success
    with
    | :?System.AggregateException as excs ->
        for exc in excs.InnerExceptions do
            match exc with
            | KeyNotFound knf ->
                printfn "error: invalid column name \"%s\"" knf
            | any -> printfn "error: %s" any.Message
        1
    | ex ->
        printfn "error: %s" ex.Message
        1 // exit failure

(*
    // benchmarking
    let listrows = documents |> List.ofSeq

    let stopwatch = new System.Diagnostics.Stopwatch()
    stopwatch.Start()
    let runs = 100
    for i in [1..runs] do
        listrows |> Seq.iter workq.addWork

    workq.finish()
    stopwatch.Stop()
    let ts = stopwatch.Elapsed
    printfn "%A" ts
    let secs = (double ts.Seconds) + ((double ts.Milliseconds) / 1000.0)
    let persec = (double runs) / secs
    printfn "%d runs in %.2fs (%.2f/sec)" runs secs persec
    0
    *)
