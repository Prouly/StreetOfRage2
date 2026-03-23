using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreText;
    private int _score = 0;
    private static GameManager _instance = null;
    [SerializeField] private InventoryCount inventoryCount;
    [SerializeField] private InventoryPicker inventoryPicker;
    
    

    void Start()
    {
        _score = 0;

    }
    
    private void Awake()
    {
        if (_instance != null)
        {
            // Condición para asegurar que solo es creado un GameManager
            if (_instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                // No destruir GameManager si hay cambio de Scene
                DontDestroyOnLoad(this.gameObject);
            }
        }
        else
        {
            // Si no está creado GameManager, asignarlo y no destruirlo con cambio de Scene
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void UpdateScore()
    {
        _scoreText.text = _score.ToString("00000");
    }

    // Update is called once per frame
    public void AddPoints(int points)
    {
        _score += points;
    }
    
    private GameManager()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }
    
    public static GameManager Instance
    {
        get => _instance;
    }

    public InventoryCount GetInventory()
    {
        return inventoryCount;
    }
    
    public InventoryPicker GetInventoryPicker()
    {
        return inventoryPicker;
    }
}
