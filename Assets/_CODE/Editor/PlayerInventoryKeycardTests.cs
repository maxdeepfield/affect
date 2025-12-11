using NUnit.Framework;
using UnityEngine;

public class PlayerInventoryKeycardTests
{
    private GameObject _go;
    private PlayerInventory _inventory;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("PlayerInventory_Test");
        _inventory = _go.AddComponent<PlayerInventory>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
    }

    [Test]
    public void UpgradeKeycard_IncreasesLevel_AndInvokesEvent()
    {
        int lastLevel = -1;
        _inventory.OnKeycardLevelChanged.AddListener(level => lastLevel = level);

        _inventory.UpgradeKeycard(2);

        Assert.AreEqual(2, _inventory.KeycardLevel);
        Assert.AreEqual(2, lastLevel);
    }

    [Test]
    public void UpgradeKeycard_DoesNotDowngrade()
    {
        _inventory.UpgradeKeycard(3);
        _inventory.UpgradeKeycard(1);

        Assert.AreEqual(3, _inventory.KeycardLevel);
        Assert.IsTrue(_inventory.HasKeycard(2));
    }

    [Test]
    public void HasKeycard_UsesAtLeastComparison()
    {
        _inventory.UpgradeKeycard(2);

        Assert.IsTrue(_inventory.HasKeycard(1));
        Assert.IsTrue(_inventory.HasKeycard(2));
        Assert.IsFalse(_inventory.HasKeycard(3));
    }

    [Test]
    public void ClearInventory_ResetsLevel_AndInvokesEvent()
    {
        int lastLevel = -1;
        _inventory.OnKeycardLevelChanged.AddListener(level => lastLevel = level);

        _inventory.UpgradeKeycard(2);
        _inventory.ClearInventory();

        Assert.AreEqual(0, _inventory.KeycardLevel);
        Assert.AreEqual(0, lastLevel);
    }
}
