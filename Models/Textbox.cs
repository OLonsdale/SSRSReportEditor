using System.Xml.Linq;

namespace ReportEditor.Models;

public sealed class Textbox : ReportItem
{
    public Textbox(XElement xml) : base(xml) { }

    public bool CanGrow { get => GetBool("CanGrow"); set => SetBool("CanGrow", value); }
    public bool CanShrink { get => GetBool("CanShrink"); set => SetBool("CanShrink", value); }
    public bool KeepTogether { get => GetBool("KeepTogether"); set => SetBool("KeepTogether", value); }

    public IEnumerable<Paragraph> Paragraphs
    {
        get
        {
            var paras = Xml.Element(Ns.R + "Paragraphs");
            if (paras == null) yield break;
            foreach (var p in paras.Elements(Ns.R + "Paragraph"))
                yield return new Paragraph(p);
        }
    }

    /// <summary>The flat text of the first run of the first paragraph.</summary>
    public string PrimaryText
    {
        get
        {
            var run = Paragraphs.FirstOrDefault()?.Runs.FirstOrDefault();
            return run?.Value ?? "";
        }
        set
        {
            var paras = Xml.Element(Ns.R + "Paragraphs");
            if (paras == null)
            {
                paras = new XElement(Ns.R + "Paragraphs");
                Xml.AddFirst(paras);
            }
            var para = paras.Element(Ns.R + "Paragraph");
            if (para == null)
            {
                para = new XElement(Ns.R + "Paragraph",
                    new XElement(Ns.R + "TextRuns",
                        new XElement(Ns.R + "TextRun",
                            new XElement(Ns.R + "Value", value))));
                paras.Add(para);
                return;
            }
            var runs = para.Element(Ns.R + "TextRuns");
            if (runs == null)
            {
                runs = new XElement(Ns.R + "TextRuns",
                    new XElement(Ns.R + "TextRun",
                        new XElement(Ns.R + "Value", value)));
                para.AddFirst(runs);
                return;
            }
            var run = runs.Element(Ns.R + "TextRun");
            if (run == null)
            {
                runs.Add(new XElement(Ns.R + "TextRun",
                    new XElement(Ns.R + "Value", value)));
                return;
            }
            var valEl = run.Element(Ns.R + "Value");
            if (valEl == null) run.AddFirst(new XElement(Ns.R + "Value", value));
            else valEl.Value = value;
        }
    }
}

public sealed class Paragraph : RdlElement
{
    public Paragraph(XElement xml) : base(xml) { }

    public IEnumerable<TextRun> Runs
    {
        get
        {
            var runs = Xml.Element(Ns.R + "TextRuns");
            if (runs == null) yield break;
            foreach (var r in runs.Elements(Ns.R + "TextRun"))
                yield return new TextRun(r);
        }
    }

    public Style Style => Style.For(Xml, "Style");
}

public sealed class TextRun : RdlElement
{
    public TextRun(XElement xml) : base(xml) { }

    public string Value
    {
        get => GetEl("Value") ?? "";
        set => SetEl("Value", value);
    }

    public Style Style => Style.For(Xml, "Style");
}
