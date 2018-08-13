(*
 * This file is part of Tische, which is copyright (c) 2018 SiKol Ltd.
 * Refer to README.md in the Tische distribution for licensing and
 * distribution terms.
 *)

module pdf

open System.IO
open org.apache.fop.apps
open javax.xml.transform
open javax.xml.transform.stream
open javax.xml.transform.sax
open System.Xml
open System.Xml.Xsl
open System.Xml.Linq
open org.apache.fop.events
open org.apache.fop.events.model

//================
// PDF generation.

type TischeEventListener () =
    interface EventListener with
        member self.processEvent event =
            let msg = EventFormatter.format(event)
            let sev = event.getSeverity()
            if sev = EventSeverity.ERROR || sev = EventSeverity.FATAL then
                printfn "PDF output error: %s" msg

// A PDFWriter is an XML processor that takes XML data in our custom schema and
// writes a PDF to the given filename based on the input data.  It does this
// using an XSL-FO stylesheet provided when the writer is constructed.

type PDFWriter(xsl: string) =
    // Create our XSL transformer.
    let xslTransformer = new XslCompiledTransform()
    do
        let xslreader = XmlReader.Create(new StringReader(xsl))
        xslTransformer.Load(xslreader)

    // Create our XSL-FO transformer.
    let xformerfactory = TransformerFactory.newInstance()

    // FOP factory used to create new FOP transforms.
    let fopfactory = FopFactory.newInstance((new java.io.File(".")).toURI())

    // Transform input XML and return XSL-FO.
    let transform (xnode: XElement) =
        use xslfostream = new StringWriter()
        let xsltargs = new XsltArgumentList()
        use xmlreader = xnode.CreateReader()
        xslTransformer.Transform(xmlreader, xsltargs, xslfostream)
        xslfostream.ToString()

    // Transform given XML and output to the given filename as PDF.
    member self.Transform (filename: string) (xnode: XElement) =
        // transform the xml into xsl-fo
        let xsl = transform xnode
        // create our FOP UA
        let foua = fopfactory.newFOUserAgent()
        foua.getEventBroadcaster().addEventListener(new TischeEventListener())

        use outstream = new java.io.FileOutputStream(filename)
        let fop = fopfactory.newFop(MimeConstants.MIME_PDF, foua, outstream)
        let res = new SAXResult(fop.getDefaultHandler())

        let source = new StreamSource(new java.io.StringReader(xsl))
        let xformer = xformerfactory.newTransformer()
        xformer.transform(source, res)

        outstream.close()
