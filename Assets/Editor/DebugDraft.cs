using System.Collections.Generic;

/// <summary>可供 Editor 调试页复用的独立编辑草稿状态。</summary>
public sealed class DebugDraft<T>
{
    private T saved;

    internal bool HasValue { get; private set; }
    internal T Value { get; set; }
    internal bool IsDirty => HasValue && !EqualityComparer<T>.Default.Equals(Value, saved);

    internal void Load(T value)
    {
        saved = value;
        Value = value;
        HasValue = true;
    }
}
