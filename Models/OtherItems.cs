using System.Xml.Linq;

namespace ReportEditor.Models;

public sealed class ImageItem : ReportItem
{
    public ImageItem(XElement xml) : base(xml) { }

    /// <summary>Embedded | External | Database</summary>
    public string? Source { get => GetEl("Source"); set => SetEl("Source", value); }

    /// <summary>For embedded, the name of an EmbeddedImage.</summary>
    public string? Value { get => GetEl("Value"); set => SetEl("Value", value); }

    /// <summary>AutoSize | Fit | FitProportional | Clip</summary>
    public string? Sizing { get => GetEl("Sizing"); set => SetEl("Sizing", value); }
}

public sealed class RectangleItem : ReportItem
{
    public RectangleItem(XElement xml) : base(xml) { }

    public IEnumerable<ReportItem> Children
    {
        get
        {
            var ri = Xml.Element(Ns.R + "ReportItems");
            if (ri == null) yield break;
            foreach (var c in ri.Elements())
                yield return ReportItem.Wrap(c);
        }
    }
}

public sealed class LineItem : ReportItem
{
    public LineItem(XElement xml) : base(xml) { }
}

// TablixItem is defined in Tablix.cs

public sealed class Subreport : ReportItem
{
    public Subreport(XElement xml) : base(xml) { }

    public string? ReportName { get => GetEl("ReportName"); set => SetEl("ReportName", value); }

    public bool MergeTransactions { get => GetBool("MergeTransactions"); set => SetBool("MergeTransactions", value); }

    /// <summary>Map of parameter name → value expression for the subreport.</summary>
    public IEnumerable<(string Name, string? Value)> Parameters
    {
        get
        {
            var ps = Xml.Element(Ns.R + "Parameters");
            if (ps == null) yield break;
            foreach (var p in ps.Elements(Ns.R + "Parameter"))
                yield return (p.Attribute("Name")?.Value ?? "",
                              p.Element(Ns.R + "Value")?.Value);
        }
    }

    public void SetParameter(string name, string? value)
    {
        var ps = Xml.Element(Ns.R + "Parameters");
        if (ps == null) { ps = new XElement(Ns.R + "Parameters"); Xml.Add(ps); }
        var p = ps.Elements(Ns.R + "Parameter")
                  .FirstOrDefault(x => x.Attribute("Name")?.Value == name);
        if (p == null)
        {
            p = new XElement(Ns.R + "Parameter", new XAttribute("Name", name),
                new XElement(Ns.R + "Value", value ?? ""));
            ps.Add(p);
        }
        else
        {
            var v = p.Element(Ns.R + "Value");
            if (v == null) p.Add(new XElement(Ns.R + "Value", value ?? ""));
            else v.Value = value ?? "";
        }
    }

    public void RemoveParameter(string name)
    {
        Xml.Element(Ns.R + "Parameters")?
           .Elements(Ns.R + "Parameter")
           .FirstOrDefault(x => x.Attribute("Name")?.Value == name)?.Remove();
    }
}
