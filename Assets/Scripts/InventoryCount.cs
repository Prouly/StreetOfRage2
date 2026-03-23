using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCount : MonoBehaviour
{
    [SerializeField] private Button moneyButton;
    [SerializeField] private Button goldButton;
    [SerializeField] private Button cakeButton;
    [SerializeField] private Button chickenButton;

    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI cakeText;
    [SerializeField] private TextMeshProUGUI chickenText;

    // Diccionario que almacena el conteo de cada tipo de item
    private Dictionary<ItemType, int> _numItems = new Dictionary<ItemType, int>();

    // Diccionario que mapea cada ItemType con su TextMeshProUGUI correspondiente
    private Dictionary<ItemType, TextMeshProUGUI> _itemTexts;

    private void Start()
    {
        // Inicializar textos desde botones si no están asignados
        if (moneyText == null || goldText == null || cakeText == null || chickenText == null)
        {
            moneyText = moneyButton.GetComponentInChildren<TextMeshProUGUI>();
            goldText = goldButton.GetComponentInChildren<TextMeshProUGUI>();
            cakeText = cakeButton.GetComponentInChildren<TextMeshProUGUI>();
            chickenText = chickenButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Mapear cada ItemType a su TextMeshProUGUI
        _itemTexts = new Dictionary<ItemType, TextMeshProUGUI>
        {
            { ItemType.Money,   moneyText },
            { ItemType.Gold,    goldText },
            { ItemType.Cake,    cakeText },
            { ItemType.Chicken, chickenText }
        };

        // Inicializar conteos y textos a 0
        foreach (ItemType itemType in Enum.GetValues(typeof(ItemType)))
        {
            _numItems[itemType] = 0;
            _itemTexts[itemType].text = "0";
        }
    }

    public void AddItem(ItemType itemType)
    {
        // Incrementar conteo en el diccionario
        _numItems[itemType]++;

        // Actualizar el texto correspondiente
        _itemTexts[itemType].text = _numItems[itemType].ToString();
    }
}