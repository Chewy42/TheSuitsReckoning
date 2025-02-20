using System.Collections.Generic;
using UnityEngine;

namespace CardGame {
    public class CardSlot {
        public PlayerType Owner;
        public int SlotNumber;
        public Transform SlotTransform;
    }

    public class TableCards : MonoBehaviour {
        private List<CardSlot> playerCardSlots = new List<CardSlot>();
        private List<CardSlot> dealerCardSlots = new List<CardSlot>();

        void Awake() {
            LoadCardSlots();
            ValidateSlots();
        }

        private void ValidateSlots() {
            if (playerCardSlots.Count == 0) {
                Debug.LogError("No player card slots found! Please check naming convention: PlayerCardSlot_X");
            }
            if (dealerCardSlots.Count == 0) {
                Debug.LogError("No dealer card slots found! Please check naming convention: DealerCardSlot_X");
            }

            // Validate slot numbers are sequential
            ValidateSequentialSlots(playerCardSlots, "Player");
            ValidateSequentialSlots(dealerCardSlots, "Dealer");
        }

        private void ValidateSequentialSlots(List<CardSlot> slots, string owner) {
            slots.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));
            for (int i = 0; i < slots.Count; i++) {
                if (slots[i].SlotNumber != i) {
                    Debug.LogWarning($"{owner} card slots are not sequential. Expected {i} but found {slots[i].SlotNumber}");
                }
            }
        }

        private void LoadCardSlots() {
            playerCardSlots.Clear();
            dealerCardSlots.Clear();

            for (int i = 0; i < transform.childCount; i++) {
                Transform child = transform.GetChild(i);
                if (child == null) continue;

                try {
                    if (child.name.StartsWith("PlayerCardSlot_") || child.name.StartsWith("DealerCardSlot_")) {
                        CardSlot slot = new CardSlot();
                        string[] parts = child.name.Split('_');
                        
                        if (child.name.StartsWith("PlayerCardSlot_")) {
                            slot.Owner = PlayerType.Player;
                            Debug.Log($"Found Player slot: {child.name}");
                        } else {
                            slot.Owner = PlayerType.Dealer;
                            Debug.Log($"Found Dealer slot: {child.name}");
                        }

                        // Parse slot number with better error handling
                        if (parts.Length > 1) {
                            if (int.TryParse(parts[1], out int number)) {
                                slot.SlotNumber = number;
                            } else {
                                Debug.LogError($"Invalid slot number format in {child.name}");
                                continue;
                            }
                        } else {
                            Debug.LogError($"Missing slot number in {child.name}");
                            continue;
                        }
                        
                        slot.SlotTransform = child;

                        if (slot.Owner == PlayerType.Player) {
                            playerCardSlots.Add(slot);
                        } else {
                            dealerCardSlots.Add(slot);
                        }
                    }
                } catch (System.Exception e) {
                    Debug.LogError($"Error processing card slot {child.name}: {e.Message}");
                }
            }

            // Sort slots by their number
            playerCardSlots.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));
            dealerCardSlots.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));

            Debug.Log($"Loaded {playerCardSlots.Count} Player slots and {dealerCardSlots.Count} Dealer slots");
        }

        public List<Transform> GetPlayerSlots() {
            return playerCardSlots.ConvertAll(slot => slot.SlotTransform);
        }

        public List<Transform> GetDealerSlots() {
            return dealerCardSlots.ConvertAll(slot => slot.SlotTransform);
        }

        public int GetPlayerSlotCount() {
            return playerCardSlots.Count;
        }

        public int GetDealerSlotCount() {
            return dealerCardSlots.Count;
        }
    }
}