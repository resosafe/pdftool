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
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Xml;
using iText.Kernel;

namespace pdftool
{

    public static class Globals
    {
        public static string USERNAME = "";
        public static int VERBOSITY = 0;
    }



    class Command
    {
        public static void ShowHelp(OptionSet optset)
        {
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name+" "+Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("Options:");
            optset.WriteOptionDescriptions(Console.Out);
        }

        public static void  ParseParameters(string[] args, OptionSet optset)
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
            [JsonProperty("pg")]
            public int? Pg { get; set; }
            [JsonProperty("x1")]
            public double? X1 { get; set; }
            [JsonProperty("x2")]
            public double? X2 { get; set; }
            [JsonProperty("y1")]
            public double? Y1 { get; set; }
            [JsonProperty("y2")]
            public double? Y2 { get; set; }
        }
        class Field
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("value")]
            public string Value { get; set; }
            [JsonProperty("positions")]
            public List<Position> Positions { get; set; }

            public Field()
            {
                Positions = new List<Position>();
            }
        }


        class Infos
        {
            [JsonProperty("types")]
            public List<string> Types { get; set; }
            [JsonProperty("indexationDate")]
            public string IndexationDate { get; set; }
            [JsonProperty("indexationStatus")]
            public string IndexationStatus { get; set; }
            [JsonProperty("tags")]
            public List<string> Tags { get; set; }
            [JsonProperty("fields")]
            public List<Field> Fields { get; set; }
            [JsonProperty("author")]
            public string Author { get; set; }
            [JsonProperty("when")]
            public string When { get; set; }

            public Infos()
            {
                Fields = new List<Field>();
                Tags = new List<string>();
                Types = new List<string>();
            }

        }


        class HistoryEvent
        {
            [JsonProperty("action")]
            public string Action { get; set; }
            [JsonProperty("changed")]
            public string Changed { get; set; }
            [JsonProperty("instanceID")]
            public string InstanceID { get; set; }
            [JsonProperty("parameters")]
            public string Parameters { get; set; }
            [JsonProperty("softwareAgent")]
            public string SoftwareAgent { get; set; }
            [JsonProperty("when")]
            public string When { get; set; }


        }

        class History
        {
            [JsonProperty("events")]
            public List<HistoryEvent> Events { get; set; }

            public History()
            {
                Events = new List<HistoryEvent>();
            }
        }



        public int Get(string[] args)
        {
            string srcFilePath = null;
            string key =null;
            var p = new OptionSet() {
                { "k|key=", "key to retreive [original-id|modified-id]", 
                    v => key = v},
                { "i|input=", "input file path",
                   v => srcFilePath = v }
            };
            ParseParameters(args, p);
            if (srcFilePath == null || key == null)
            {
                ShowHelp(p);
                return 1;
            }

            try
            {
                PdfReader reader = new PdfReader(srcFilePath);
                PdfDocument pdfDoc = new PdfDocument(reader);
                switch (key)
                {
                    case "original-id":
                        Console.WriteLine(BitConverter.ToString(pdfDoc.GetOriginalDocumentId().GetValueBytes()).Replace("-", string.Empty));
                        break;

                    case "modified-id":
                        Console.WriteLine(BitConverter.ToString(pdfDoc.GetModifiedDocumentId().GetValueBytes()).Replace("-", string.Empty));
                        break;

                    default:
                        ShowHelp(p);
                        return 1;

                }
            } 
            catch 
            {
                return 1;
            }
            return 0;

        }

        public int GetIndexation(string[] args)
        {
            string srcFilePath = null;
            var p = new OptionSet() {
                { "i|input=", "input file path",
                   v => srcFilePath = v }
            };
            ParseParameters(args, p);
            if (srcFilePath == null)
            {
                ShowHelp(p);
                return 1;
            }
            XMPSchemaRegistry registry = XMPMetaFactory.GetSchemaRegistry();
            registry.RegisterNamespace(GM_NAMEPACE, "gm");

            PdfReader reader = new PdfReader(srcFilePath);
            PdfDocument pdfDoc = new PdfDocument(reader);
            Document doc = new Document(pdfDoc);

            try
            {
                XMPMeta xmpmeta = XMPMetaFactory.ParseFromBuffer(pdfDoc.GetXmpMetadata(true));

                Infos infos = new Infos
                {
                    IndexationDate = xmpmeta.GetPropertyString(GM_NAMEPACE, "IndexationDate"),
                    IndexationStatus = xmpmeta.GetPropertyString(GM_NAMEPACE, "IndexationStatus"),
                    When = xmpmeta.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:when"),
                    Author = xmpmeta.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:author")
                };
                for (int i = 1; i < xmpmeta.CountArrayItems(GM_NAMEPACE, "DocInfos/gm:Types") + 1; i++)
                {
                    infos.Types.Add(xmpmeta.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:Types[" + (i) + "]"));
                }

                for (int i = 1; i < xmpmeta.CountArrayItems(GM_NAMEPACE, "DocInfos/gm:Tags") + 1; i++)
                {
                    infos.Tags.Add(xmpmeta.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:Tags[" + (i) + "]"));
                }


                for (int i = 1; i < xmpmeta.CountArrayItems(GM_NAMEPACE, "DocInfos/gm:Fields") + 1; i++)
                {
                    Field field = new Field
                    {
                        Id = xmpmeta.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldDesc/gm:id"),
                        Value = xmpmeta.GetPropertyString(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldDesc/gm:value"),
                    };

                    for (int j = 1; j < xmpmeta.CountArrayItems(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldPos") + 1; j++)
                    {
                        Position position = new Position
                        {
                            Pg = xmpmeta.GetPropertyInteger(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldPos[" + (j) + "]/gm:pg"),
                            X1 = xmpmeta.GetPropertyDouble(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldPos[" + (j) + "]/gm:x1"),
                            X2 = xmpmeta.GetPropertyDouble(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldPos[" + (j) + "]/gm:x2"),
                            Y1 = xmpmeta.GetPropertyDouble(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldPos[" + (j) + "]/gm:y1"),
                            Y2 = xmpmeta.GetPropertyDouble(GM_NAMEPACE, "DocInfos/gm:Fields[" + (i) + "]/gm:FieldPos[" + (j) + "]/gm:y2"),
                        };
                        field.Positions.Add(position);
                    }

                    infos.Fields.Add(field);
                }

                Console.WriteLine(JsonConvert.SerializeObject(infos, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : {0}.", ex.Message);
                return 1;
            }

            return 0;

        }

        public int GetHistory(string[] args)
        {
            string srcFilePath = null;
            var p = new OptionSet() {
                { "i|input=", "input file path",
                   v => srcFilePath = v }
            };
            ParseParameters(args, p);
            if (srcFilePath == null)
            {
                ShowHelp(p);
                return 1;
            }

            PdfReader reader = new PdfReader(srcFilePath);
            PdfDocument pdfDoc = new PdfDocument(reader);
            Document doc = new Document(pdfDoc);
          

            try
            {
                XMPMeta xmpmeta = XMPMetaFactory.ParseFromBuffer(pdfDoc.GetXmpMetadata(true));

                History history = new History();

                for (int i = 1; i < xmpmeta.CountArrayItems(XMPConst.NS_XMP_MM, "History") + 1; i++)
                {
                    HistoryEvent hevent=new HistoryEvent();
                    history.Events.Add(hevent);

                    try { hevent.Action = xmpmeta.GetPropertyString(XMPConst.NS_XMP_MM, "History[" + (i) + "]/stEvt:action"); } catch { }
                    try { hevent.Changed = xmpmeta.GetPropertyString(XMPConst.NS_XMP_MM, "History[" + (i) + "]/stEvt:changed"); } catch { }
                    try { hevent.InstanceID = xmpmeta.GetPropertyString(XMPConst.NS_XMP_MM, "History[" + (i) + "]/stEvt:instanceID"); } catch { }
                    try { hevent.SoftwareAgent = xmpmeta.GetPropertyString(XMPConst.NS_XMP_MM, "History[" + (i) + "]/stEvt:softwareAgent"); } catch { }
                    try { hevent.Parameters = xmpmeta.GetPropertyString(XMPConst.NS_XMP_MM, "History[" + (i) + "]/stEvt:parameters"); } catch { }
                    try { hevent.When = xmpmeta.GetPropertyString(XMPConst.NS_XMP_MM, "History[" + (i) + "]/stEvt:when"); } catch { }
                                       
                }

                Console.WriteLine(JsonConvert.SerializeObject(history, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : {0}.", ex.Message);
                return 1;
            }

            return 0;

        }



        public int SetIndexation(string[] args)
        {
            string srcFilePath = null;
            string infosJSON = null;
            var p = new OptionSet() {
                { "i|input=", "file path",
                   v => srcFilePath = v },
                {"d|data=", "infos (json)",
                    v => infosJSON = v}
            };

            ParseParameters(args, p);
            if( srcFilePath == null || infosJSON == null)
            {
                ShowHelp(p);
                return 1;
            }

            try
            {

                Infos docInfos = JsonConvert.DeserializeObject<Infos>(infosJSON);

                string destFilePath = srcFilePath + ".tmp";

                using (var file = new FileStream(srcFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    PdfReader reader = new PdfReader(file);
                    PdfWriter writer = new PdfWriter(destFilePath);

                    PdfDocument pdfDoc = new PdfDocument(reader, writer);
                    byte[] buffer = pdfDoc.GetXmpMetadata(true);

                    XMPSchemaRegistry registry = XMPMetaFactory.GetSchemaRegistry();
                    registry.RegisterNamespace(GM_NAMEPACE, "gm");

                    XMPMeta xmpmeta;
                    try
                    {
                        xmpmeta = XMPMetaFactory.ParseFromBuffer(buffer);
                    }
                    catch (Exception)
                    {
                        xmpmeta = XMPMetaFactory.Create();
                    }

                    bool storeInfos = true;

                    if (docInfos.IndexationStatus != null)
                    {

                        string[] validStatus = { "", "complete", "partial", "excluded" };

                        if (Array.IndexOf(validStatus, docInfos.IndexationStatus) != -1)
                        {
                            xmpmeta.SetProperty(GM_NAMEPACE, "IndexationStatus", docInfos.IndexationStatus);
                            if (docInfos.IndexationStatus == "excluded")
                            {
                                xmpmeta.DeleteProperty(GM_NAMEPACE, "DocInfos");
                                storeInfos = false;
                            }
                        }
                    }

                    if (storeInfos && docInfos.Types.Count > 0)
                    {
                        xmpmeta.SetProperty(GM_NAMEPACE, "IndexationDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

                        xmpmeta.DeleteProperty(GM_NAMEPACE, "DocInfos");

                        foreach (var type in docInfos.Types)
                        {
                            xmpmeta.AppendArrayItem(GM_NAMEPACE, "DocInfos/gm:Types", new PropertyOptions(PropertyOptions.ARRAY), type, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        }

                        /*  foreach (var status in docInfos.Status)
                          {
                              xmpmeta.AppendArrayItem(GM_NAMEPACE, "DocInfos/gm:Status", new PropertyOptions(PropertyOptions.ARRAY), status, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                          }
                          */
                        foreach (var tag in docInfos.Tags)
                        {
                            xmpmeta.AppendArrayItem(GM_NAMEPACE, "DocInfos/gm:Tags", new PropertyOptions(PropertyOptions.ARRAY), tag, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        }


                        foreach (var field in docInfos.Fields)
                        {
                            xmpmeta.AppendArrayItem(GM_NAMEPACE, "DocInfos/gm:Fields", new PropertyOptions(PropertyOptions.ARRAY), null, new PropertyOptions(PropertyOptions.STRUCT));
                            xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldDesc", null, new PropertyOptions(PropertyOptions.STRUCT | PropertyOptions.SEPARATE_NODE));
                            xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldDesc/gm:id", field.Id, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                            xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldDesc/gm:value", field.Value, new PropertyOptions(PropertyOptions.NO_OPTIONS));

                            foreach (var pos in field.Positions)
                            {
                                xmpmeta.AppendArrayItem(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldPos", new PropertyOptions(PropertyOptions.ARRAY), null, new PropertyOptions(PropertyOptions.STRUCT));
                                xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldPos[last()]/gm:pg", pos.Pg, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                                xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldPos[last()]/gm:x1", pos.X1, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                                xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldPos[last()]/gm:x2", pos.X2, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                                xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldPos[last()]/gm:y1", pos.Y1, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                                xmpmeta.SetProperty(GM_NAMEPACE, "DocInfos/gm:Fields[last()]/gm:FieldPos[last()]/gm:y2", pos.Y2, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                            }

                        }

                        xmpmeta.AppendArrayItem(XMPConst.NS_XMP_MM, "History", new PropertyOptions(PropertyOptions.ARRAY_ORDERED), null, new PropertyOptions(PropertyOptions.STRUCT));
                        xmpmeta.SetProperty(XMPConst.NS_XMP_MM, "History[last()]/stEvt:action", "indexed", new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        xmpmeta.SetProperty(XMPConst.NS_XMP_MM, "History[last()]/stEvt:when", DateTime.Now.ToString(), new PropertyOptions(PropertyOptions.NO_OPTIONS));

                        if (Globals.USERNAME.Length > 0)
                        {
                            xmpmeta.SetProperty(XMPConst.NS_XMP_MM, "History[last()]/stEvt:parameters", "by " + Globals.USERNAME, new PropertyOptions(PropertyOptions.NO_OPTIONS));
                        }

                    }

                    pdfDoc.SetXmpMetadata(xmpmeta);

                    pdfDoc.Close();

                    File.Delete(srcFilePath);
                    File.Move(destFilePath, srcFilePath);
                }
            }
            catch (PdfException ex)
            {
                Console.WriteLine("Exception : {0}", ex.Message);
                return 1;
            }
            return 0;

        }
    }



    class DocumentManipulation : Command
    {

        private class CustomSplitter : PdfSplitter
        {
            private int count;
            private readonly string destinationFullBasePath;

            public CustomSplitter(PdfDocument pdfDocument, string destinationDirectory, string baseFilename) : base(pdfDocument)
            {

                if (!Directory.Exists(destinationDirectory))
                {
                    DirectoryInfo di = Directory.CreateDirectory(destinationDirectory);
                }

                destinationFullBasePath = destinationDirectory+"/"+baseFilename;
                count = 1;
            }

            protected override PdfWriter GetNextPdfWriter(PageRange documentPageRange)
            {
                return new PdfWriter(destinationFullBasePath + (count++).ToString().PadLeft(4, '0') + ".pdf");
            }
        }

        public int Split(string[] args)
        {
            string srcFilePath = null;
            int pagesCount = 1;
            string outputDirectory = null;
            var p = new OptionSet() {
                { "i|input=", "input file path",
                   v => srcFilePath = v },
                { "o|output=", "output directory",
                   v => outputDirectory = v },
                { "c|count=", "page count in each split",
                   v => pagesCount = Int32.Parse(v) }
            };

            ParseParameters(args, p);
            if( srcFilePath == null )
            {
                ShowHelp(p);
                return 1;
            }

            if ( outputDirectory == null )
            {
                outputDirectory = System.IO.Path.GetDirectoryName(srcFilePath);
            }

            using (PdfDocument pdfDoc = new PdfDocument(new PdfReader(srcFilePath)))
            {
                try
                {
                    CustomSplitter splitter = new CustomSplitter(pdfDoc, outputDirectory, System.IO.Path.GetFileNameWithoutExtension(srcFilePath) + "-split-");
                    IList<PdfDocument> splittedDocs = splitter.SplitByPageCount(pagesCount);

                    foreach (PdfDocument splittedDoc in splittedDocs)
                    {
                        splittedDoc.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                    return 1;
                }
            }
            return 0;
        }

        public int Extract(string[] args)
        {
            string srcFilePath = null;
            string range = null;
            string outputDirectory = null;

            var p = new OptionSet() {
                { "i|input=", "file path",
                   v => srcFilePath = v },
                { "o|output=", "output directory",
                   v => outputDirectory = v },
                { "n|numbers=", "pages to extract",
                  v => range =v}
            };

            ParseParameters(args, p);
            if (srcFilePath == null || range == null)
            {
                ShowHelp(p);
                return 1;
            }

            try { 

                if (outputDirectory == null)
                {
                    outputDirectory = System.IO.Path.GetDirectoryName(srcFilePath);
                }

                using (PdfDocument pdfDoc = new PdfDocument(new PdfReader(srcFilePath)))
                {
                    CustomSplitter splitter = new CustomSplitter(pdfDoc, outputDirectory, System.IO.Path.GetFileNameWithoutExtension(srcFilePath) + "-extract-");
                    splitter.ExtractPageRange(new PageRange(range)).Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
                return 1;
            }
            return 0;

        }


        public int Merge(string[] args)
        {
            List<string> srcFilesPath = new List<string>();
            string destFilePath = null;
            var p = new OptionSet() {
                { "i|input=", "input file path",
                   v => srcFilesPath.Add(v) },
                { "o|output=", "file path",
                   v => destFilePath = v}
            };

            ParseParameters(args, p);
            if ( srcFilesPath.Count == 0 || destFilePath == null)
            {
                ShowHelp(p);
                return 1;
            }

            try
            {
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
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
                return 1;
            }

            return 0;

        }

    }

    class MainClass: Command
    {

        public static int Main(string[] args)
        {
            string action = "";

            var p = new OptionSet() {
                { "a|action=", "action to perform [get-info|set-indexation|get-indexation|get-history|split|merge|extract]",
                   v => action = v.ToLower() },
                { "u|username=", "user performing the action",
                   v => Globals.USERNAME = v },
                { "v", "increase debug message verbosity",
                   v => { if (v != null) Globals.VERBOSITY++; } },
                { "h|help",  "show this message and exit",
                   v => action = "help" },
            };

            ParseParameters(args, p);
            int ret = 0;
            switch (action)
            {
                case "get-info":
                    ret = new DocInfos().Get(args);
                    break;
                case "set-indexation":
                    ret = new DocInfos().SetIndexation(args);
                    break;
                case "get-indexation":
                    ret = new DocInfos().GetIndexation(args);
                    break;
                case "get-history":
                    ret = new DocInfos().GetHistory(args);
                    break;
                case "split":
                    ret = new DocumentManipulation().Split(args);
                    break;
                case "merge":
                    ret = new DocumentManipulation().Merge(args);
                    break;
                case "extract":
                    ret = new DocumentManipulation().Extract(args);
                    break;
                default:
                    ShowHelp(p);
                    break;

            }
            return ret;
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

                XMPMeta xmpmeta = XMPMetaFactory.ParseFromBuffer(xmpbytes);
                //xmpmeta.set
                //            xmpmeta.InsertArrayItem("xmpMM", "CustTags", 0, "valuetest");

                xmpmeta.AppendArrayItem(XMPConst.NS_XMP_MM, "Tags", new PropertyOptions(PropertyOptions.ARRAY), "valuetest2", new PropertyOptions(PropertyOptions.NO_OPTIONS));

                /*
                PdfDictionary catalog = pdfDoc.GetTrailer();
                PdfDictionary map = catalog.GetAsDictionary(PdfName.Info);
                map.Put(new PdfName("test"), new PdfString("test"));
                map.Put(new PdfName("test1"), new PdfString("test1"));
                */

                //String xmp = "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description xmlns:xmpMM=\"http://ns.adobe.com/xap/1.0/mm/\" rdf:about=\"\"><xmpMM:CustTags><rdf:Seq><rdf:li>elexmpmeta</rdf:li></rdf:Seq></xmpMM:CustTags></rdf:Description></rdf:RDF>";

                //XMPMeta m=XMPMetaFactory.ParseFromString(xmp,new ParseOptions().SetAcceptLatin1(true).SetRequireXMPMeta(false).SetStrictAliasing(false).SetOmitNormalization(true).SetFixControlChars(false));
                pdfDoc.SetXmpMetadata(xmpmeta);

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
