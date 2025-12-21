using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    // [SerializeField] private VisualTreeAsset inventoryItemTemplate; // UXML pre item slot
    // private UIDocument uiDoc;
    // private ListView inventoryList;
    // private VisualElement inventoryOverlay;
    // private Label selectedItemLabel;
    // private Button useButton, dropButton;

    // private PlayerController player;
    // private int selectedIndex = -1;
    // private InputAction inventoryAction, scrollAction, leftArrowAction, rightArrowAction;

    // void Start()
    // {
    //     uiDoc = GetComponent<UIDocument>();
    //     player = FindFirstObjectByType<PlayerController>();
    //     SetupUI();
    //     SetupInput();
    // }

    // void SetupUI()
    // {
    //     var root = uiDoc.rootVisualElement;
    //     inventoryOverlay = root.Q<VisualElement>("InventoryOverlay");
    //     inventoryList = root.Q<ListView>("InventoryList");
    //     selectedItemLabel = root.Q<Label>("SelectedItemLabel");
    //     useButton = root.Q<Button>("UseButton");
    //     dropButton = root.Q<Button>("DropButton");

    //     // ListView setup
    //     inventoryList.makeItem = () => inventoryItemTemplate.Instantiate();
    //     inventoryList.bindItem = BindInventoryItem;
    //     inventoryList.selectionChanged += OnItemSelected;
    //     inventoryList.fixedItemHeight = 50;

    //     useButton.clicked += UseSelectedItem;
    //     dropButton.clicked += DropSelectedItem;
    // }

    // void SetupInput()
    // {
    //     var inputActions = InputSystem.actions;
    //     inventoryAction = inputActions.FindAction("Inventory"); // Pridaj do Input Actions map: I key
    //     scrollAction = inputActions.FindAction("Scroll"); // Mouse scroll
    //     leftArrowAction = inputActions.FindAction("Move"); // <- arrow (uprav index)
    //     rightArrowAction = inputActions.FindAction("Move"); // -> arrow

    //     inventoryAction.performed += _ => ToggleInventory();
    // }

    // public void UpdateInventory(List<CollectibleItem> items)
    // {
    //     inventoryList.itemsSource = items;
    //     inventoryList.Rebuild();
    //     UpdateSelectedDisplay();
    // }

    // void BindInventoryItem(VisualElement element, int index)
    // {
    //     var item = (CollectibleItem)inventoryList.itemsSource[index];
    //     var icon = element.Q<VisualElement>("ItemIcon");
    //     var nameLabel = element.Q<Label>("ItemName");
    //     icon.style.backgroundImage = new StyleBackground(item.itemIcon.texture);
    //     nameLabel.text = item.itemName;

    //     // Highlight selected
    //     element.ClearClassList();
    //     if (index == selectedIndex) element.AddToClassList("selected");

    //     element.RegisterCallback<MouseEnterEvent>(e => SelectItem(index));
    // }

    // void OnItemSelected(System.Collections.Generic.IEnumerable<object> selected)
    // {
    //     selectedIndex = inventoryList.selectedIndex;
    //     UpdateSelectedDisplay();
    //     inventoryList.Rebuild(); // Refresh highlight
    // }

    // void UpdateSelectedDisplay()
    // {
    //     if (selectedIndex >= 0 && selectedIndex < player.inventoryItems.Count)
    //     {
    //         selectedItemLabel.text = $"Vybrané: {player.inventoryItems[selectedIndex].itemName}";
    //     }
    // }

    // void ToggleInventory()
    // {
    //     inventoryOverlay.style.display = 
    //         inventoryOverlay.style.display == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;
    //     if (inventoryOverlay.style.display == DisplayStyle.Flex) UpdateInventory(player.inventoryItems);
    // }

    // void SelectItem(int index)
    // {
    //     selectedIndex = index;
    //     inventoryList.selectedIndex = index;
    //     UpdateSelectedDisplay();
    // }

    // void UseSelectedItem()
    // {
    //     if (selectedIndex < 0) return;
    //     var item = player.inventoryItems[selectedIndex];
    //     // Logic podľa itemu: nápoj +HP, cigareta +HP, ostatné drop
    //     GameManager.Instance.ChangeHealth(20); // Príklad
    //     player.inventoryItems.RemoveAt(selectedIndex);
    //     UpdateInventory(player.inventoryItems);
    //     selectedIndex = -1;
    // }

    // void DropSelectedItem()
    // {
    //     if (selectedIndex >= 0)
    //     {
    //         // Spawn drop prefab s damage v minihre
    //         player.inventoryItems.RemoveAt(selectedIndex);
    //         UpdateInventory(player.inventoryItems);
    //         selectedIndex = -1;
    //     }
    // }

    // void Update() // Scroll/navigácia
    // {
    //     float scroll = scrollAction.ReadValue<Vector2>().y;
    //     if (scroll != 0) selectedIndex = Mathf.Clamp(selectedIndex + (scroll > 0 ? -1 : 1), 0, player.inventoryItems.Count - 1);

    //     // Arrow keys podobné
    //     UpdateSelectedDisplay();
    // }


    public static InventoryManager Instance { get; private set; }
    [SerializeField] private GameObject inventoryUI;
    public Transform itemsParent;
    InventorySlot[] slots;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        inventoryUI.SetActive(false);
        GameManager.Instance.onItemChangedCallback += UpdateUI;

        slots = itemsParent.GetComponentsInChildren<InventorySlot>();
    }

    public void ShowUI()
    {
        Debug.Log("Toggling Inventory UI");
        inventoryUI.SetActive(!inventoryUI.activeSelf);
    }

    public void UpdateUI()
    {
        Debug.Log("Updating Inventory UI");
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < GameManager.Instance.m_CollectedItems.Count)
            {
                slots[i].AddItem(GameManager.Instance.m_CollectedItems[i]);
            }
        }
    }
}