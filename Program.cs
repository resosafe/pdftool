using System;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Element;
using iText.Layout;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.XMP;
using iText.IO.Source;
using System.IO;

using iText.Kernel.Utils;
using iText.Kernel.XMP.Options;
using System.Text;
using iText.Kernel.XMP.Impl;
using System.Collections.Generic;
using NDesk.Options;
using Newtonsoft.Json;
using iText.Kernel.XMP.Properties;

namespace itexttest
{

    public static class Globals
    {
        public static string USERNAME = "";
        public static int VERBOSITY = 0;
    }



    class Command
    {
        private void ShowHelp(OptionSet optset)
        {
            optset.WriteOptionDescriptions(Console.Out);
        }

        public void ParseParameters(string[] args, OptionSet optset)
        {
            try
            {
                optset.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(optset);
            }
        }
    }


    class DocInfos : Command
    {

        public const string GM_NAMEPACE = "http://resosafe.fr/ns/gm";

        class Position
        {
            public int? Pg { get; set; }
            public int? X1 { get; set; }
            public int? X2 { get; set; }
            public int? Y1 { get; set; }
            public int? Y2 { get; set; }
        }
        class Field
        {
            public string Id { get; set; }
            public string Value { get; set; }
            public List<Position> Positions { get; set; }

            public Field()
            {
                Positions = new List<Position>();
            }
        }


        class Infos
        {
            public string Type { get; set; }
            public List<Field> Fields { get; set; }
            public string Author { get; set; }

            public Infos()
            {
                Fields = new List<Field>();
            }

        }


        public void Get(string[] args)
        {
            string srcFilePath = "";
            var p = new OptionSet() {
                { "i|input=", "input file path",
                   v => srcFilePath = v }
            };
            ParseParameters(args, p);


            PdfReader reader = new PdfReader(srcFilePath);
            PdfDocument pdfDoc = new PdfDocument(reader);
            Document doc = new Document(pdfDoc);
            XMPMeta m1 = XMPMetaFactory.ParseFromBuffer(pdfDoc.GetXmpMetadata(true));
            Infos infos = new Infos
            {
                Type = m1.GetProperty(GM_NAMEPACE, "DocInfos/gm:type").ToString()
            };


            for (int i = 1; i < m1.CountArrayItems(GM_NAMEPACE, "DocInfos/gm:Fields") + 1; i++)
            {
                Field field = new Field
                {
                    Id = m1.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:id"),
                    Value = m1.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:value"),


                };

                for (int j = 1; j < m1.CountArrayItems(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:pos") + 1; j++)
                {
                    Position position = new Position
                    {
                        Pg = m1.GetPropertyInteger(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:pos[" + (j) + "]/gm:pg"),
                        X1 = m1.GetPropertyInteger(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:pos[" + (j) + "]/gm:x1"),
                        X2 = m1.GetPropertyInteger(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:pos[" + (j) + "]/gm:x2"),
                        Y1 = m1.GetPropertyInteger(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:pos[" + (j) + "]/gm:y1"),
                        Y2 = m1.GetPropertyInteger(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:pos[" + (j) + "]/gm:y2"),
                    };
                    field.Positions.Add(position);
                }

                infos.Fields.Add(field);


            }



            Console.WriteLine(JsonConvert.SerializeObject(infos, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));


        }


        public void Set(string[] args)
        {
            string srcFilePath = "";
            string infosJSON = "";
            var p = new OptionSet() {
                { "i|input=", "file path",
                   v => srcFilePath = v },
                {"d|data=", "infos (json)",
                    v => infosJSON = v}
            };

            ParseParameters(args, p);

            //{"type": "/doc/type/subtype", "fields": [{"id":"field id","value":"keyword value","positions":[{"pg":1,"x1":1,"x2":2,"y1":10,"y2":20}, {"pg":1,"x1":2,"x2":3,"y1":30,"y2":50}]}]}

            Infos docInfos = JsonConvert.DeserializeObject<Infos>(infosJSON);


            string destFilePath = srcFilePath + ".tmp";

            using (var file = new FileStream(srcFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                PdfReader reader = new PdfReader(file);
                PdfWriter writer = new PdfWriter(destFilePath);

                PdfDocument pdfDoc = new PdfDocument(reader, writer);

                XMPMeta m1 = XMPMetaFactory.ParseFromBuffer(pdfDoc.GetXmpMetadata(true));
                XMPSchemaRegistry registry = XMPMetaFactory.GetSchemaRegistry();
                registry.RegisterNamespace(GM_NAMEPACE, "gm");

                string keywords = "type:" + docInfos.Type;
                m1.DeleteProperty(GM_NAMEPACE, "DocInfos");
                m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:type", docInfos.Type, new PropertyOptions(PropertyOptions.NO_OPTIONS));

                foreach (var field in docInfos.Fields)
                {
                    keywords += "," + field.Id + ":" + field.Value;
                    m1.AppendArrayItem(GM_NAMEPACE, "DocInfos/gm:Fields", new PropertyOptions(PropertyOptions.ARRAY), null, new PropertyOptions(PropertyOptions.STRUCT));
                    m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:id", field.Id, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                    m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:value", field.Value, new PropertyOptions(PropertyOptions.NO_OPTIONS));

                    foreach (var pos in field.Positions)
                    {
                        m1.AppendArrayItem(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:pos", new PropertyOptions(PropertyOptions.ARRAY), null, new PropertyOptions(PropertyOptions.STRUCT));
                        m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:pos[last()]/gm:pg", pos.Pg, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:pos[last()]/gm:x1", pos.X1, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:pos[last()]/gm:x2", pos.X2, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:pos[last()]/gm:y1", pos.Y1, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        m1.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:pos[last()]/gm:y2", pos.Y2, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                    }
                }

                m1.AppendArrayItem(XMPConst.NS_XMP_MM, "History", new PropertyOptions(PropertyOptions.ARRAY_ORDERED), null, new PropertyOptions(PropertyOptions.STRUCT));
                m1.SetProperty(XMPConst.NS_XMP_MM, "History[last()]/stEvt:action", "indexed", new PropertyOptions(PropertyOptions.NO_OPTIONS));
                if (Globals.USERNAME.Length > 0)
                {
                    m1.SetProperty(XMPConst.NS_XMP_MM, "History[last()]/stEvt:parameters", "by " + Globals.USERNAME, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                }

                pdfDoc.GetDocumentInfo().SetKeywords(keywords);
                pdfDoc.SetXmpMetadata(m1);
                pdfDoc.Close();

                File.Delete(srcFilePath);
                File.Move(destFilePath, srcFilePath);
            }
        }
    }



    class DocumentManipulation : Command
    {

        private class CustomSplitter : PdfSplitter
        {
            private int count;
            private readonly string destinationPath;

            public CustomSplitter(PdfDocument pdfDocument, string destination) : base(pdfDocument)
            {
                destinationPath = destination;
                count = 1;
            }

            protected override PdfWriter GetNextPdfWriter(PageRange documentPageRange)
            {
                return new PdfWriter(destinationPath + count++ + ".pdf");
            }
        }

        public void Split(string[] args)
        {
            string srcFilePath = "";

            var p = new OptionSet() {
                { "i|input=", "input file path",
                   v => srcFilePath = v }
            };

            ParseParameters(args, p);

            using (PdfDocument pdfDoc = new PdfDocument(new PdfReader(srcFilePath)))
            {
                CustomSplitter splitter = new CustomSplitter(pdfDoc, System.IO.Path.ChangeExtension(srcFilePath, null) + "-split-");
                IList<PdfDocument> splittedDocs = splitter.SplitByPageCount(1);

                foreach (PdfDocument splittedDoc in splittedDocs)
                {
                    splittedDoc.Close();
                }
            }
        }

        public void Extract(string[] args)
        {
            string srcFilePath = "";
            string range = null;


            var p = new OptionSet() {
                { "i|input=", "file path",
                   v => srcFilePath = v },
                { "n|numbers=", "pages to extract",
                  v => range =v}
            };


            ParseParameters(args, p);

            using (PdfDocument pdfDoc = new PdfDocument(new PdfReader(srcFilePath)))
            {
                CustomSplitter splitter = new CustomSplitter(pdfDoc, System.IO.Path.ChangeExtension(srcFilePath, null) + "-extract-");
                splitter.ExtractPageRange(new PageRange(range)).Close();
            }

        }


        public void Merge(string[] args)
        {
            Console.WriteLine(args);
            List<string> srcFilesPath = new List<string>();
            string destFilePath = "";
            var p = new OptionSet() {
                { "i|input=", "input file path",
                   v => srcFilesPath.Add(v) },
                { "o|output=", "file path",
                   v => destFilePath = v}
            };

            ParseParameters(args, p);

            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(destFilePath));
            PdfMerger merger = new PdfMerger(pdfDoc);

            foreach (string src in srcFilesPath)
            {
                Console.WriteLine(src);
                PdfDocument srcPdfDoc = new PdfDocument(new PdfReader(src));
                merger.Merge(srcPdfDoc, 1, srcPdfDoc.GetNumberOfPages());
                srcPdfDoc.Close();
            }

            pdfDoc.Close();

        }

    }

    class MainClass
    {


        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: itext-test [OPTIONS]+");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }


        public static void Main(string[] args)
        {
            string action = "";

            var p = new OptionSet() {
                { "a|action=", "action to perform",
                   v => action = v.ToLower() },
                { "u|username=", "user performing the action",
                   v => Globals.USERNAME = v },
                { "v", "increase debug message verbosity",
                   v => { if (v != null) Globals.VERBOSITY++; } },
                { "h|help",  "show this message and exit",
                   v => action = "help" },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(p);
                return;
            }


            switch (action)
            {
                case "set-infos":
                    new DocInfos().Set(args);
                    break;
                case "get-infos":
                    new DocInfos().Get(args);
                    break;
                case "split":
                    new DocumentManipulation().Split(args);
                    break;
                case "merge":
                    new DocumentManipulation().Merge(args);
                    break;
                case "extract":
                    new DocumentManipulation().Extract(args);
                    break;
                default:
                    ShowHelp(p);
                    return;

            }
        }
    }
}



#if false
            string destFilePath = srcFilePath+".tmp";

            using (var file = new FileStream(srcFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                PdfReader reader = new PdfReader(file);
                PdfWriter writer = new PdfWriter(destFilePath);

                PdfDocument pdfDoc = new PdfDocument(reader, writer);
                Document doc = new Document(pdfDoc);

                PdfDocumentInfo pinfo = pdfDoc.GetDocumentInfo();
                Console.Write(pinfo.GetKeywords());

                // pinfo.SetKeywords("tests");
                //XMPMeta xmp = new XMPMeta();
                pinfo.SetMoreInfo("moreinfo1", "value2");

                /*
                PdfCatalog catalog=pdfDoc.GetCatalog();g

                catalog.g
                */
                byte[] xmpbytes = pdfDoc.GetXmpMetadata(true);
                Console.Write(System.Text.Encoding.UTF8.GetString(xmpbytes));

                XMPMeta m1 = XMPMetaFactory.ParseFromBuffer(xmpbytes);
                //m1.set
                //            m1.InsertArrayItem("xmpMM", "CustTags", 0, "valuetest");

                m1.AppendArrayItem(XMPConst.NS_XMP_MM, "Tags", new PropertyOptions(PropertyOptions.ARRAY), "valuetest2", new PropertyOptions(PropertyOptions.NO_OPTIONS));

                /*
                PdfDictionary catalog = pdfDoc.GetTrailer();
                PdfDictionary map = catalog.GetAsDictionary(PdfName.Info);
                map.Put(new PdfName("test"), new PdfString("test"));
                map.Put(new PdfName("test1"), new PdfString("test1"));
                */

                //String xmp = "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description xmlns:xmpMM=\"http://ns.adobe.com/xap/1.0/mm/\" rdf:about=\"\"><xmpMM:CustTags><rdf:Seq><rdf:li>elem1</rdf:li></rdf:Seq></xmpMM:CustTags></rdf:Description></rdf:RDF>";

                //XMPMeta m=XMPMetaFactory.ParseFromString(xmp,new ParseOptions().SetAcceptLatin1(true).SetRequireXMPMeta(false).SetStrictAliasing(false).SetOmitNormalization(true).SetFixControlChars(false));
                pdfDoc.SetXmpMetadata(m1);

                /*
                XMPMeta xMPMeta = new XMPMeta;
                XMPMeta xmp = xMPMeta;
                xmp.SetObjectName("XMPTest");

                pdfDoc.SetXmpMetadata(new XMPMeta())
                */

                /*

                PdfCanvas under = new PdfCanvas(pdfDoc.GetFirstPage().NewContentStreamBefore(), new PdfResources(), pdfDoc);

                PdfFont font = PdfFontFactory.CreateFont(StandardFontFamilies.HELVETICA);

                Paragraph p = new Paragraph("This watermark is added UNDER the existing content").SetFont(font).SetFontSize(15);

                new Canvas(under, pdfDoc, pdfDoc.GetDefaultPageSize())
                    .ShowTextAligned(p, 297, 550, 1, TextAlignment.CENTER, VerticalAlignment.TOP, 0);

               PdfCanvas over = new PdfCanvas(pdfDoc.GetFirstPage());
               over.SetFillColor(ColorConstants.BLACK);

                p = new Paragraph("This watermark is added ON TOP OF the existing content").SetFont(font).SetFontSize(15);

                new Canvas(over, pdfDoc, pdfDoc.GetDefaultPageSize())
                        .ShowTextAligned(p, 297, 500, 1, TextAlignment.CENTER, VerticalAlignment.TOP, 0);

                p = new Paragraph("This TRANSPARENT watermark is added ON TOP OF the existing content")
                        .SetFont(font).SetFontSize(15);

                over.SaveState();
                PdfExtGState gs1 = new PdfExtGState();
                gs1.SetFillOpacity(0.5f);
                over.SetExtGState(gs1);
                new Canvas(over, pdfDoc, pdfDoc.GetDefaultPageSize())
                        .ShowTextAligned(p, 297, 450, 1, TextAlignment.CENTER, VerticalAlignment.TOP, 0);
                over.RestoreState();
                */
                pdfDoc.Close();

                System.Threading.Thread.Sleep(15000);

                File.Delete(srcFilePath);
                File.Move(destFilePath, srcFilePath);
            }

#endif


/*
 * SEARCH TEXT
 * //Open PDF document
using (var doc = PdfDocument.Load(@"d:\0\test_big.pdf"))
{
    //Enumerate pages
    foreach(var page in doc.Pages)
    {
        var found = page.Text.Find("text for search", FindFlags.MatchWholeWord, 0);
        if (found == null)
            return; //nothing found
        do
        {
            var textInfo = found.FindedText;
            foreach(var rect in textInfo.Rects)
            {
                float x = rect.left;
                float y = rect.top;
                //...
            }
        } while (found.FindNext());

        page.Dispose();
    }
}


*/
