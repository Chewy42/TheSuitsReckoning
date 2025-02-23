using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Object;
using CardGame;

public class PlayerInteraction : MonoBehaviour {
    // Current hovered card reference
    private Card currentHoveredCard = null;
    private Dictionary<Card, Material> originalMaterials = new Dictionary<Card, Material>();
    private GameManager gameManager;
    
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

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    void Update()
    {
        if (gameManager == null)
        {
            ClearHoverState();
            return;
        }

        // Get current game state early to avoid multiple calls
        GameState currentState = gameManager.GetCurrentGameState();
        
        // Enhanced state check for interactions
        bool shouldDisableInteraction = currentState == GameState.Initializing ||
                                      currentState == GameState.GameOver ||
                                      currentState == GameState.Intermission ||
                                      currentState == GameState.DealerTurn ||
                                      gameManager.IsDealing() ||
                                      gameManager.IsReturnOrRandomizing();
        
        if (shouldDisableInteraction)
        {
            ClearHoverState();
            return;
        }

        // Only allow interaction during player's turn and when not dealing
        if (currentState != GameState.PlayerTurn)
        {
            ClearHoverState();
            return;
        }

        HandleCardHover();
    }

    private void HandleCardHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            Card hitCard = hit.collider.GetComponent<Card>();
            
            if (hitCard != null && hitCard.transform.parent != null)
            {
                bool isPlayerCard = hitCard.transform.parent.name.StartsWith("PlayerCardSlot_");
                
                if (isPlayerCard)
                {
                    if (currentHoveredCard != hitCard)
                    {
                        ClearHoverState();
                        currentHoveredCard = hitCard;
                        SetCardTransparent(currentHoveredCard);
                    }
                }
                else
                {
                    ClearHoverState();
                }
            }
            else
            {
                ClearHoverState();
            }
        }
        else
        {
            ClearHoverState();
        }
    }

    private void ClearHoverState()
    {
        if (currentHoveredCard != null)
        {
            SetCardFade(currentHoveredCard);
            currentHoveredCard = null;
        }
    }

    private void OnEnable()
    {
        ClearHoverState();
    }
    
    private void SetCardTransparent(Card card)
    {
        Renderer rend = card.gameObject.GetComponent<Renderer>();
        if (rend != null)
        {
            // Store original material if we haven't already
            if (!originalMaterials.ContainsKey(card))
            {
                originalMaterials[card] = rend.material;
            }

            // Create new material instance
            rend.material = new Material(originalMaterials[card]);
            
            // Set shader to Standard if supported and set transparent mode
            if (rend.material.shader.name == "Standard")
            {
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
    
    private void SetCardFade(Card card)
    {
        Renderer rend = card.gameObject.GetComponent<Renderer>();
        if (rend != null && originalMaterials.ContainsKey(card))
        {
            rend.material = originalMaterials[card];
            originalMaterials.Remove(card);
        }
    }

    public Card GetHoveredCard() => currentHoveredCard;
}
