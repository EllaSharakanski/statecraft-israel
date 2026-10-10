#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Playtest 2, P2-4: map clicks under same-frame input.</summary>
public class MapPressTests
{
    /// <summary>MapPointer evaluates every press at its own position, in order, even when several land in one frame.</summary>
    [Test]
    public void EveryMapPressIsCheckedAtItsOwnPosition()
    {
        var go = new GameObject("P24Pointer");
        try
        {
            var pointer = go.AddComponent<MapPointer>();
            var asked = new List<Vector2>();
            pointer.IsOverUi = p => { asked.Add(p); return p.y > 1000; };   // the top of the screen is UI
            pointer.OnPress(new Vector2(1908, 1151));
            pointer.OnPress(new Vector2(1630, 317));
            pointer.ProcessPresses();
            CollectionAssert.AreEqual(new[] { new Vector2(1908, 1151), new Vector2(1630, 317) }, asked, "both presses, each where it happened");
            asked.Clear();
            pointer.ProcessPresses();
            Assert.IsEmpty(asked, "a press is handled once");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
