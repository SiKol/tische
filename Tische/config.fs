(*
 * This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
 * Refer to README.md in the Tische distribution for licensing and
 * distribution terms.
 *)

module config

open System.IO
open FSharp.Data
open System
open CommandLine
open FSharp.Configuration

type Resources = ResXProvider<"Resources.resx">

//=============================
// Configuration file handling.
//
// The configuration is a simple XML file containing our input source (connection string),
// the query to run, and the output file.

// Create the parser for our XML; this creates a type based on the schema.
type XmlConfig = XmlProvider<Schema="config.xsd">

// Exception thrown if the config can't be loaded.
exception ConfigError of string

// A type representing our parsed arguments.
type Arguments = {
    [<Option('v', "verbose", HelpText="be more verbose about operations")>]
    verbose : bool;
    [<Value(0, Default="config.xml", MetaName="config", HelpText="configuration file")>]
    config : string;
}

// Parse our arguments and return an Arguments object.
exception UsageError of string
let parseArguments (args : string[]) =
    let result = CommandLine.Parser.Default.ParseArguments<Arguments>(args)
    match result with
    | :? Parsed<Arguments> as parsed -> parsed.Value
    | _ -> raise (UsageError("cannot parse options"))

type ColumnDefinition = {
    Name: string
    Width: string option
    Heading: string
}

type Config(argv) =
    let args = parseArguments argv

    // Parse our XML configuration file and store the data.
    let xmlconfig =
        if args.verbose then printfn "loading configuration from \"%s\"..." args.config
        try     XmlConfig.Parse(File.ReadAllText(args.config))
        with    e -> raise (ConfigError(e.Message))

    member val verbose = args.verbose

    member self.vlog(s:string) = if self.verbose then Console.Write("{0}", s)

    // The number of processing threads to create.
    member self.threads =
        match xmlconfig.Threads with
        | None      -> 1
        | Some n    -> int n

    // The SQL Server connection string to use.
    member self.sqlconnection = xmlconfig.Input.Connection
    // The SQL query to execute.
    member self.sqlquery = xmlconfig.Input.Query

    // The output filename.
    member self.outputFilename = xmlconfig.Output.Filename

    // The directory to place output files in.
    member self.outputDirectory =
        match xmlconfig.Output.Directory with
        | None      -> "."
        | Some dir  -> dir

    // If true, overwrite existing files, otherwise skip them.
    member self.overwrite =
        match xmlconfig.Output.Overwrite with
        | None      -> true
        | Some v    -> v

    // If true, results should be grouped.
    member self.isGrouped =
        match xmlconfig.Output.Group with
        | Some _    -> true
        | None      -> false

    // If isGrouped is true, this is the field to group by.
    member self.groupField =
        match xmlconfig.Output.Group with
        | Some gc   -> gc.Field
        | None      -> raise (ConfigError("group field not configured"))

    // When grouping output, the number of directory levels to create (may be zero).
    member self.dirlevels =
        match xmlconfig.Output.Group with
        | None      -> 0
        | Some gc   ->
            match gc.Multilevel with
            | None -> 0
            | Some levels -> int levels

    // String containing the XSL stylesheet for rendering.
    member self.xslstyle =
        match xmlconfig.Output.Style with
        | None -> Resources.style
        | Some filename -> 
            try File.ReadAllText(filename)
            with exc -> failwithf "could not load the XSL transform from \
file \"%s\" (specified in configuration file): %s" filename exc.Message

    // Column definitions
    member self.columns =
        [
            for col in xmlconfig.Output.Columns ->
                {
                    Name = col.Name
                    Width = col.Width
                    Heading =
                        match col.Heading with
                        | Some s -> s
                        | None -> col.Name
                }
        ]
