using System.Xml.Linq;

namespace ReportEditor.Models;

/// <summary>
/// Base class for everything that lives in &lt;ReportItems&gt;: Textbox, Image,
/// Rectangle, Line, Tablix, Subreport. Geometry (Top/Left/Width/Height) is
/// common.
/// </summary>
public abstract class ReportItem : RdlElement
{
    protected ReportItem(XElement xml) : base(xml) { }

    public string Name
    {
        get => GetAttr("Name") ?? "";
        set => SetAttr("Name", value);
    }

    public string Kind => Xml.Name.LocalName;

    public SsrsUnit Top { get => GetUnit("Top"); set => SetUnit("Top", value); }
    public SsrsUnit Left { get => GetUnit("Left"); set => SetUnit("Left", value); }
    public SsrsUnit Width { get => GetUnit("Width"); set => SetUnit("Width", value); }
    public SsrsUnit Height { get => GetUnit("Height"); set => SetUnit("Height", value); }

    public int ZIndex { get => GetInt("ZIndex"); set => SetInt("ZIndex", value); }

    public string? VisibilityHidden
    {
        get => Xml.Element(Ns.R + "Visibility")?.Element(Ns.R + "Hidden")?.Value;
        set
        {
            var vis = Xml.Element(Ns.R + "Visibility");
            if (string.IsNullOrEmpty(value))
            {
                vis?.Element(Ns.R + "Hidden")?.Remove();
                if (vis != null && !vis.Elements().Any()) vis.Remove();
                return;
            }
            if (vis == null)
            {
                vis = new XElement(Ns.R + "Visibility");
                Xml.Add(vis);
            }
            var h = vis.Element(Ns.R + "Hidden");
            if (h == null) vis.Add(new XElement(Ns.R + "Hidden", value));
            else h.Value = value;
        }
    }

    public Style Style => Style.For(Xml, "Style");

    public string? ToolTip { get => GetEl("ToolTip"); set => SetEl("ToolTip", value); }

    /// <summary>True if this item lives inside a Tablix cell (its geometry is governed by the grid).</summary>
    public bool IsInsideTablixCell
    {
        get
        {
            var p = Xml.Parent;
            while (p != null)
            {
                if (p.Name.LocalName == "CellContents") return true;
                p = p.Parent;
            }
            return false;
        }
    }

    /// <summary>Factory: build the right subclass from an XElement.</summary>
    public static ReportItem Wrap(XElement el) => el.Name.LocalName switch
    {
        "Textbox"   => new Textbox(el),
        "Image"     => new ImageItem(el),
        "Rectangle" => new RectangleItem(el),
        "Line"      => new LineItem(el),
        "Tablix"    => new TablixItem(el),
        "Subreport" => new Subreport(el),
        _           => new GenericItem(el),
    };
}

public sealed class GenericItem : ReportItem
{
    public GenericItem(XElement xml) : base(xml) { }
}
