using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace CardGame {
    public class CardSlot {
        public PlayerType Owner { get; set; }
        public int SlotNumber { get; set; }
        public Transform SlotTransform { get; set; }
    }

    public class TableCards : MonoBehaviour {
        private List<CardSlot> playerCardSlots = new List<CardSlot>();
        private List<CardSlot> dealerCardSlots = new List<CardSlot>();
        private Dictionary<Transform, CardSlot> slotLookup = new Dictionary<Transform, CardSlot>();
        private bool isInitialized = false;

        void Awake() {
            LoadCardSlots();
            ValidateSlots();
        }

        private void ValidateSlots() {
            ValidateSlotList(playerCardSlots, "Player");
            ValidateSlotList(dealerCardSlots, "Dealer");
        }

        private void ValidateSlotList(List<CardSlot> slots, string owner) {
            if (slots.Count == 0) {
                Debug.LogError($"No {owner.ToLower()} card slots found! Please check naming convention: {owner}CardSlot_X");
                return;
            }

            slots.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));
            
            // Validate each slot
            for (int i = 0; i < slots.Count; i++) {
                var slot = slots[i];
                if (slot.SlotNumber != i) {
                    Debug.LogWarning($"{owner} card slots are not sequential. Expected {i} but found {slot.SlotNumber}");
                }
                if (slot.SlotTransform == null) {
                    Debug.LogError($"{owner} slot {i} has no transform reference!");
                }
                Debug.Log($"Validated {owner} slot {i}: Transform exists: {slot.SlotTransform != null}, Position: {slot.SlotTransform?.position}");
            }
        }

        public int GetAvailableSlotCount(PlayerType playerType) {
            var slots = playerType == PlayerType.Player ? playerCardSlots : dealerCardSlots;
            int usedSlots = 0;
            foreach (var slot in slots) {
                if (slot.SlotTransform.childCount > 0) {
                    usedSlots++;
                }
            }
            Debug.Log($"{playerType} has {slots.Count - usedSlots} available slots out of {slots.Count}");
            return slots.Count - usedSlots;
        }

        private void LoadCardSlots() {
            playerCardSlots.Clear();
            dealerCardSlots.Clear();

            foreach (Transform child in transform) {
                if (child == null) continue;

                if (TryParseCardSlot(child, out CardSlot slot)) {
                    (slot.Owner == PlayerType.Player ? playerCardSlots : dealerCardSlots).Add(slot);
                }
            }

            Debug.Log($"Loaded {playerCardSlots.Count} Player slots and {dealerCardSlots.Count} Dealer slots");
        }

        private bool TryParseCardSlot(Transform child, out CardSlot slot) {
            slot = null;
            if (!child.name.StartsWith("PlayerCardSlot_") && !child.name.StartsWith("DealerCardSlot_")) return false;

            string[] parts = child.name.Split('_');
            if (parts.Length <= 1 || !int.TryParse(parts[1], out int slotNumber)) {
                Debug.LogError($"Invalid slot number format in {child.name}");
                return false;
            }

            slot = new CardSlot {
                Owner = child.name.StartsWith("PlayerCardSlot_") ? PlayerType.Player : PlayerType.Dealer,
                SlotNumber = slotNumber,
                SlotTransform = child
            };

            Debug.Log($"Found {slot.Owner} slot: {child.name}");
            return true;
        }

        public List<Transform> GetPlayerSlots() => playerCardSlots.ConvertAll(slot => slot.SlotTransform);
        public List<Transform> GetDealerSlots() => dealerCardSlots.ConvertAll(slot => slot.SlotTransform);
        public int GetPlayerSlotCount() => playerCardSlots.Count;
        public int GetDealerSlotCount() => dealerCardSlots.Count;

        public bool IsSlotAvailable(Transform slot) {
            if (slot == null) return false;
            
            // Check if slot belongs to either player or dealer slots
            bool isValidSlot = playerCardSlots.Any(s => s.SlotTransform == slot) || 
                              dealerCardSlots.Any(s => s.SlotTransform == slot);
            
            if (!isValidSlot) {
                Debug.LogError($"Attempted to use invalid slot: {slot.name}");
                return false;
            }

            // Check if slot is already occupied
            return slot.childCount == 0;
        }

        public void ResetAllSlots() {
            foreach (var slot in playerCardSlots.Concat(dealerCardSlots)) {
                if (slot.SlotTransform != null) {
                    for (int i = slot.SlotTransform.childCount - 1; i >= 0; i--) {
                        var card = slot.SlotTransform.GetChild(i);
                        if (card != null) {
                            Destroy(card.gameObject);
                        }
                    }
                }
            }
            Debug.Log("All table slots have been reset");
        }

        private void ValidateAndInitializeSlots() {
            if (isInitialized) return;
            
            foreach (var slot in playerCardSlots.Concat(dealerCardSlots)) {
                if (slot.SlotTransform == null) {
                    Debug.LogError($"Found null slot transform in {(slot.Owner == PlayerType.Player ? "Player" : "Dealer")} slots");
                    continue;
                }
                slotLookup[slot.SlotTransform] = slot;
            }
            
            isInitialized = true;
        }

        public CardSlot GetSlotInfo(Transform slotTransform) {
            ValidateAndInitializeSlots();
            return slotLookup.TryGetValue(slotTransform, out CardSlot slot) ? slot : null;
        }

        public bool IsValidSlot(Transform slot, PlayerType expectedOwner) {
            if (slot == null) return false;
            
            var slotInfo = GetSlotInfo(slot);
            if (slotInfo == null) {
                Debug.LogError($"Invalid slot transform: {slot.name}");
                return false;
            }
            
            if (slotInfo.Owner != expectedOwner) {
                Debug.LogError($"Slot owner mismatch. Expected {expectedOwner} but found {slotInfo.Owner}");
                return false;
            }
            
            return true;
        }

        public int GetUsedSlotCount(PlayerType playerType) {
            ValidateAndInitializeSlots();
            var slots = playerType == PlayerType.Player ? playerCardSlots : dealerCardSlots;
            return slots.Count(slot => slot.SlotTransform != null && slot.SlotTransform.childCount > 0);
        }

        public bool HasAvailableSlots(PlayerType playerType) {
            int usedSlots = GetUsedSlotCount(playerType);
            int totalSlots = playerType == PlayerType.Player ? playerCardSlots.Count : dealerCardSlots.Count;
            bool hasSlots = usedSlots < totalSlots;
            
            if (!hasSlots) {
                Debug.LogWarning($"No more slots available for {playerType}. Used: {usedSlots}, Total: {totalSlots}");
            }
            
            return hasSlots;
        }

        public void ValidateCardPositions() {
            ValidateAndInitializeSlots();
            foreach (var slot in playerCardSlots.Concat(dealerCardSlots)) {
                if (slot.SlotTransform == null) continue;
                
                for (int i = 0; i < slot.SlotTransform.childCount; i++) {
                    var cardTransform = slot.SlotTransform.GetChild(i);
                    var card = cardTransform.GetComponent<Card>();
                    
                    if (card == null) {
                        Debug.LogError($"Found non-card object in slot {slot.SlotTransform.name}");
                        continue;
                    }
                    
                    // Verify card position matches slot position
                    if (Vector3.Distance(cardTransform.position, slot.SlotTransform.position) > 0.01f) {
                        Debug.LogWarning($"Card {card.name} position mismatch in slot {slot.SlotTransform.name}. Fixing...");
                        cardTransform.position = slot.SlotTransform.position;
                    }
                }
            }
        }
    }
}
