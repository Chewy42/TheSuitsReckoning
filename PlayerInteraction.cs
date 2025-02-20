using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour {
    // Current hovered card reference
    private CardGame.Card currentHoveredCard = null;
    private Dictionary<CardGame.Card, Material> originalMaterials = new Dictionary<CardGame.Card, Material>();
    
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
    
    }

    // Update is called once per frame
    void Update()
    {
        // Perform a raycast from mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            CardGame.Card hitCard = hit.collider.GetComponent<CardGame.Card>();
            if (hitCard != null)
            {
                // If the hovered card has changed, reset previous and update new one.
                if (currentHoveredCard != hitCard)
                {
                    if (currentHoveredCard != null)
                        SetCardFade(currentHoveredCard);
                    
                    currentHoveredCard = hitCard;
                    SetCardTransparent(currentHoveredCard);
                }
            }
            else
            {
                // Ray hit but not a card: reset if there is a hovered card.
                if (currentHoveredCard != null)
                {
                    SetCardFade(currentHoveredCard);
                    currentHoveredCard = null;
                }
            }
        }
        else
        {
            // No hit: reset if a card was previously hovered.
            if (currentHoveredCard != null)
            {
                SetCardFade(currentHoveredCard);
                currentHoveredCard = null;
            }
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