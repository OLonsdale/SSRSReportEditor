using System.Xml.Linq;

namespace ReportEditor.Models;

/// <summary>Produces fresh ReportItem XML for the palette / paste / duplicate.</summary>
public static class ItemFactory
{
    public static XElement Textbox(string name, SsrsUnit left, SsrsUnit top,
        SsrsUnit? width = null, SsrsUnit? height = null, string text = "")
    {
        return new XElement(Ns.R + "Textbox",
            new XAttribute("Name", name),
            new XElement(Ns.R + "CanGrow", "true"),
            new XElement(Ns.R + "KeepTogether", "true"),
            new XElement(Ns.R + "Paragraphs",
                new XElement(Ns.R + "Paragraph",
                    new XElement(Ns.R + "TextRuns",
                        new XElement(Ns.R + "TextRun",
                            new XElement(Ns.R + "Value", text),
                            new XElement(Ns.R + "Style"))),
                    new XElement(Ns.R + "Style"))),
            new XElement(Ns.R + "Top", top.ToString()),
            new XElement(Ns.R + "Left", left.ToString()),
            new XElement(Ns.R + "Height", (height ?? SsrsUnit.FromMm(8)).ToString()),
            new XElement(Ns.R + "Width", (width ?? SsrsUnit.FromMm(40)).ToString()),
            new XElement(Ns.R + "Style",
                new XElement(Ns.R + "Border",
                    new XElement(Ns.R + "Style", "None")),
                new XElement(Ns.R + "PaddingLeft", "2pt"),
                new XElement(Ns.R + "PaddingRight", "2pt"),
                new XElement(Ns.R + "PaddingTop", "2pt"),
                new XElement(Ns.R + "PaddingBottom", "2pt")));
    }

    public static XElement Image(string name, SsrsUnit left, SsrsUnit top)
    {
        return new XElement(Ns.R + "Image",
            new XAttribute("Name", name),
            new XElement(Ns.R + "Source", "Embedded"),
            new XElement(Ns.R + "Value", ""),
            new XElement(Ns.R + "Sizing", "FitProportional"),
            new XElement(Ns.R + "Top", top.ToString()),
            new XElement(Ns.R + "Left", left.ToString()),
            new XElement(Ns.R + "Height", SsrsUnit.FromMm(30).ToString()),
            new XElement(Ns.R + "Width",  SsrsUnit.FromMm(40).ToString()),
            new XElement(Ns.R + "Style",
                new XElement(Ns.R + "Border",
                    new XElement(Ns.R + "Style", "None"))));
    }

    public static XElement Rectangle(string name, SsrsUnit left, SsrsUnit top)
    {
        return new XElement(Ns.R + "Rectangle",
            new XAttribute("Name", name),
            new XElement(Ns.R + "ReportItems"),
            new XElement(Ns.R + "KeepTogether", "true"),
            new XElement(Ns.R + "Top", top.ToString()),
            new XElement(Ns.R + "Left", left.ToString()),
            new XElement(Ns.R + "Height", SsrsUnit.FromMm(30).ToString()),
            new XElement(Ns.R + "Width",  SsrsUnit.FromMm(50).ToString()),
            new XElement(Ns.R + "Style",
                new XElement(Ns.R + "Border",
                    new XElement(Ns.R + "Style", "Solid"))));
    }

    public static XElement Line(string name, SsrsUnit left, SsrsUnit top)
    {
        return new XElement(Ns.R + "Line",
            new XAttribute("Name", name),
            new XElement(Ns.R + "Top", top.ToString()),
            new XElement(Ns.R + "Left", left.ToString()),
            new XElement(Ns.R + "Height", SsrsUnit.FromMm(0).ToString()),
            new XElement(Ns.R + "Width",  SsrsUnit.FromMm(60).ToString()),
            new XElement(Ns.R + "Style",
                new XElement(Ns.R + "Border",
                    new XElement(Ns.R + "Color", "Black"),
                    new XElement(Ns.R + "Style", "Solid"),
                    new XElement(Ns.R + "Width", "1pt"))));
    }

    /// <summary>2×2 starter tablix.</summary>
    public static XElement Tablix(string name, SsrsUnit left, SsrsUnit top)
    {
        XElement Cell()
        {
            var tbName = "TB_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return new XElement(Ns.R + "TablixCell",
                new XElement(Ns.R + "CellContents",
                    new XElement(Ns.R + "Textbox",
                        new XAttribute("Name", tbName),
                        new XElement(Ns.R + "CanGrow", "true"),
                        new XElement(Ns.R + "KeepTogether", "true"),
                        new XElement(Ns.R + "Paragraphs",
                            new XElement(Ns.R + "Paragraph",
                                new XElement(Ns.R + "TextRuns",
                                    new XElement(Ns.R + "TextRun",
                                        new XElement(Ns.R + "Value", ""),
                                        new XElement(Ns.R + "Style"))),
                                new XElement(Ns.R + "Style"))),
                        new XElement(Ns.R + "Style",
                            new XElement(Ns.R + "Border",
                                new XElement(Ns.R + "Style", "Solid"))))));
        }
        XElement Col() => new XElement(Ns.R + "TablixColumn",
            new XElement(Ns.R + "Width", SsrsUnit.FromMm(35).ToString()));
        XElement Row() => new XElement(Ns.R + "TablixRow",
            new XElement(Ns.R + "Height", SsrsUnit.FromMm(8).ToString()),
            new XElement(Ns.R + "TablixCells", Cell(), Cell()));
        XElement Member() => new XElement(Ns.R + "TablixMember");

        return new XElement(Ns.R + "Tablix",
            new XAttribute("Name", name),
            new XElement(Ns.R + "TablixBody",
                new XElement(Ns.R + "TablixColumns", Col(), Col()),
                new XElement(Ns.R + "TablixRows", Row(), Row())),
            new XElement(Ns.R + "TablixColumnHierarchy",
                new XElement(Ns.R + "TablixMembers", Member(), Member())),
            new XElement(Ns.R + "TablixRowHierarchy",
                new XElement(Ns.R + "TablixMembers", Member(), Member())),
            new XElement(Ns.R + "Top", top.ToString()),
            new XElement(Ns.R + "Left", left.ToString()),
            new XElement(Ns.R + "Height", SsrsUnit.FromMm(16).ToString()),
            new XElement(Ns.R + "Width",  SsrsUnit.FromMm(70).ToString()),
            new XElement(Ns.R + "Style",
                new XElement(Ns.R + "Border",
                    new XElement(Ns.R + "Style", "None"))));
    }

    public static XElement Subreport(string name, SsrsUnit left, SsrsUnit top)
    {
        return new XElement(Ns.R + "Subreport",
            new XAttribute("Name", name),
            new XElement(Ns.R + "ReportName", ""),
            new XElement(Ns.R + "Top", top.ToString()),
            new XElement(Ns.R + "Left", left.ToString()),
            new XElement(Ns.R + "Height", SsrsUnit.FromMm(30).ToString()),
            new XElement(Ns.R + "Width",  SsrsUnit.FromMm(60).ToString()),
            new XElement(Ns.R + "Style",
                new XElement(Ns.R + "Border",
                    new XElement(Ns.R + "Style", "Dashed"))));
    }

    /// <summary>Deep-clone of an existing item XML for paste/duplicate, with a unique name.</summary>
    public static XElement Duplicate(XElement source, string newName,
        SsrsUnit offsetX, SsrsUnit offsetY)
    {
        var clone = new XElement(source);
        clone.SetAttributeValue("Name", newName);
        // Rename nested textbox names too to avoid collisions.
        foreach (var tb in clone.Descendants(Ns.R + "Textbox"))
        {
            var n = tb.Attribute("Name");
            if (n != null) n.Value = n.Value + "_" + Guid.NewGuid().ToString("N").Substring(0, 4);
        }
        var leftEl = clone.Element(Ns.R + "Left");
        var topEl  = clone.Element(Ns.R + "Top");
        if (leftEl != null)
        {
            var u = SsrsUnit.Parse(leftEl.Value);
            leftEl.Value = u.WithMm(u.Mm + offsetX.Mm).ToString();
        }
        if (topEl != null)
        {
            var u = SsrsUnit.Parse(topEl.Value);
            topEl.Value = u.WithMm(u.Mm + offsetY.Mm).ToString();
        }
        return clone;
    }
}
