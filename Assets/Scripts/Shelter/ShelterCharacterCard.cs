using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameJam.Characters;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent]
    public sealed class ShelterCharacterCard : MonoBehaviour
    {
        [SerializeField] ShelterCommander commander;
        [SerializeField] BunkerSimulation simulation;
        [SerializeField] ShelterSupplies supplies;
        [SerializeField] Image[] hungerCells;
        [SerializeField] GameObject panel;
        [SerializeField] Button closeButton;
        [SerializeField] Image portrait, mentalFill;
        [SerializeField] Sprite[] portraits;
        [SerializeField] TMP_Text characterName, role, mental, planning, programming, art, combat;
        [SerializeField] GameObject emptyEquipment;
        [SerializeField] ScrollRect equipmentList;
        [SerializeField] CharacterEquipmentRow rowPrefab;

        readonly List<CharacterEquipmentRow> rows = new List<CharacterEquipmentRow>();
        CharacterIdentity character;
        public CharacterIdentity SelectedCharacter => panel.activeInHierarchy ? character : null;
        ICharacterEquipmentView equipment;
        int lastMental, lastMaximum, lastHunger;

        void OnEnable()
        {
            commander.SelectionChanged += Select;
            closeButton.onClick.AddListener(Close);
            Select(commander.Selected);
        }

        void OnDisable()
        {
            commander.SelectionChanged -= Select;
            closeButton.onClick.RemoveListener(Close);
            Close();
        }

        void Select(ShelterWalker walker)
        {
            Close();
            if (!walker || !walker.isActiveAndEnabled || !walker.TryGetComponent(out character) || character.Definition == null)
                return;

            var definition = character.Definition;
            characterName.text = definition.DisplayName;
            role.text = definition.Role;
            portrait.sprite = Array.Find(portraits, sprite => sprite && sprite.name == definition.Id);
            portrait.enabled = portrait.sprite != null;
            equipment = walker.GetComponent<ICharacterEquipmentView>();
            if (equipment != null) equipment.EquipmentChanged += RefreshEquipment;
            panel.SetActive(true);
            lastMental = -1;
            lastHunger = -1;
            RefreshMental();
            RefreshEquipment();
            equipmentList.StopMovement();
            equipmentList.verticalNormalizedPosition = 1;
        }

        void Close()
        {
            if (equipment != null) equipment.EquipmentChanged -= RefreshEquipment;
            equipment = null;
            character = null;
            panel.SetActive(false);
        }

        void LateUpdate()
        {
            if (!panel.activeSelf) return;
            if (!character || !character.isActiveAndEnabled) Close();
            else RefreshMental();
        }

        void RefreshMental()
        {
            if (character.Hunger != lastHunger)
            {
                lastHunger = character.Hunger;
                for (int i = 0; i < hungerCells.Length; i++) hungerCells[i].enabled = i < lastHunger;
            }
            int current = character.CurrentMental;
            int maximum = simulation.Rules ? simulation.Rules.MentalMaximum : 0;
            if (current == lastMental && maximum == lastMaximum) return;
            lastMental = current;
            lastMaximum = maximum;
            mental.text = maximum > 0 ? $"{current} / {maximum}" : current.ToString();
            mentalFill.fillAmount = maximum > 0 ? (float)current / maximum : 0;
        }
        internal void Unequip()
        {
            if (!commander.InputBlocked && SelectedCharacter) supplies.TryUnequip(character);
        }

        void RefreshEquipment()
        {
            planning.text = character.Planning.ToString();
            programming.text = character.Programming.ToString();
            art.text = character.Art.ToString();
            combat.text = character.Combat.ToString();
            var items = equipment?.EquippedItems;
            int count = items?.Count ?? 0;
            emptyEquipment.SetActive(count == 0);
            equipmentList.gameObject.SetActive(count > 0);
            for (int i = 0; i < count; i++)
            {
                if (i == rows.Count) rows.Add(Instantiate(rowPrefab, equipmentList.content));
                rows[i].Display(items[i]);
                rows[i].gameObject.SetActive(true);
            }
            for (int i = count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
        }
    }
}
