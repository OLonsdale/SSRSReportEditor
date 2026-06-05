using System.Xml.Linq;

namespace ReportEditor.Models;

/// <summary>
/// Full Tablix wrapper. SSRS Tablix has TablixBody (the cell grid),
/// TablixColumnHierarchy + TablixRowHierarchy (groupings, mostly orthogonal
/// to layout), and a DataSetName binding.
/// </summary>
public partial class TablixItem : ReportItem
{
    public TablixItem(XElement xml) : base(xml) { }

    public string? DataSetName { get => GetEl("DataSetName"); set => SetEl("DataSetName", value); }

    public XElement BodyXml
    {
        get
        {
            var b = Xml.Element(Ns.R + "TablixBody");
            if (b == null) { b = new XElement(Ns.R + "TablixBody"); Xml.AddFirst(b); }
            return b;
        }
    }

    public IList<TablixColumn> Columns
    {
        get
        {
            var cols = BodyXml.Element(Ns.R + "TablixColumns");
            if (cols == null) return Array.Empty<TablixColumn>();
            return cols.Elements(Ns.R + "TablixColumn").Select(e => new TablixColumn(e)).ToList();
        }
    }

    public IList<TablixRow> Rows
    {
        get
        {
            var rows = BodyXml.Element(Ns.R + "TablixRows");
            if (rows == null) return Array.Empty<TablixRow>();
            return rows.Elements(Ns.R + "TablixRow").Select(e => new TablixRow(e)).ToList();
        }
    }

    /// <summary>Width computed by summing column widths.</summary>
    public double TotalColMm => Columns.Sum(c => c.Width.Mm);
    public double TotalRowMm => Rows.Sum(r => r.Height.Mm);

    public void AddColumn(SsrsUnit width)
    {
        var cols = BodyXml.Element(Ns.R + "TablixColumns");
        if (cols == null)
        {
            cols = new XElement(Ns.R + "TablixColumns");
            BodyXml.AddFirst(cols);
        }
        cols.Add(new XElement(Ns.R + "TablixColumn",
            new XElement(Ns.R + "Width", width.ToString())));

        // Add a cell to every existing row.
        foreach (var row in BodyXml.Element(Ns.R + "TablixRows")?
                                .Elements(Ns.R + "TablixRow") ?? Enumerable.Empty<XElement>())
        {
            var cells = row.Element(Ns.R + "TablixCells");
            if (cells == null) { cells = new XElement(Ns.R + "TablixCells"); row.Add(cells); }
            cells.Add(EmptyCell());
        }
        // Also add a TablixMember in the column hierarchy.
        AddHierarchyMember("TablixColumnHierarchy");
    }

    public void AddRow(SsrsUnit height)
    {
        var rows = BodyXml.Element(Ns.R + "TablixRows");
        if (rows == null) { rows = new XElement(Ns.R + "TablixRows"); BodyXml.Add(rows); }
        var nCols = Columns.Count;
        var row = new XElement(Ns.R + "TablixRow",
            new XElement(Ns.R + "Height", height.ToString()),
            new XElement(Ns.R + "TablixCells",
                Enumerable.Range(0, nCols).Select(_ => EmptyCell())));
        rows.Add(row);
        AddHierarchyMember("TablixRowHierarchy");
    }

    public void MoveColumn(int from, int to)
    {
        if (from == to || from < 0 || to < 0) return;
        var colsHost = BodyXml.Element(Ns.R + "TablixColumns");
        var colList = colsHost?.Elements(Ns.R + "TablixColumn").ToList();
        if (colsHost == null || colList == null || from >= colList.Count || to >= colList.Count) return;

        // Move the TablixColumn itself.
        var col = colList[from];
        col.Remove();
        var remCols = colsHost.Elements(Ns.R + "TablixColumn").ToList();
        if (to >= remCols.Count) colsHost.Add(col);
        else remCols[to].AddBeforeSelf(col);

        // Move the cell at index `from` in each row to index `to`.
        foreach (var rowXml in BodyXml.Element(Ns.R + "TablixRows")?
                                     .Elements(Ns.R + "TablixRow") ?? Enumerable.Empty<XElement>())
        {
            var cellsHost = rowXml.Element(Ns.R + "TablixCells");
            var cells = cellsHost?.Elements(Ns.R + "TablixCell").ToList();
            if (cellsHost == null || cells == null || from >= cells.Count) continue;
            var c = cells[from];
            c.Remove();
            var rem = cellsHost.Elements(Ns.R + "TablixCell").ToList();
            if (to >= rem.Count) cellsHost.Add(c);
            else rem[to].AddBeforeSelf(c);
        }

        MoveHierarchyMember("TablixColumnHierarchy", from, to);
    }

    public void MoveRow(int from, int to)
    {
        if (from == to || from < 0 || to < 0) return;
        var rowsHost = BodyXml.Element(Ns.R + "TablixRows");
        var rowList = rowsHost?.Elements(Ns.R + "TablixRow").ToList();
        if (rowsHost == null || rowList == null || from >= rowList.Count || to >= rowList.Count) return;
        var row = rowList[from];
        row.Remove();
        var remRows = rowsHost.Elements(Ns.R + "TablixRow").ToList();
        if (to >= remRows.Count) rowsHost.Add(row);
        else remRows[to].AddBeforeSelf(row);
        MoveHierarchyMember("TablixRowHierarchy", from, to);
    }

    private void MoveHierarchyMember(string hierarchyName, int from, int to)
    {
        var membersHost = Xml.Element(Ns.R + hierarchyName)?
                             .Element(Ns.R + "TablixMembers");
        var list = membersHost?.Elements(Ns.R + "TablixMember").ToList();
        if (membersHost == null || list == null || from >= list.Count) return;
        var m = list[from];
        m.Remove();
        var rem = membersHost.Elements(Ns.R + "TablixMember").ToList();
        if (to >= rem.Count) membersHost.Add(m);
        else rem[to].AddBeforeSelf(m);
    }

    public XElement? GetRowXml(int index)
    {
        var rows = BodyXml.Element(Ns.R + "TablixRows")?.Elements(Ns.R + "TablixRow").ToList();
        return rows != null && index >= 0 && index < rows.Count ? rows[index] : null;
    }

    public XElement? GetColumnXml(int index)
    {
        var cols = BodyXml.Element(Ns.R + "TablixColumns")?.Elements(Ns.R + "TablixColumn").ToList();
        return cols != null && index >= 0 && index < cols.Count ? cols[index] : null;
    }

    public void RemoveColumn(int index)
    {
        var cols = BodyXml.Element(Ns.R + "TablixColumns");
        var colsList = cols?.Elements(Ns.R + "TablixColumn").ToList();
        if (cols == null || colsList == null || index < 0 || index >= colsList.Count) return;
        colsList[index].Remove();
        foreach (var row in BodyXml.Element(Ns.R + "TablixRows")?
                                .Elements(Ns.R + "TablixRow") ?? Enumerable.Empty<XElement>())
        {
            var cellsList = row.Element(Ns.R + "TablixCells")?
                                .Elements(Ns.R + "TablixCell").ToList();
            if (cellsList != null && index < cellsList.Count) cellsList[index].Remove();
        }
        RemoveHierarchyMember("TablixColumnHierarchy", index);
    }

    public void RemoveRow(int index)
    {
        var rows = BodyXml.Element(Ns.R + "TablixRows")?
                          .Elements(Ns.R + "TablixRow").ToList();
        if (rows == null || index < 0 || index >= rows.Count) return;
        rows[index].Remove();
        RemoveHierarchyMember("TablixRowHierarchy", index);
    }

    private void AddHierarchyMember(string hierarchyName)
    {
        var h = Xml.Element(Ns.R + hierarchyName);
        if (h == null)
        {
            h = new XElement(Ns.R + hierarchyName,
                new XElement(Ns.R + "TablixMembers"));
            Xml.Add(h);
        }
        var members = h.Element(Ns.R + "TablixMembers");
        if (members == null) { members = new XElement(Ns.R + "TablixMembers"); h.Add(members); }
        members.Add(new XElement(Ns.R + "TablixMember"));
    }

    private void RemoveHierarchyMember(string hierarchyName, int index)
    {
        var members = Xml.Element(Ns.R + hierarchyName)?
                         .Element(Ns.R + "TablixMembers")?
                         .Elements(Ns.R + "TablixMember").ToList();
        if (members != null && index < members.Count) members[index].Remove();
    }

    private static XElement EmptyCell()
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
}

public sealed class TablixColumn : RdlElement
{
    public TablixColumn(XElement xml) : base(xml) { }
    public SsrsUnit Width
    {
        get => GetUnit("Width");
        set => SetUnit("Width", value);
    }
}

public sealed class TablixRow : RdlElement
{
    public TablixRow(XElement xml) : base(xml) { }
    public SsrsUnit Height
    {
        get => GetUnit("Height");
        set => SetUnit("Height", value);
    }

    public IList<TablixCell> Cells
    {
        get
        {
            var cs = Xml.Element(Ns.R + "TablixCells");
            if (cs == null) return Array.Empty<TablixCell>();
            return cs.Elements(Ns.R + "TablixCell").Select(e => new TablixCell(e)).ToList();
        }
    }
}

public sealed class TablixCell : RdlElement
{
    public TablixCell(XElement xml) : base(xml) { }

    public XElement? CellContents => Xml.Element(Ns.R + "CellContents");

    /// <summary>The single Textbox commonly inside a cell, if any.</summary>
    public Textbox? Textbox
    {
        get
        {
            var tb = CellContents?.Element(Ns.R + "Textbox");
            return tb == null ? null : new Textbox(tb);
        }
    }

    public int? ColSpan
    {
        get => int.TryParse(CellContents?.Element(Ns.R + "ColSpan")?.Value, out var i) ? i : null;
    }

    public int? RowSpan
    {
        get => int.TryParse(CellContents?.Element(Ns.R + "RowSpan")?.Value, out var i) ? i : null;
    }
}

/// <summary>Wraps a TablixMember (a row or column header in the hierarchy).</summary>
public sealed class TablixMember : RdlElement
{
    public TablixMember(XElement xml) : base(xml) { }

    public XElement? GroupXml => Xml.Element(Ns.R + "Group");

    public string? GroupName
    {
        get => GroupXml?.Attribute("Name")?.Value;
        set
        {
            var g = GroupXml ?? EnsureGroup();
            if (string.IsNullOrEmpty(value)) g.Attribute("Name")?.Remove();
            else g.SetAttributeValue("Name", value);
        }
    }

    /// <summary>The first &lt;Group&gt;/&lt;GroupExpressions&gt;/&lt;GroupExpression&gt;.</summary>
    public string? GroupExpression
    {
        get => GroupXml?.Element(Ns.R + "GroupExpressions")?
                        .Element(Ns.R + "GroupExpression")?.Value;
        set
        {
            var g = GroupXml ?? EnsureGroup();
            var ges = g.Element(Ns.R + "GroupExpressions");
            if (ges == null) { ges = new XElement(Ns.R + "GroupExpressions"); g.Add(ges); }
            var ge = ges.Element(Ns.R + "GroupExpression");
            if (string.IsNullOrEmpty(value))
            {
                ge?.Remove();
                if (!ges.Elements().Any()) ges.Remove();
                return;
            }
            if (ge == null) ges.Add(new XElement(Ns.R + "GroupExpression", value));
            else ge.Value = value;
        }
    }

    private XElement EnsureGroup()
    {
        var g = new XElement(Ns.R + "Group");
        Xml.AddFirst(g);
        return g;
    }

    public void RemoveGroup() => GroupXml?.Remove();
}

public partial class TablixItem
{
    public IList<TablixMember> RowMembers
    {
        get
        {
            var ms = Xml.Element(Ns.R + "TablixRowHierarchy")?
                        .Element(Ns.R + "TablixMembers");
            if (ms == null) return Array.Empty<TablixMember>();
            return ms.Elements(Ns.R + "TablixMember").Select(e => new TablixMember(e)).ToList();
        }
    }

    public IList<TablixMember> ColumnMembers
    {
        get
        {
            var ms = Xml.Element(Ns.R + "TablixColumnHierarchy")?
                        .Element(Ns.R + "TablixMembers");
            if (ms == null) return Array.Empty<TablixMember>();
            return ms.Elements(Ns.R + "TablixMember").Select(e => new TablixMember(e)).ToList();
        }
    }
}
