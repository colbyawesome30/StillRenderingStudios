using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UI;

public class PayWall : MonoBehaviour, IInteractable
{
    [SerializeField] private String AreaName;
    [SerializeField] private GameObject puchaseTab;
    [SerializeField] private PlayerInfo playerInfo;
    [SerializeField] private int purchasePrice;
    [SerializeField] private Material hiddenMaterial;
    [SerializeField] private GameObject questionMark;
    public bool purchasedLand;
    [SerializeField] private Text purchaseText;
    [SerializeField] private Text areaNameText;
    [SerializeField] private Rebake NavRebake;
    private NavMeshModifier navModifier;
    public AudioSource audioSource;
    [SerializeField] private AudioClip sucessSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private static PayWall openWall;
    public static bool TabOpen => openWall != null && openWall.puchaseTab != null && openWall.puchaseTab.activeSelf;
    // Remembers each renderer's original materials so they can be restored
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    [Tooltip("Colliders that must stay on while locked, e.g. the one the player taps to buy.")]
    [SerializeField] private List<Collider> keepEnabled = new List<Collider>();
    // Remembers whether each collider was enabled, so ones that were off stay off
    private readonly Dictionary<Collider, bool> originalColliders = new Dictionary<Collider, bool>();

    //On awake get children and add to disctionary as well as lists
    private void Awake()
    {
        navModifier = GetComponent<NavMeshModifier>();
        if (navModifier == null) navModifier = gameObject.AddComponent<NavMeshModifier>();
        navModifier.applyToChildren = true;
        navModifier.ignoreFromBuild = !purchasedLand;

        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            if (puchaseTab != null && rend.transform.IsChildOf(puchaseTab.transform)) continue;
            if (questionMark != null && rend.transform.IsChildOf(questionMark.transform)) continue;

            originalMaterials[rend] = rend.sharedMaterials;
        }
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col.gameObject == gameObject) continue; 
            if (keepEnabled.Contains(col)) continue;
            if (puchaseTab != null && col.transform.IsChildOf(puchaseTab.transform)) continue;

            originalColliders[col] = col.enabled;
        }
        areaNameText.text = AreaName;

        if (questionMark != null)
        {
            NavMeshModifier qmMod = questionMark.GetComponent<NavMeshModifier>();
            if (qmMod == null) qmMod = questionMark.AddComponent<NavMeshModifier>();
            qmMod.overrideArea = false;
            qmMod.ignoreFromBuild = true;   // true = never part of the NavMesh
        }
    }

    //Set references
    private void Start()
    {
        ToggleTab(false);
        if (playerInfo == null) playerInfo = FindAnyObjectByType<PlayerInfo>();
        if (NavRebake == null) NavRebake = FindAnyObjectByType<Rebake>();
        purchaseText.text = "$" + purchasePrice.ToString();

        if (purchasedLand) SetPurchasedLand();
        else
        {
            SetHidden();
            if (NavRebake != null) NavRebake.RebakeMesh();
        }
    }

    //Interact with hidden area
    public void Interact(RaycastHit hit)
    {
        if (PayWall.TabOpen|| WorkStation.UIOpen) return;   // a workstation is selected, ignore the paywall

        if (!purchasedLand)
        {
            ToggleTab(true);
        }
    }

    //Toggle purchase tab
    public void ToggleTab(bool Active)
    {
        if (puchaseTab != null) puchaseTab.SetActive(Active);

        if (Active) openWall = this;
        else if (openWall == this) openWall = null;

        if (Active) audioSource.PlayOneShot(openSound, 0.5f);
        if (!Active) audioSource.PlayOneShot(closeSound, 0.5f);

    }

    //close tab, return materials, and also check we have the right amound of funds before everything
    public void PurchaseLand()
    {
        if (playerInfo.playerFunds < purchasePrice)
        {
            Debug.Log("Not enough funds to buy land");
            audioSource.PlayOneShot(failSound);
            return;
        }
        audioSource.PlayOneShot(sucessSound);
        playerInfo.playerFunds -= purchasePrice;
        purchasedLand = true;
        SetPurchasedLand();
        ToggleTab(false);
    }

    //Put the hidden material on every material slot of every renderer
    private void SetHidden()
    {
        if (navModifier != null) navModifier.ignoreFromBuild = true;
        foreach (var pair in originalColliders)
        {
            if (pair.Key != null) pair.Key.enabled = false;
        }
        if (hiddenMaterial == null)
        {
            Debug.LogWarning("PayWall: no hidden material assigned.", this);
            return;
        }

        foreach (var pair in originalMaterials)
        {
            Renderer rend = pair.Key;
            if (rend == null) continue;

            Material[] hidden = new Material[pair.Value.Length];
            for (int i = 0; i < hidden.Length; i++) hidden[i] = hiddenMaterial;
            rend.sharedMaterials = hidden;
        }
        if (questionMark != null) questionMark.SetActive(true);
    }

    // Restore every renderer's original materials and colliders
    public void SetPurchasedLand()
    {
        foreach (var pair in originalColliders)
        {
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        }
        foreach (var pair in originalMaterials)
        {
            if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        }

        if (navModifier != null) navModifier.ignoreFromBuild = false;   // must happen before the rebake
        Collider own = GetComponent<Collider>();
        if (own != null) own.enabled = false;
        if (NavRebake != null) NavRebake.RebakeMesh();
        if (questionMark != null) questionMark.SetActive(false);
    }

    private void OnDestroy()
    {
        if (openWall == this) openWall = null;
    }
}