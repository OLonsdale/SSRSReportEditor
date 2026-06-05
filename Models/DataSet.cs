using System.Xml.Linq;

namespace ReportEditor.Models;

public sealed class DataSource : RdlElement
{
    public DataSource(XElement xml) : base(xml) { }

    public string Name { get => GetAttr("Name") ?? ""; set => SetAttr("Name", value); }

    public string? DataSourceReference
    {
        get => GetEl("DataSourceReference");
        set => SetEl("DataSourceReference", value);
    }

    public XElement? ConnectionProperties => Xml.Element(Ns.R + "ConnectionProperties");

    public string? ConnectString
    {
        get => ConnectionProperties?.Element(Ns.R + "ConnectString")?.Value;
        set
        {
            var cp = ConnectionProperties;
            if (cp == null)
            {
                cp = new XElement(Ns.R + "ConnectionProperties");
                Xml.AddFirst(cp);
            }
            var cs = cp.Element(Ns.R + "ConnectString");
            if (cs == null) cp.Add(new XElement(Ns.R + "ConnectString", value ?? ""));
            else cs.Value = value ?? "";
        }
    }

    public string? DataProvider
    {
        get => ConnectionProperties?.Element(Ns.R + "DataProvider")?.Value;
    }
}

public sealed class RdlDataSet : RdlElement
{
    public RdlDataSet(XElement xml) : base(xml) { }

    public string Name { get => GetAttr("Name") ?? ""; set => SetAttr("Name", value); }

    public XElement QueryXml
    {
        get
        {
            var q = Xml.Element(Ns.R + "Query");
            if (q == null) { q = new XElement(Ns.R + "Query"); Xml.AddFirst(q); }
            return q;
        }
    }

    public string? DataSourceName
    {
        get => QueryXml.Element(Ns.R + "DataSourceName")?.Value;
        set
        {
            var e = QueryXml.Element(Ns.R + "DataSourceName");
            if (e == null) QueryXml.AddFirst(new XElement(Ns.R + "DataSourceName", value ?? ""));
            else e.Value = value ?? "";
        }
    }

    public string CommandText
    {
        get => QueryXml.Element(Ns.R + "CommandText")?.Value ?? "";
        set
        {
            var e = QueryXml.Element(Ns.R + "CommandText");
            if (e == null) QueryXml.Add(new XElement(Ns.R + "CommandText", value));
            else e.Value = value;
        }
    }

    public string? CommandType
    {
        get => QueryXml.Element(Ns.R + "CommandType")?.Value;
        set
        {
            var e = QueryXml.Element(Ns.R + "CommandType");
            if (string.IsNullOrEmpty(value)) { e?.Remove(); return; }
            if (e == null) QueryXml.Add(new XElement(Ns.R + "CommandType", value));
            else e.Value = value;
        }
    }

    public IEnumerable<DataSetQueryParameter> QueryParameters
    {
        get
        {
            var qps = QueryXml.Element(Ns.R + "QueryParameters");
            if (qps == null) yield break;
            foreach (var p in qps.Elements(Ns.R + "QueryParameter"))
                yield return new DataSetQueryParameter(p);
        }
    }

    public IEnumerable<DataSetField> Fields
    {
        get
        {
            var fs = Xml.Element(Ns.R + "Fields");
            if (fs == null) yield break;
            foreach (var f in fs.Elements(Ns.R + "Field"))
                yield return new DataSetField(f);
        }
    }

    public void ReplaceFields(IEnumerable<(string Name, string? TypeName)> fields)
    {
        var fs = Xml.Element(Ns.R + "Fields");
        if (fs == null)
        {
            fs = new XElement(Ns.R + "Fields");
            // insert after Query
            var q = Xml.Element(Ns.R + "Query");
            if (q != null) q.AddAfterSelf(fs);
            else Xml.AddFirst(fs);
        }
        fs.RemoveNodes();
        foreach (var (n, t) in fields)
        {
            var f = new XElement(Ns.R + "Field",
                new XAttribute("Name", n),
                new XElement(Ns.R + "DataField", n));
            if (!string.IsNullOrEmpty(t))
                f.Add(new XElement(Ns.Rd + "TypeName", t));
            fs.Add(f);
        }
    }
}

public sealed class DataSetField : RdlElement
{
    public DataSetField(XElement xml) : base(xml) { }
    public string Name { get => GetAttr("Name") ?? ""; set => SetAttr("Name", value); }
    public string? DataField { get => GetEl("DataField"); set => SetEl("DataField", value); }

    public string? TypeName
    {
        get => Xml.Element(Ns.Rd + "TypeName")?.Value;
        set
        {
            var e = Xml.Element(Ns.Rd + "TypeName");
            if (string.IsNullOrEmpty(value)) { e?.Remove(); return; }
            if (e == null) Xml.Add(new XElement(Ns.Rd + "TypeName", value));
            else e.Value = value;
        }
    }
}

public sealed class DataSetQueryParameter : RdlElement
{
    public DataSetQueryParameter(XElement xml) : base(xml) { }
    public string Name { get => GetAttr("Name") ?? ""; set => SetAttr("Name", value); }

    public string? Value
    {
        get => GetEl("Value");
        set => SetEl("Value", value);
    }
}

public sealed class RdlReportParameter : RdlElement
{
    public RdlReportParameter(XElement xml) : base(xml) { }
    public string Name { get => GetAttr("Name") ?? ""; set => SetAttr("Name", value); }
    public string? DataType { get => GetEl("DataType"); set => SetEl("DataType", value); }
    public string? Prompt { get => GetEl("Prompt"); set => SetEl("Prompt", value); }
    public bool Nullable { get => GetBool("Nullable"); set => SetBool("Nullable", value); }
    public bool AllowBlank { get => GetBool("AllowBlank"); set => SetBool("AllowBlank", value); }
    public bool MultiValue { get => GetBool("MultiValue"); set => SetBool("MultiValue", value); }

    public string? DefaultValue
    {
        get => Xml.Element(Ns.R + "DefaultValue")?
                  .Element(Ns.R + "Values")?
                  .Element(Ns.R + "Value")?.Value;
        set
        {
            var dv = Xml.Element(Ns.R + "DefaultValue");
            if (string.IsNullOrEmpty(value))
            {
                dv?.Remove();
                return;
            }
            if (dv == null)
            {
                dv = new XElement(Ns.R + "DefaultValue",
                    new XElement(Ns.R + "Values",
                        new XElement(Ns.R + "Value", value)));
                Xml.Add(dv);
                return;
            }
            var values = dv.Element(Ns.R + "Values");
            if (values == null) { dv.Add(new XElement(Ns.R + "Values", new XElement(Ns.R + "Value", value))); return; }
            var v = values.Element(Ns.R + "Value");
            if (v == null) values.Add(new XElement(Ns.R + "Value", value));
            else v.Value = value;
        }
    }
}
