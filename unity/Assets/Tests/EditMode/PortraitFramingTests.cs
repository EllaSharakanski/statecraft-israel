#nullable enable
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;

/// <summary>Playtest 2, P2-5: event portraits show the face. Each portrait carries a documented focal point (people.yaml
/// portrait.focus_y, the face's height as a share of the picture); the card positions the picture so that point is in view.</summary>
public class PortraitFramingTests
{
    const string CatalogPath = "Assets/UI/Portraits/PortraitCatalog.asset";

    [Test]
    public void EveryPortraitFramesItsFace()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        Assert.IsNotNull(catalog);
        Assert.IsNotEmpty(catalog.entries);
        foreach (var e in catalog.entries)
        {
            Assert.That(e.focusY, Is.InRange(0.05f, 0.95f), $"{e.personId}: a focal point");
            var (top, bottom) = PortraitFraming.VisibleBand(e.image.width, e.image.height, EventCardPresenter.PortraitWidth, EventCardPresenter.PortraitHeight, e.focusY);
            Assert.LessOrEqual(top, Mathf.Max(0f, e.focusY - 0.12f), $"{e.personId}: the top of the face is in view ({top:0.00}-{bottom:0.00})");
            Assert.GreaterOrEqual(bottom, Mathf.Min(1f, e.focusY + 0.12f), $"{e.personId}: the chin is in view ({top:0.00}-{bottom:0.00})");
        }
    }

    /// <summary>The framing arithmetic: a wide box shows a band of a tall picture, centred on the focal point where it can be.</summary>
    [Test]
    public void TheBandIsCentredOnTheFocalPoint()
    {
        var (top, bottom) = PortraitFraming.VisibleBand(375, 512, 396, 200, 0.30f);    // the old banner: a band of the picture
        Assert.AreEqual(0.30f, (top + bottom) / 2, 0.01f, "centred on the face");
        (top, bottom) = PortraitFraming.VisibleBand(375, 512, 396, 200, 0.05f);
        Assert.AreEqual(0f, top, 1e-4f, "clamped at the picture's top");
        Assert.AreEqual(100f, PortraitFraming.PositionYPercent(375, 512, 396, 200, 0.99f), 1e-3f, "clamped at the bottom");
    }
}
