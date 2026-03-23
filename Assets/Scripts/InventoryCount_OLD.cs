using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCount_OLD : MonoBehaviour
{
    //[SerializeField] private int maxNumberItem = 5;

    //References to 5 predefined items in the inventory (Buttons)
    [SerializeField] private Button moneyButton;
    [SerializeField] private Button goldButton;
    [SerializeField] private Button cakeButton;
    [SerializeField] private Button chickenButton;

    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI cakeText;
    [SerializeField] private TextMeshProUGUI chickenText;
    
    
    // Array of number of item in the inventory
    private int[] _numItems = new int [Enum.GetValues(typeof(ItemType)).Length];

    private void Start()
    {
        _numItems = new int [Enum.GetValues(typeof(ItemType)).Length];
        
        // set the text of the buttons to 0
        if (moneyText == null || goldText == null || cakeText == null || chickenText == null)
        {
            // get the text of the buttons
            moneyText = moneyButton.GetComponentInChildren<TextMeshProUGUI>();
            goldText = goldButton.GetComponentInChildren<TextMeshProUGUI>();
            cakeText = cakeButton.GetComponentInChildren<TextMeshProUGUI>();
            chickenText = chickenButton.GetComponentInChildren<TextMeshProUGUI>();
        }
        moneyText.text = "0";
        goldText.text = "0";
        cakeText.text = "0";
        chickenText.text = "0";
    }
    
    //Cambiar esta clase por un diccionario
    
    public void AddItem(ItemType itemType)
    {
        // add _numItems to the item type
        _numItems[(int) itemType]++;
            
        // update the text of the button
        switch (itemType)
        {
            case ItemType.Money:
                moneyText.text = _numItems[(int) itemType].ToString();
                break;
            case ItemType.Gold:
                goldText.text = _numItems[(int) itemType].ToString();
                break;
            case ItemType.Cake:
                cakeText.text = _numItems[(int) itemType].ToString();
                break;
            case ItemType.Chicken:
                chickenText.text = _numItems[(int) itemType].ToString();
                break;
        }
    }

}
