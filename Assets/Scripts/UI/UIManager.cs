using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManager : MonoBehaviour
{
    [Header("Time slider")]
    [SerializeField] Slider timeslider;
    [SerializeField] TMP_Text timeSliderValue;
    [SerializeField] [Min(1f)] float maxValue;

    [Header("Resources")]
    [SerializeField] TMP_Text greenAmount;
    [SerializeField] TMP_Text redAmount;

    [Header("Ship Building Menu")]
    [SerializeField] BuildShip buildMiningShip;
    [SerializeField] BuildShip buildCargoShip;
    [SerializeField] BuildShip buildCombatShip;
}

[Serializable]
struct BuildShip
{
    public Button button;
    public Resources cost;
    public ResourcesText text;

    public void ApplyCost()
    {
        text.green.text = cost.green.ToString();
        text.red.text = cost.red.ToString();
    }
}

// monobehaviour
partial class UIManager {
    void Start()
    {
        InitTimeSlider();
        InitBuildMenu();
    }

    void InitTimeSlider()
    {
        timeslider.maxValue = maxValue;
        timeslider.value = 1f;
        OnTimeSliderValueChanged(timeslider.value);
    }

    void InitBuildMenu()
    {
        buildMiningShip.ApplyCost();
        buildMiningShip.button.onClick.AddListener(TryBuildMiningShip);

        buildCargoShip.ApplyCost();
        buildCargoShip.button.onClick.AddListener(TryBuildCargoShip);

        buildCombatShip.ApplyCost();
        buildCombatShip.button.onClick.AddListener(TryBuildCombatShip);
    }
}

// inherent impl
partial class UIManager {
    public void OnTimeSliderValueChanged(float value)
    {
        GameManager.RateOfTime = value;
        timeSliderValue.text = value.ToString("F2");
    }

    public void UpdatePlayerResources(Resources res)
    {
        greenAmount.text = res.green.ToString();
        redAmount.text = res.red.ToString();
    }

    // todo: deduplication

    public void TryBuildMiningShip()
    {
        bool shipyardNotFound = !GameManager.PlayerObject.Shipyard(out Shipyard shipyard);
        if (shipyardNotFound) return;
        
        if (shipyard.ConstructionAreaBlocked()) return;

        if (shipyard.resources < buildMiningShip.cost) return;
        shipyard.resources -= buildMiningShip.cost;

        MiningShip ship = MiningShip.SpawnUnchecked("Mining Ship", shipyard.constructionPos);
        GameManager.PlayerObject.team.Add(ship);

        UpdatePlayerResources(shipyard.resources);
    }

    public void TryBuildCargoShip()
    {
        bool shipyardNotFound = !GameManager.PlayerObject.Shipyard(out Shipyard shipyard);
        if (shipyardNotFound) return;
        
        if (shipyard.ConstructionAreaBlocked()) return;

        if (shipyard.resources < buildCargoShip.cost) return;
        shipyard.resources -= buildCargoShip.cost;

        CargoShip ship = CargoShip.SpawnUnchecked("Cargo Ship", shipyard.constructionPos);
        GameManager.PlayerObject.team.Add(ship);

        UpdatePlayerResources(shipyard.resources);
    }

    public void TryBuildCombatShip()
    {
        bool shipyardNotFound = !GameManager.PlayerObject.Shipyard(out Shipyard shipyard);
        if (shipyardNotFound) return;
        
        if (shipyard.ConstructionAreaBlocked()) return;

        if (shipyard.resources < buildCombatShip.cost) return;
        shipyard.resources -= buildCombatShip.cost;

        CombatShip ship = CombatShip.SpawnUnchecked("Combat Ship", shipyard.constructionPos);
        GameManager.PlayerObject.team.Add(ship);

        UpdatePlayerResources(shipyard.resources);
    }
}
