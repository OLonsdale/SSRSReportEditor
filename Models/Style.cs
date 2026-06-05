using System.Xml.Linq;

namespace ReportEditor.Models;

/// <summary>
/// Wraps an RDL &lt;Style&gt; element. Creates it lazily on first write.
/// </summary>
public sealed class Style : RdlElement
{
    private readonly XElement _parent;
    private readonly string _elementName;

    private Style(XElement parent, XElement xml, string name) : base(xml)
    {
        _parent = parent;
        _elementName = name;
    }

    public static Style For(XElement parent, string name = "Style")
    {
        var n = Ns.R + name;
        var e = parent.Element(n);
        if (e == null)
        {
            e = new XElement(n);
            // styles tend to live at the end of the item, that's fine.
            parent.Add(e);
        }
        return new Style(parent, e, name);
    }

    public string? FontFamily { get => GetEl("FontFamily"); set => SetEl("FontFamily", value); }
    public string? FontSize { get => GetEl("FontSize"); set => SetEl("FontSize", value); }
    public string? FontWeight { get => GetEl("FontWeight"); set => SetEl("FontWeight", value); }
    public string? FontStyle { get => GetEl("FontStyle"); set => SetEl("FontStyle", value); }
    public string? TextAlign { get => GetEl("TextAlign"); set => SetEl("TextAlign", value); }
    public string? VerticalAlign { get => GetEl("VerticalAlign"); set => SetEl("VerticalAlign", value); }
    public string? Color { get => GetEl("Color"); set => SetEl("Color", value); }
    public string? BackgroundColor { get => GetEl("BackgroundColor"); set => SetEl("BackgroundColor", value); }
    public string? TextDecoration { get => GetEl("TextDecoration"); set => SetEl("TextDecoration", value); }
    public string? PaddingLeft { get => GetEl("PaddingLeft"); set => SetEl("PaddingLeft", value); }
    public string? PaddingRight { get => GetEl("PaddingRight"); set => SetEl("PaddingRight", value); }
    public string? PaddingTop { get => GetEl("PaddingTop"); set => SetEl("PaddingTop", value); }
    public string? PaddingBottom { get => GetEl("PaddingBottom"); set => SetEl("PaddingBottom", value); }

    public Border Border => Border.For(Xml, "Border");
}

public sealed class Border : RdlElement
{
    private Border(XElement xml) : base(xml) { }

    public static Border For(XElement parent, string name)
    {
        var n = Ns.R + name;
        var e = parent.Element(n);
        if (e == null)
        {
            e = new XElement(n);
            parent.Add(e);
        }
        return new Border(e);
    }

    public string? Style { get => GetEl("Style"); set => SetEl("Style", value); }
    public string? Color { get => GetEl("Color"); set => SetEl("Color", value); }
    public string? Width { get => GetEl("Width"); set => SetEl("Width", value); }
}
