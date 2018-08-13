(*
 * This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
 * Refer to README.md in the Tische distribution for licensing and
 * distribution terms.
 *)

module sql

open config
open System.Data.SqlClient
open System.Data
open System.Collections.ObjectModel
open System.Data.Common

//========================
// SQL connection support.
//
// F# has very nice built-in SQL support with LINQ integration, but unfortunately that
// requires hardcoding the SQL queries, while we need to take them from our config file.
// Instead we use the .NET System.Data.SqlClient interface directly, which is a bit messy.

// A type that represents the description of a column.
type SQLColumn = {
    Name : string;
}

// A type that represents one column in a result row.
type SQLField = {
    Name : string;
    Value : string;
}

// A type that represents a single result row.
type SQLRow = {
    Fields : list<SQLField>;
}

// Find a column of the given name in a row.  If no column exists, throw KeyNotFound.

let rec fetch' (key : string) fields =
    match fields with
    | [] -> None
    | x::xs ->
        if x.Name = key
        then Some x.Value
        else fetch' key xs

exception KeyNotFound of string
let fetch key row =
    match fetch' key row.Fields with
    | Some value    -> value
    | None          -> raise (KeyNotFound("could not find key in row"))

// Given a reader and a field index, make a Field for that field.
let makeField (reader : SqlDataReader) fieldnum = {
    Name = reader.GetName(fieldnum);
    Value = reader.[fieldnum].ToString()
}

// Given an SqlDataReader, return a Row representing the current row.
let makeRow (reader : SqlDataReader) = {
    SQLRow.Fields = [ for i in 0 .. (reader.FieldCount - 1) -> (makeField reader i) ]
}

// Given an SQL DbColumn, create a Column.
let makeColumn (column : DataRow) = {
    SQLColumn.Name = column.Item("ColumnName") :?> string
}

// Given a DataTable describing the schema (from SqlDataReader.GetSchemaTable),
// create a Header to describe the result schema.
let makeHeader (schema : DataTable) = [ for col in schema.Rows -> makeColumn col ]

let fetchRows (conn : SqlConnection) (reader : SqlDataReader) =
    seq {
        while reader.Read() do
            yield (makeRow reader)
        reader.Close()
        conn.Close()
    }

// Connect to the server with the given connection string, execute the
// given query and yield the result as a sequence of Rows.
let runQuery (config : Config) cnstr q =
    config.vlog (sprintf "connecting to SQL server: %s... " cnstr)
    let conn = new SqlConnection(cnstr)
    conn.Open()
    config.vlog "ok\n"

    let cmd = new SqlCommand(q, conn)
    cmd.CommandTimeout <- 0
    cmd.CommandType <- CommandType.Text

    config.vlog "executing query..."
    let reader = cmd.ExecuteReader()
    config.vlog "ok\n"

    (makeHeader (reader.GetSchemaTable()), fetchRows conn reader)
