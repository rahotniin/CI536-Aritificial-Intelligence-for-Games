using System;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerObject : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] InputActionReference wasd;
    [SerializeField] InputActionReference leftShift;
    [SerializeField] InputActionReference leftCtrl;

    [Header("General")]
    [SerializeField] float moveSpeed = 10f;

    [Header("Mouse")]
    //[SerializeField] float horizontalSensitivity = 1.0f;
    //[SerializeField] float verticalSensitivity = 1.0f;
    [SerializeField] float scrollSensitivity = 80.0f;

    [Header("Camera")]
    [SerializeField] float minCameraHeight = 1f;
    [SerializeField] float maxCameraHeight = 20f;
    [Range(0f, 1f)]
    [SerializeField] float cameraLerpValue = 0.5f;
    [SerializeField] Vector3 targetCameraPos;
    float initCameraY;
    float cameraHeightFactor;

    [Header("Ship Selection")]
    [SerializeField] SelectionArea selectionArea;
    [SerializeField] [Range(0.25f, 1.5f)] float clickSelectionRange = 1f;

    [Header("Team")] // todo: readonly the lot
    // currently, should only ever contain 1 shipyard
    public ObjectPool shipyards;
    // contains all the players ships
    public ObjectPool team;
    ObjectPool selectedShips;
}

public class PlayerTeam {}
public class PlayerSelectedShips {}
public class PlayerShipyards {}

// `MonoBehaviour`
partial class PlayerObject {
    void Start()
    {
        targetCameraPos = Camera.main.transform.localPosition;
        initCameraY = targetCameraPos.y;
        
        shipyards = GameManager.ObjectPools.Get<PlayerShipyards>();
        team = GameManager.ObjectPools.Get<PlayerTeam>();
        selectedShips = GameManager.ObjectPools.Get<PlayerSelectedShips>();
    }

    void Update()
    {
        cameraHeightFactor = Camera.main.transform.localPosition.y / initCameraY;
        
        HandleMovement();
        HandleCameraZooming();
        
        HandleUnitSelection();
        HandleCommandingSelectedShips();

        // todo: pressing escape to clear selection
    }
}

partial class PlayerObject {
    enum SelectionMode
    {
        Select,
        Deselect,
        None,
    }

    // todo: multiple shipyards?
    public bool Shipyard(out Shipyard shipyard)
    {
        shipyard = null;
        foreach (Shipyard yard in shipyards.objects)
        {
            shipyard = yard;
            return true;
        }
        return false;
    }

    //==========================//

    void HandleMovement()
    {
        Vector2 h = wasd.action.ReadValue<Vector2>() * moveSpeed;
        Vector3 dz = h.y * transform.forward;
        Vector3 dx = h.x * transform.right;
        Vector3 ds = (dx + dz) * cameraHeightFactor * Time.deltaTime;
        transform.position += ds;
    }

    // todo: conventional middle-button-drag camera movement,
    // this is unused for now, as right-button conflicts with telling ships where to go
    // void HandleLooking()
    // {
    //     if (!Mouse.current.rightButton.IsPressed()) { return; }
    //     var ds = Mouse.current.delta.ReadValue() * Time.deltaTime;
    //     // rotate the player around the vertical axis
    //     transform.Rotate(Vector3.up, ds.x * horizontalSensitivity);
    //     // rotate the camera around the horizontal axis
    //     Camera.main.transform.Rotate(Vector3.left, ds.y * verticalSensitivity);
    // }

    void HandleCameraZooming()
    {
        // note: `.y` seems to only ever be either -1, 0, or 1
        float scroll = Mouse.current.scroll.ReadValue().y * scrollSensitivity;
        
        Vector3 dir = transform.worldToLocalMatrix * Camera.main.transform.forward;
        Vector3 ds = dir * scroll * Time.deltaTime;

        float newTargetY = targetCameraPos.y + ds.y;
        if (newTargetY > minCameraHeight && newTargetY < maxCameraHeight)
        {
            targetCameraPos += ds;
        }

        Vector3 current = Camera.main.transform.localPosition;
        
        Camera.main.transform.localPosition = Vector3.Lerp(current, targetCameraPos, cameraLerpValue);
    }

    void HandleUnitSelection()
    {
        Vector2 mousePos;
        if (!TryGetMousePos(out mousePos)) { return; }

        SelectionMode mode = (leftShift.action.IsPressed(), leftCtrl.action.IsPressed()) switch
        {
            (false, false) or (true, false) => SelectionMode.Select,
            (false, true) => SelectionMode.Deselect,
            _ => SelectionMode.None,
        };

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!leftShift.action.IsPressed() && !leftCtrl.action.IsPressed())
            {
                DeselectAllShips();
            }
            TrySelectNearestShip(Vector2Int.RoundToInt(mousePos), mode);
            
            // begin sweep-select
            selectionArea.gameObject.SetActive(true);
            selectionArea.transform.position = new Vector3(mousePos.x, 0f, mousePos.y);
        }
        
        bool isSweeping = selectionArea.gameObject.activeSelf;
        if (isSweeping) { SweepSelectShips(mousePos, mode); }

        if (Mouse.current.leftButton.wasReleasedThisFrame) { 
            // stop sweep-select
            selectionArea.gameObject.SetActive(false);
        }
    }

    bool TryGetMousePos(out Vector2 mousePos)
    {
        mousePos = Vector2.positiveInfinity;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        Rect screenBounds = Camera.main.pixelRect;
        if (!screenBounds.Contains(mouseScreenPos)) { return false; }

        Ray mouseRay = Camera.main.ScreenPointToRay(mouseScreenPos);
        bool didntHit = !Physics.Raycast(mouseRay, out RaycastHit hit);
        if (didntHit) { return false; }

        mousePos = hit.point.XZ();
        return true;
    }

    void DeselectAllShips()
    {
        foreach (Spaceship ship in selectedShips.objects)
        {
            ship.Deselect();
        }
        selectedShips.Clear();
    }

    void SelectShip(Spaceship ship, SelectionMode mode)
    {
        switch (mode)
        {
            case SelectionMode.Select:
                ship.Select();
                selectedShips.Add(ship);
                break;
            case SelectionMode.Deselect:
                ship.Deselect();
                selectedShips.Remove(ship);
                break;
        }
    }

    void TrySelectNearestShip(Vector2Int pos, SelectionMode mode)
    {
        if (mode == SelectionMode.None) { return; }

        Spaceship closest = null;
        float dst = float.MaxValue;

        const int RANGE = 1;
        for (int dx = -RANGE; dx <= RANGE; dx++)
        {
            for (int dz = -RANGE; dz <= RANGE; dz++)
            {
                Vector2Int samplePos = pos + new Vector2Int(dx, dz);

                Spaceship ship = null;
                foreach (var occupant in GameManager.SpatialDictionary.Get(samplePos))
                {
                    if (occupant is Spaceship)
                    {
                        ship = (Spaceship)occupant;
                        break;
                    }

                }
                if (ship is null) { continue; }
                
                Vector2 shipPos = ship.transform.position.XZ();
                float sampleDst = Vector2.SqrMagnitude(shipPos - pos);

                if (sampleDst < dst && sampleDst < clickSelectionRange * clickSelectionRange)
                {
                    dst = sampleDst;
                    closest = ship;
                }
            }
        }
        
        if (closest != null)
        {
            SelectShip(closest, mode);
        }
    }

    void SweepSelectShips(Vector2 mousePos, SelectionMode mode)
    {
        Vector2 scale = mousePos - selectionArea.transform.position.XZ();
        selectionArea.transform.localScale = new Vector3(scale.x, 1f, scale.y);

        if (mode == SelectionMode.None) { return; }

        Rect selectionBounds = selectionArea.Bounds();

        foreach (Spaceship ship in team.objects)
        {
            Vector2 shipPos = new(ship.transform.position.x, ship.transform.position.z);
            if (selectionBounds.Contains(shipPos, true))
            {
                SelectShip(ship, mode);
            }
        }
    }

    void HandleCommandingSelectedShips()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) { return; }
        
        Vector2 mousePos;
        if (!TryGetMousePos(out mousePos)) { return; }
        Vector2Int mousePosInt = Vector2Int.RoundToInt(mousePos);

        Action<Spaceship> command = leftShift.action.IsPressed() switch
        {
            false => (ship) =>
            {
                ship.nextCommand = mousePosInt;
                ship.deferredCommands.Clear();
            },
            true => (ship) => { ship.deferredCommands.Enqueue(mousePosInt); },
        };

        foreach (Spaceship ship in selectedShips.objects)
        {
            command(ship);
        }
    }
}
