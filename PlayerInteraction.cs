using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Object;
using CardGame;  // Add this to get access to GameState enum

public class PlayerInteraction : MonoBehaviour {
    // Current hovered card reference
    private CardGame.Card currentHoveredCard = null;
    private Dictionary<CardGame.Card, Material> originalMaterials = new Dictionary<CardGame.Card, Material>();
    private CardGame.GameManager gameManager;
    
    // How far to raycast
    public float rayDistance = 100f;

    void OnDisable() {
        // Reset any active hover effect when disabled
        if (currentHoveredCard != null) {
            SetCardFade(currentHoveredCard);
            currentHoveredCard = null;
        }
    }

    void OnDestroy() {
        // Clean up all stored materials
        foreach (Material mat in originalMaterials.Values) {
            if (mat != null) {
                Destroy(mat);
            }
        }
        originalMaterials.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindFirstObjectByType<CardGame.GameManager>();
    }

    void Update()
    {
        // Get current game state early to avoid multiple calls
        var currentState = gameManager?.GetCurrentGameState() ?? GameState.Initializing;
        
        // Enhanced state check for interactions
        if (gameManager == null || 
            currentState == GameState.Initializing ||
            currentState == GameState.GameOver ||
            currentState == GameState.Intermission ||
            currentState == GameState.DealerTurn ||
            gameManager.IsDealing() ||
            gameManager.IsReturnOrRandomizing()) {
            
            if (currentHoveredCard != null) {
                SetCardFade(currentHoveredCard);
                currentHoveredCard = null;
            }
            return;
        }

        // Only allow interaction during player's turn and when not dealing
        if (currentState != GameState.PlayerTurn) {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, rayDistance)) {
            CardGame.Card hitCard = hit.collider.GetComponent<CardGame.Card>();
            
            // Only allow hovering over player's own cards
            if (hitCard != null && hitCard.transform.parent != null) {
                bool isPlayerCard = hitCard.transform.parent.name.StartsWith("PlayerCardSlot_");
                
                if (isPlayerCard) {
                    if (currentHoveredCard != hitCard) {
                        if (currentHoveredCard != null)
                            SetCardFade(currentHoveredCard);
                        
                        currentHoveredCard = hitCard;
                        SetCardTransparent(currentHoveredCard);
                    }
                } else {
                    if (currentHoveredCard != null) {
                        SetCardFade(currentHoveredCard);
                        currentHoveredCard = null;
                    }
                }
            } else {
                if (currentHoveredCard != null) {
                    SetCardFade(currentHoveredCard);
                    currentHoveredCard = null;
                }
            }
        } else {
            if (currentHoveredCard != null) {
                SetCardFade(currentHoveredCard);
                currentHoveredCard = null;
            }
        }
    }

    private void OnEnable() {
        // Clear any lingering effects when enabled
        if (currentHoveredCard != null) {
            SetCardFade(currentHoveredCard);
            currentHoveredCard = null;
        }
    }
    
    // Set card rendering to transparent (e.g., reduce alpha)
    private void SetCardTransparent(CardGame.Card card)
    {
        Renderer rend = card.gameObject.GetComponent<Renderer>();
        if (rend != null)
        {
            // Store original material if we haven't already
            if (!originalMaterials.ContainsKey(card)) {
                originalMaterials[card] = rend.material;
            }

            // Create new material instance
            rend.material = new Material(originalMaterials[card]);
            
            // Set shader to Standard if supported and set transparent mode
            if (rend.material.shader.name == "Standard") {
                rend.material.SetFloat("_Mode", 3);
                rend.material.DisableKeyword("_ALPHATEST_ON");
                rend.material.EnableKeyword("_ALPHABLEND_ON");
                rend.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                rend.material.renderQueue = 3000;
            }
            Color col = rend.material.color;
            col.a = 0.5f;  // semi-transparent
            rend.material.color = col;
        }
    }
    
    // Reset card rendering back to opaque
    private void SetCardFade(CardGame.Card card)
    {
        Renderer rend = card.gameObject.GetComponent<Renderer>();
        if (rend != null)
        {
            // Restore original material if we have it
            if (originalMaterials.ContainsKey(card)) {
                rend.material = originalMaterials[card];
                originalMaterials.Remove(card);
            }
        }
    }

    // Getter for currentHoveredCard
    public CardGame.Card GetHoveredCard()
    {
        return currentHoveredCard;
    }
}