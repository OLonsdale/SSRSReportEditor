using System.Xml.Linq;

namespace ReportEditor.Models;

public sealed class Report : RdlElement
{
    public XDocument Document { get; }

    public Report(XDocument doc) : base(doc.Root ?? throw new InvalidOperationException("Empty doc"))
    {
        Document = doc;
    }

    public string? Description { get => GetEl("Description"); set => SetEl("Description", value); }

    public IEnumerable<DataSource> DataSources
    {
        get
        {
            var ds = Xml.Element(Ns.R + "DataSources");
            if (ds == null) yield break;
            foreach (var d in ds.Elements(Ns.R + "DataSource"))
                yield return new DataSource(d);
        }
    }

    public IEnumerable<RdlDataSet> DataSets
    {
        get
        {
            var ds = Xml.Element(Ns.R + "DataSets");
            if (ds == null) yield break;
            foreach (var d in ds.Elements(Ns.R + "DataSet"))
                yield return new RdlDataSet(d);
        }
    }

    public IEnumerable<RdlReportParameter> Parameters
    {
        get
        {
            var ps = Xml.Element(Ns.R + "ReportParameters");
            if (ps == null) yield break;
            foreach (var p in ps.Elements(Ns.R + "ReportParameter"))
                yield return new RdlReportParameter(p);
        }
    }

    /// <summary>The first ReportSection (most RDLs only have one).</summary>
    public XElement? FirstSection
        => Xml.Element(Ns.R + "ReportSections")?.Element(Ns.R + "ReportSection");

    public XElement? Body => FirstSection?.Element(Ns.R + "Body");
    public XElement? PageHeader => PageElement?.Element(Ns.R + "PageHeader");
    public XElement? PageFooter => PageElement?.Element(Ns.R + "PageFooter");

    public XElement EnsurePageHeader(SsrsUnit? height = null)
    {
        var p = PageElement ?? throw new InvalidOperationException("Report has no Page element");
        var h = p.Element(Ns.R + "PageHeader");
        if (h == null)
        {
            h = new XElement(Ns.R + "PageHeader",
                new XElement(Ns.R + "Height", (height ?? SsrsUnit.FromMm(20)).ToString()),
                new XElement(Ns.R + "PrintOnFirstPage", "true"),
                new XElement(Ns.R + "PrintOnLastPage", "true"),
                new XElement(Ns.R + "ReportItems"),
                new XElement(Ns.R + "Style"));
            // PageHeader must be the first child of Page.
            p.AddFirst(h);
        }
        return h;
    }

    public XElement EnsurePageFooter(SsrsUnit? height = null)
    {
        var p = PageElement ?? throw new InvalidOperationException("Report has no Page element");
        var f = p.Element(Ns.R + "PageFooter");
        if (f == null)
        {
            f = new XElement(Ns.R + "PageFooter",
                new XElement(Ns.R + "Height", (height ?? SsrsUnit.FromMm(15)).ToString()),
                new XElement(Ns.R + "PrintOnFirstPage", "true"),
                new XElement(Ns.R + "PrintOnLastPage", "true"),
                new XElement(Ns.R + "ReportItems"),
                new XElement(Ns.R + "Style"));
            // Inserted after PageHeader if present, else first.
            var ph = p.Element(Ns.R + "PageHeader");
            if (ph != null) ph.AddAfterSelf(f); else p.AddFirst(f);
        }
        return f;
    }

    public SsrsUnit PageHeaderHeight
    {
        get => SsrsUnit.Parse(PageHeader?.Element(Ns.R + "Height")?.Value);
        set
        {
            var h = PageHeader; if (h == null) return;
            var el = h.Element(Ns.R + "Height");
            if (el == null) h.AddFirst(new XElement(Ns.R + "Height", value.ToString()));
            else el.Value = value.ToString();
        }
    }

    public SsrsUnit PageFooterHeight
    {
        get => SsrsUnit.Parse(PageFooter?.Element(Ns.R + "Height")?.Value);
        set
        {
            var h = PageFooter; if (h == null) return;
            var el = h.Element(Ns.R + "Height");
            if (el == null) h.AddFirst(new XElement(Ns.R + "Height", value.ToString()));
            else el.Value = value.ToString();
        }
    }

    public IEnumerable<ReportItem> ItemsIn(XElement container)
    {
        var ri = container.Element(Ns.R + "ReportItems");
        if (ri == null) yield break;
        foreach (var c in ri.Elements())
            yield return ReportItem.Wrap(c);
    }

    public ReportItem AddItemTo(XElement container, XElement itemXml)
    {
        var ri = container.Element(Ns.R + "ReportItems");
        if (ri == null) { ri = new XElement(Ns.R + "ReportItems"); container.AddFirst(ri); }
        ri.Add(itemXml);
        return ReportItem.Wrap(itemXml);
    }

    // --- code block & language ---
    public string? Code
    {
        get => GetEl("Code");
        set => SetEl("Code", value);
    }

    public string? Language
    {
        get => GetEl("Language");
        set => SetEl("Language", value);
    }

    public bool ConsumeContainerWhitespace
    {
        get => GetBool("ConsumeContainerWhitespace");
        set => SetBool("ConsumeContainerWhitespace", value);
    }

    // RDL 2016 stores Body Width on the ReportSection, not inside <Body>.
    // Older RDLs put it inside Body. Support both for read; prefer the
    // existing location when writing.
    public SsrsUnit BodyWidth
    {
        get
        {
            var bw = Body?.Element(Ns.R + "Width")?.Value;
            if (!string.IsNullOrEmpty(bw)) return SsrsUnit.Parse(bw);
            var sw = FirstSection?.Element(Ns.R + "Width")?.Value;
            if (!string.IsNullOrEmpty(sw)) return SsrsUnit.Parse(sw);
            // Last-ditch fallback: page width minus margins.
            var p = PageElement;
            if (p != null)
            {
                var pw = SsrsUnit.Parse(p.Element(Ns.R + "PageWidth")?.Value).Mm;
                var ml = SsrsUnit.Parse(p.Element(Ns.R + "LeftMargin")?.Value).Mm;
                var mr = SsrsUnit.Parse(p.Element(Ns.R + "RightMargin")?.Value).Mm;
                if (pw > 0) return SsrsUnit.FromMm(Math.Max(10, pw - ml - mr));
            }
            return SsrsUnit.FromMm(160);
        }
        set
        {
            var b = Body;
            if (b != null && b.Element(Ns.R + "Width") != null)
            {
                b.Element(Ns.R + "Width")!.Value = value.ToString();
                return;
            }
            var s = FirstSection;
            if (s != null)
            {
                var w = s.Element(Ns.R + "Width");
                if (w == null) s.Add(new XElement(Ns.R + "Width", value.ToString()));
                else w.Value = value.ToString();
            }
        }
    }

    public SsrsUnit BodyHeight
    {
        get
        {
            var bh = Body?.Element(Ns.R + "Height")?.Value;
            if (!string.IsNullOrEmpty(bh)) return SsrsUnit.Parse(bh);
            // Fallback: tall enough to host items.
            return SsrsUnit.FromMm(297);
        }
        set
        {
            var b = Body;
            if (b == null) return;
            var h = b.Element(Ns.R + "Height");
            if (h == null) b.Add(new XElement(Ns.R + "Height", value.ToString()));
            else h.Value = value.ToString();
        }
    }

    public IEnumerable<ReportItem> BodyItems
    {
        get
        {
            var ri = Body?.Element(Ns.R + "ReportItems");
            if (ri == null) yield break;
            foreach (var c in ri.Elements())
                yield return ReportItem.Wrap(c);
        }
    }

    public IEnumerable<EmbeddedImage> EmbeddedImages
    {
        get
        {
            var ei = Xml.Element(Ns.R + "EmbeddedImages");
            if (ei == null) yield break;
            foreach (var e in ei.Elements(Ns.R + "EmbeddedImage"))
                yield return new EmbeddedImage(e);
        }
    }

    public EmbeddedImage AddEmbeddedImage(string name, string mime, string base64)
    {
        var ei = Xml.Element(Ns.R + "EmbeddedImages");
        if (ei == null)
        {
            ei = new XElement(Ns.R + "EmbeddedImages");
            // Insert after DataSets if present, else early.
            var ds = Xml.Element(Ns.R + "DataSets");
            if (ds != null) ds.AddAfterSelf(ei);
            else Xml.AddFirst(ei);
        }
        var el = new XElement(Ns.R + "EmbeddedImage",
            new XAttribute("Name", name),
            new XElement(Ns.R + "MIMEType", mime),
            new XElement(Ns.R + "ImageData", base64));
        ei.Add(el);
        return new EmbeddedImage(el);
    }

    public void RemoveEmbeddedImage(string name)
    {
        var ei = Xml.Element(Ns.R + "EmbeddedImages");
        ei?.Elements(Ns.R + "EmbeddedImage")
           .FirstOrDefault(e => e.Attribute("Name")?.Value == name)?.Remove();
    }

    /// <summary>Add a new ReportItem XElement to Body/ReportItems and return its wrapper.</summary>
    public ReportItem AddBodyItem(XElement itemXml)
    {
        var b = Body ?? throw new InvalidOperationException("Report has no Body");
        var ri = b.Element(Ns.R + "ReportItems");
        if (ri == null) { ri = new XElement(Ns.R + "ReportItems"); b.AddFirst(ri); }
        ri.Add(itemXml);
        return ReportItem.Wrap(itemXml);
    }

    public DataSource AddDataSource(string name)
    {
        var dsHost = Xml.Element(Ns.R + "DataSources");
        if (dsHost == null)
        {
            dsHost = new XElement(Ns.R + "DataSources");
            // Insert near top, after Description if present.
            var afterEl = Xml.Element(Ns.R + "Description") ?? Xml.Element(Ns.R + "AutoRefresh");
            if (afterEl != null) afterEl.AddAfterSelf(dsHost);
            else Xml.AddFirst(dsHost);
        }
        var el = new XElement(Ns.R + "DataSource",
            new XAttribute("Name", name),
            new XElement(Ns.R + "ConnectionProperties",
                new XElement(Ns.R + "DataProvider", "SQL"),
                new XElement(Ns.R + "ConnectString", "")));
        dsHost.Add(el);
        return new DataSource(el);
    }

    public RdlDataSet AddDataSet(string name, string? dataSourceName = null)
    {
        var dsHost = Xml.Element(Ns.R + "DataSets");
        if (dsHost == null)
        {
            dsHost = new XElement(Ns.R + "DataSets");
            var afterEl = Xml.Element(Ns.R + "DataSources");
            if (afterEl != null) afterEl.AddAfterSelf(dsHost);
            else Xml.AddFirst(dsHost);
        }
        var el = new XElement(Ns.R + "DataSet",
            new XAttribute("Name", name),
            new XElement(Ns.R + "Query",
                new XElement(Ns.R + "DataSourceName", dataSourceName ?? ""),
                new XElement(Ns.R + "CommandText", "")));
        dsHost.Add(el);
        return new RdlDataSet(el);
    }

    public RdlReportParameter AddParameter(string name, string dataType = "String")
    {
        var ph = Xml.Element(Ns.R + "ReportParameters");
        if (ph == null)
        {
            ph = new XElement(Ns.R + "ReportParameters");
            // Insert before ReportSections.
            var sections = Xml.Element(Ns.R + "ReportSections");
            if (sections != null) sections.AddBeforeSelf(ph);
            else Xml.Add(ph);
        }
        var el = new XElement(Ns.R + "ReportParameter",
            new XAttribute("Name", name),
            new XElement(Ns.R + "DataType", dataType),
            new XElement(Ns.R + "Nullable", "true"),
            new XElement(Ns.R + "AllowBlank", "true"),
            new XElement(Ns.R + "Prompt", name));
        ph.Add(el);
        return new RdlReportParameter(el);
    }

    // --- page layout ---
    public XElement? PageElement => FirstSection?.Element(Ns.R + "Page");

    public SsrsUnit PageWidth
    {
        get => SsrsUnit.Parse(PageElement?.Element(Ns.R + "PageWidth")?.Value);
        set { var p = PageElement; if (p == null) return; SetUnitInline(p, "PageWidth", value); }
    }

    public SsrsUnit PageHeight
    {
        get => SsrsUnit.Parse(PageElement?.Element(Ns.R + "PageHeight")?.Value);
        set { var p = PageElement; if (p == null) return; SetUnitInline(p, "PageHeight", value); }
    }

    public SsrsUnit MarginLeft   => SsrsUnit.Parse(PageElement?.Element(Ns.R + "LeftMargin")?.Value);
    public SsrsUnit MarginRight  => SsrsUnit.Parse(PageElement?.Element(Ns.R + "RightMargin")?.Value);
    public SsrsUnit MarginTop    => SsrsUnit.Parse(PageElement?.Element(Ns.R + "TopMargin")?.Value);
    public SsrsUnit MarginBottom => SsrsUnit.Parse(PageElement?.Element(Ns.R + "BottomMargin")?.Value);

    private static void SetUnitInline(XElement parent, string name, SsrsUnit value)
    {
        var n = Ns.R + name;
        var e = parent.Element(n);
        if (e == null) parent.Add(new XElement(n, value.ToString()));
        else e.Value = value.ToString();
    }

    /// <summary>Body &lt;Style&gt;/&lt;BackgroundImage&gt; (a watermark in most reports).</summary>
    public (string? Value, string Source, string Repeat)? BodyBackgroundImage
    {
        get
        {
            var bg = Body?.Element(Ns.R + "Style")?.Element(Ns.R + "BackgroundImage");
            if (bg == null) return null;
            return (
                bg.Element(Ns.R + "Value")?.Value,
                bg.Element(Ns.R + "Source")?.Value ?? "Embedded",
                bg.Element(Ns.R + "BackgroundRepeat")?.Value ?? "NoRepeat"
            );
        }
    }

    // --- loading ---
    public static Report Load(Stream stream)
    {
        var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        return new Report(doc);
    }

    public static Report Load(string xml)
    {
        var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        return new Report(doc);
    }

    public string SerializeToString()
    {
        using var sw = new StringWriter();
        Document.Save(sw, SaveOptions.None);
        return sw.ToString();
    }
}
