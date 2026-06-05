using System.Xml.Linq;
using ReportEditor.Models;

namespace ReportEditor.Services;

/// <summary>
/// Scoped per-user editor state: the currently loaded report, multi-selection,
/// undo/redo (full XML snapshots — simple but effective at RDL scale),
/// add-item placement mode, and an XML view dirty bit.
/// </summary>
public sealed class EditorState
{
    public Report? Report { get; private set; }
    public string? FileName { get; private set; }
    public bool Dirty { get; private set; }

    public enum EditScope { Body, PageHeader, PageFooter }
    public EditScope Scope { get; private set; } = EditScope.Body;

    public void SetScope(EditScope s)
    {
        Scope = s;
        SelectedItems.Clear();
        Changed?.Invoke();
    }

    /// <summary>Which XElement container the canvas is currently editing.</summary>
    public System.Xml.Linq.XElement? CurrentContainer => Report == null ? null : Scope switch
    {
        EditScope.Body        => Report.Body,
        EditScope.PageHeader  => Report.PageHeader,
        EditScope.PageFooter  => Report.PageFooter,
        _ => Report.Body
    };

    public IEnumerable<ReportItem> CurrentItems
    {
        get
        {
            if (Report == null || CurrentContainer == null) return Array.Empty<ReportItem>();
            return Report.ItemsIn(CurrentContainer);
        }
    }

    /// <summary>All currently-selected items.</summary>
    public List<ReportItem> SelectedItems { get; } = new();

    /// <summary>In-memory clipboard for fast cut/copy/paste without OS roundtrip.</summary>
    public List<System.Xml.Linq.XElement> Clipboard { get; } = new();

    /// <summary>Primary selected item (most-recently clicked).</summary>
    public ReportItem? Selected =>
        SelectedItems.Count == 0 ? null : SelectedItems[^1];

    /// <summary>If non-null, the next canvas click adds an item of this kind.</summary>
    public string? PlacingKind { get; private set; }

    public event Action? Changed;

    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private const int MaxHistory = 50;

    public void Load(string xml, string fileName)
    {
        Report = Report.Load(xml);
        FileName = fileName;
        SelectedItems.Clear();
        PlacingKind = null;
        Dirty = false;
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke();
    }

    public void Select(ReportItem? item, bool additive = false)
    {
        if (item == null)
        {
            SelectedItems.Clear();
            Changed?.Invoke();
            return;
        }
        if (additive)
        {
            // Toggle.
            var existing = SelectedItems.FirstOrDefault(x => ReferenceEquals(x, item));
            if (existing != null) SelectedItems.Remove(existing);
            else SelectedItems.Add(item);
        }
        else
        {
            SelectedItems.Clear();
            SelectedItems.Add(item);
        }
        Changed?.Invoke();
    }

    public void SelectMany(IEnumerable<ReportItem> items)
    {
        SelectedItems.Clear();
        SelectedItems.AddRange(items);
        Changed?.Invoke();
    }

    public bool IsSelected(ReportItem item)
        => SelectedItems.Any(x => ReferenceEquals(x, item));

    public void Mutate(Action mutation)
    {
        if (Report == null) return;
        Snapshot();
        mutation();
        Dirty = true;
        Changed?.Invoke();
    }

    public void MutateContinuous(Action mutation)
    {
        if (Report == null) return;
        mutation();
        Dirty = true;
        Changed?.Invoke();
    }

    public void Snapshot()
    {
        if (Report == null) return;
        _undo.Push(Report.SerializeToString());
        if (_undo.Count > MaxHistory)
        {
            var keep = _undo.Take(MaxHistory).Reverse().ToArray();
            _undo.Clear();
            foreach (var s in keep) _undo.Push(s);
        }
        _redo.Clear();
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Undo()
    {
        if (Report == null || _undo.Count == 0) return;
        _redo.Push(Report.SerializeToString());
        var prev = _undo.Pop();
        Report = Report.Load(prev);
        SelectedItems.Clear();
        Dirty = true;
        Changed?.Invoke();
    }

    public void Redo()
    {
        if (Report == null || _redo.Count == 0) return;
        _undo.Push(Report.SerializeToString());
        var next = _redo.Pop();
        Report = Report.Load(next);
        SelectedItems.Clear();
        Dirty = true;
        Changed?.Invoke();
    }

    public void MarkClean() { Dirty = false; Changed?.Invoke(); }

    public void StartPlacing(string kind) { PlacingKind = kind; Changed?.Invoke(); }
    public void CancelPlacing() { PlacingKind = null; Changed?.Invoke(); }

    /// <summary>Pick a name that doesn't collide with existing body items.</summary>
    public string UniqueName(string prefix)
    {
        if (Report == null) return prefix + "1";
        var existing = Report.BodyItems.Select(x => x.Name).ToHashSet();
        for (int i = 1; i < 10_000; i++)
        {
            var n = prefix + i;
            if (!existing.Contains(n)) return n;
        }
        return prefix + Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    public void Raise() => Changed?.Invoke();
}
