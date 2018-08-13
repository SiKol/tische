(*
 * This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
 * Refer to README.md in the Tische distribution for licensing and
 * distribution terms.
 *)

module output

open FsRegEx
open config
open System.IO
open xml
open pdf
open sql
open System

type OutputDocument = {
    AggregateKey : string;
    Rows : SQLRow list;
}

let rec splitN (num : int) (rest : string) =
   if rest.Length <= num
   then [rest]
   else List.append [rest.Substring(0, num)] (splitN num (rest.Substring(num)))

let multiLevelDirectory num filename =
    splitN num filename |> List.rev |> List.tail |> List.rev

let sanitizeFilename name = replace @"[^\w]" "_" name

let makeDirectory (config : Config) groupvalue =
    let outputDirectory = config.outputDirectory.Split [|'\\'|] |> List.ofArray

    let mldir = if config.dirlevels > 0
                then multiLevelDirectory config.dirlevels groupvalue
                else []

    List.append outputDirectory mldir |> String.concat "\\"

let makeBasename (config : Config) groupvalue =
    if config.isGrouped
    then config.outputFilename.Replace("%G", groupvalue)
    else config.outputFilename

let makeFilename (config : Config) groupvalue =
    let dir = makeDirectory config groupvalue
    let filename = makeBasename config groupvalue

    match dir.Length with
    | 0 -> filename
    | _ ->
        Directory.CreateDirectory(dir) |> ignore
        dir + "\\" + filename

let writeDocument (writer: PDFWriter) (config: Config) (doc: OutputDocument) =
    let filename = makeFilename config (sanitizeFilename doc.AggregateKey)

    if not config.overwrite && File.Exists(filename) then
        Console.WriteLine("skip \"{0}\" for group value \"{1}\" (already exists)",
                          filename, doc.AggregateKey)
    else
        let tempfile = filename + ".tmp"

        Console.WriteLine("writing to \"{0}\" for group value \"{1}\"", filename, doc.AggregateKey)
        makeXml config.columns doc.Rows
        |> writer.Transform tempfile

        File.Delete(filename)
        File.Move(tempfile, filename)
