using System.Globalization;
using System.Xml.Linq;

namespace ReportEditor.Models;

/// <summary>
/// Base wrapper over an XElement. Setters mutate the live XElement so that
/// any unknown child elements survive untouched on save (round-trip fidelity).
/// </summary>
public abstract class RdlElement
{
    public XElement Xml { get; }
    protected RdlElement(XElement xml) { Xml = xml; }

    // ---------- string element helpers ----------
    protected string? GetEl(string name) => Xml.Element(Ns.R + name)?.Value;

    protected void SetEl(string name, string? value)
    {
        var n = Ns.R + name;
        var e = Xml.Element(n);
        if (string.IsNullOrEmpty(value))
        {
            e?.Remove();
            return;
        }
        if (e == null) Xml.Add(new XElement(n, value));
        else e.Value = value;
    }

    protected SsrsUnit GetUnit(string name, string defaultUnit = "cm")
        => SsrsUnit.Parse(GetEl(name), defaultUnit);

    protected void SetUnit(string name, SsrsUnit value)
        => SetEl(name, value.ToString());

    protected bool GetBool(string name, bool dflt = false)
    {
        var v = GetEl(name);
        return v == null ? dflt : bool.TryParse(v, out var b) ? b : dflt;
    }

    protected void SetBool(string name, bool value, bool dflt = false)
        => SetEl(name, value == dflt ? null : value ? "true" : "false");

    protected int GetInt(string name, int dflt = 0)
    {
        var v = GetEl(name);
        return v != null && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : dflt;
    }

    protected void SetInt(string name, int value, int dflt = 0)
        => SetEl(name, value == dflt ? null : value.ToString(CultureInfo.InvariantCulture));

    // ---------- attribute helpers ----------
    protected string? GetAttr(string name) => Xml.Attribute(name)?.Value;

    protected void SetAttr(string name, string? value)
    {
        if (string.IsNullOrEmpty(value)) Xml.Attribute(name)?.Remove();
        else Xml.SetAttributeValue(name, value);
    }
}
