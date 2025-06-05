using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
public class LoadControllerPreferences : MonoBehaviour
{
    public String preferenceKeyJoystick = "joystick";
    public String preferenceKeyTeleport = "teleportation";
    public String preferenceKeyTurn = "turn";

    public String preferenceKeyFly = "fly";
    public String preferenceKeyLeft = "left";

    public TeleportationProvider teleportationProvider;
    public XRRayInteractor rayInteractor;

    public SnapTurnProvider snapTurnProvider;
    public ContinuousTurnProvider continuousTurnProvider;

    public DynamicMoveProvider moveProvider;
    private int isJoystickEnabled;
    private int isTeleportEnabled;

    private int turnValue;
    private int isFlyEnabled;
    private int isLeftEnabled;

    [Header("For the left handed mode")]
    public GameObject LeftControllerVisual;
    public GameObject RightControllerVisual;
    public GameObject LeftControllerVisualUniversalController;
    public GameObject RightControllerVisualUniversalController;
    public GameObject CanvasButtonOnController;
    public GameObject ControllerSettings;
    public GameObject ControllerHelpMenu;
    public GameObject ControllerSpawnMenu;
    public GameObject Clock;
    public GameObject TextHoverController;
    public GameObject Plus;
    public GameObject Minus;
    public GameObject CanvasSlidersOnController;
    public GameObject SpawnPoint;
    public GameObject CanvasImageSpawn;


    private UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask originalInteractionLayers;
    void Start()
    {
        originalInteractionLayers = rayInteractor.interactionLayers;
        
        LoadJoystickPreference();

        LoadTeleportPreference();

        LoadTurnPreference();

        LoadFlyPreference();
        
        LoadLeftPreference();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LoadTeleportPreference()
    {
        isTeleportEnabled = PlayerPrefs.GetInt(preferenceKeyTeleport, 1);
        if (isTeleportEnabled == 0)
        {
            teleportationProvider.enabled = false;
            rayInteractor.interactionLayers = 0;
        }
        else
        {
            teleportationProvider.enabled = true;
            rayInteractor.interactionLayers = originalInteractionLayers;
        }
    }

    private void LoadJoystickPreference()
    {
        isJoystickEnabled = PlayerPrefs.GetInt(preferenceKeyJoystick, 1);
        if (isJoystickEnabled == 0)
        {
            moveProvider.enabled = false;
        }
        else
        {
            moveProvider.enabled = true;
        }
        
    }

    private void LoadFlyPreference()
    {
        isFlyEnabled = PlayerPrefs.GetInt(preferenceKeyFly, 0);
        if (isFlyEnabled == 1)
        {
            moveProvider.enableFly = true;
        }
        else
        {
            moveProvider.enableFly = false;
        }
    }

    private void LoadTurnPreference()
    {
        turnValue = PlayerPrefs.GetInt(preferenceKeyTurn, 0);
        switch (turnValue)
        {
            case 0: // cas continuous
                continuousTurnProvider.enabled = true;
                snapTurnProvider.enabled = false;
                break;
            case 1: // cas snap
                continuousTurnProvider.enabled = false;
                snapTurnProvider.enabled = true;
                break;
            default:
                // desactiver les deux
                continuousTurnProvider.enabled = false;
                snapTurnProvider.enabled = false;
                break;
        }
    }

    private void LoadLeftPreference()
    {
        isLeftEnabled = PlayerPrefs.GetInt(preferenceKeyLeft, 0);

        bool isRight = isLeftEnabled == 1;

        // Choix des parents selon la préférence
        Transform mainParent = isRight ? RightControllerVisual.transform : LeftControllerVisual.transform;
        Transform altParent = isRight ? LeftControllerVisualUniversalController.transform : RightControllerVisualUniversalController.transform;

        // Éléments à positionner sur le contrôleur principal
        SetPosition(CanvasButtonOnController.transform, mainParent, 0f, 0.0102f, -0.0025f);
        SetPosition(ControllerSettings.transform, mainParent, isRight ? -0.5f : 0f, 0f, 0f);
        SetPosition(ControllerHelpMenu.transform, mainParent, isRight ? -0.45f : 0f, 0f, 0f);
        SetPosition(ControllerSpawnMenu.transform, mainParent, isRight ? -0.5f : 0f, 0f,0f);
        SetPosition(Clock.transform, mainParent, 0f, 0f,0f);
        SetPosition(TextHoverController.transform, mainParent, 0f,0f,0f);
        SetPosition(CanvasSlidersOnController.transform, mainParent, 0f, -0.0207f, 0.051f);
        SetPosition(SpawnPoint.transform, mainParent, 0f,0f,-0.54f);

        // Boutons - vont toujours sur le contrôleur opposé
        SetPosition(Minus.transform, altParent, 0f,0.00639f, -0.003570002f);
        SetPosition(Plus.transform, altParent, 0f,0.00639f, -0.003570002f);
        SetPosition(CanvasImageSpawn.transform, altParent, 0f, 0.02f, -0.0346f);
    }

    private void SetPosition(Transform item, Transform parent, float newX, float newY, float newZ)
    {
        item.SetParent(parent);
        Vector3 pos = item.localPosition;
        pos.x = newX;
        pos.y = newY;
        pos.z = newZ;
        item.localPosition = pos;
    }

}
