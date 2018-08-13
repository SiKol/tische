(*
 * This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
 * Refer to README.md in the Tische distribution for licensing and
 * distribution terms.
 *)

module workq

open System.Collections.Concurrent
open System.Threading.Tasks
open System

type WorkQ<'a>(size : int, nthreads : int, workfn : 'a -> unit) =
    let queue = new BlockingCollection<'a>(size)
    let workerfn = fun () -> 
        let mutable finished = false
        while not finished do 
            try workfn(queue.Take())
            with :?InvalidOperationException -> finished <- true

    let tasks = [for i in [1..nthreads] -> Task.Factory.StartNew(workerfn)]

    member this.addWork (i : 'a) =
        queue.Add(i)
    
    member this.finish() =
        queue.CompleteAdding()
        this.wait()

    member this.wait() =
        for t in tasks do
            t.Wait()