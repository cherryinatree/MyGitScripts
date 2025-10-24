using UnityEngine;

public class ShelfSlot : MonoBehaviour
{
    public Carryable _currentItem;
    public GameObject _currentItemGameObject;
    private MerchandisingFixtures parentFixture;

    private void Awake()
    {
        if (_currentItemGameObject != null)
        {
            _currentItemGameObject.transform.localPosition = transform.position;
            _currentItemGameObject.transform.localRotation = transform.rotation;
            _currentItemGameObject.GetComponent<Rigidbody>().isKinematic = true;
            if(parentFixture != null)
            {
                _currentItem.SetFixtureParent(parentFixture);
            }
        }
    }

    public void SetMyParentFixture(MerchandisingFixtures fixture)
    {
        parentFixture = fixture;
        if(_currentItem != null)
        {
            _currentItem.SetFixtureParent(parentFixture);
        }
    }

    public void ClearSlot()
    {
        _currentItem.ClearFixtureParent();
        _currentItem = null;
        _currentItemGameObject = null;
    }

    public void SetItem(Carryable item)
    {
        _currentItem = item;
        _currentItemGameObject = item.gameObject;
        _currentItem.SetFixtureParent(parentFixture);
    }

    public Carryable GetItem()
    {
        return _currentItem;
    }

    public bool isOccupied()
    {
        return _currentItem != null;
    }

    public int GetItemID()
    {
        return _currentItem ? _currentItem.GetComponent<SellItemHolder>().ItemId : -1;
    }
}
