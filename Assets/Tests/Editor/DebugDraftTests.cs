using NUnit.Framework;

public sealed class DebugDraftTests
{
    [Test]
    public void LoadAndEdit_TracksUnsavedChanges()
    {
        DebugDraft<int> draft = new();

        draft.Load(3);
        Assert.That(draft.IsDirty, Is.False);

        draft.Value = 4;
        Assert.That(draft.IsDirty, Is.True);

        draft.Load(4);
        Assert.That(draft.IsDirty, Is.False);
    }
}
