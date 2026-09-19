using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class IndexerTexter : MonoBehaviour
{
    public class Inventory
    {
        private int[] _items;

        public Inventory(int length)
        {
            _items = new int[length];
            for (int i = 0; i < _items.Length; i++)
            {
                _items[i] = i * 10; // 예시로 초기화
            }
        }

        public int Length => _items.Length;

        public int this[int index]
        {
            get
            {
                return _items[index];
            }
            set
            {
                _items[index] = value;
            }
        }
    }

    private Inventory _inventory;
    private void Awake()
    {
        _inventory = new Inventory(1000);
    }

    private int _cursor = 0;
    private void Update()
    {
        _cursor++;
        _cursor %= _inventory.Length;
        int item = _inventory[_cursor];
        UDebug.Print($"인벤토리에서 {_cursor}번째 아이템: {_inventory[_cursor]}");
    }
}
