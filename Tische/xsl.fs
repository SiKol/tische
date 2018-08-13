(*
 * This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
 * Refer to README.md in the Tische distribution for licensing and
 * distribution terms.
 *)

module xml

open sql
open config
open System.Xml.Linq

// Generate our internal XML data format.  This is passed to an XSL-FO
// transform to produce the output.

let xn (s: string) = XName.Get(s)

let item (s: string) = new XElement(xn "i", s)
let column coldef row = fetch coldef.Name row |> item

let row cols r =
    let items = [for col in cols -> column col r]
    new XElement(xn "row", items)

let header cols = new XElement(xn "header", [for col in cols -> item col.Heading])

let xmlColumn (def: ColumnDefinition) =
    let width =
        match def.Width with
        | Some str -> str
        | None -> "auto"

    new XElement(xn "column",
        new XAttribute(xn "name", def.Name),
        new XAttribute(xn "width", width))

let xmlColumns (columns: ColumnDefinition list) =
    new XElement(xn "columns", columns |> Seq.map xmlColumn)

// As htmlTable, but adding the header and footer.
let makeXml (columns: ColumnDefinition list) rows =
    new XElement(xn "report",
        xmlColumns columns,
        header columns,
        rows |> Seq.map (row columns))
