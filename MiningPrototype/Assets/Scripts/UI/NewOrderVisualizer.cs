﻿using NaughtyAttributes.Test;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class NewOrderVisualizer : ReadableItemVisualizer
{
    [SerializeField] TMP_Text costText;
    [SerializeField] Button buyButton;
    [SerializeField] Sprite checkmark, x;
    [SerializeField] TMP_Text costElementPrefab;
    [SerializeField] Transform costGrid;
    [SerializeField] GameObject leftclick, rightclick;

    [Inject] ProgressionHandler progressionHandler;
    [Inject] ShopPricesParser shopPricesParser;
    [Inject] InventoryManager inventoryManager;

    Dictionary<ItemType, int> orderedElementsWithAmounts = new Dictionary<ItemType, int>();
    Dictionary<ItemType, int> cost = new Dictionary<ItemType, int>();
    System.Action OnClose;

    private void Start()
    {
        StartCoroutine(ScaleCoroutine(scaleUp: true));
        UpdateCost();
        UpdateTutorialDisplays();
    }

    public void Handshake(System.Action onClose)
    {
        OnClose = onClose;
    }

    public void UpdateAmount(ItemType itemType, int amount, bool increased)
    {
        if (increased)
            progressionHandler.NotifyPassedTutorialFor("NewOrderLeftClick");
        else
            progressionHandler.NotifyPassedTutorialFor("NewOrderRightClick");

        UpdateTutorialDisplays();

        if (amount <= 0)
            orderedElementsWithAmounts.Remove(itemType);
        else
            orderedElementsWithAmounts[itemType] = amount;

        UpdateCost();
    }

    private void UpdateCost()
    {
        //clear cost display
        foreach (Transform child in costGrid)
        {
            Destroy(child.gameObject);
        }

        cost.Clear();

        //new cost fetching after implementation
        foreach (KeyValuePair<ItemType, int> item in orderedElementsWithAmounts)
        {
            ItemAmountPair price = shopPricesParser.GetPriceFor(item.Key, item.Value);

            if (cost.ContainsKey(price.type))
                cost[price.type] += price.amount;
            else
                cost.Add(price.type, price.amount);
        }

        ////calculate the new cost
        //float amount = 0;
        //
        //foreach (int i in orderedElementsWithAmounts.Values)
        //    amount += i;
        //
        //if (amount <= 0)
        //    return;
        //
        //cost[ItemType.Gold] = Mathf.FloorToInt((1f / 3f) * amount);
        //cost[ItemType.Copper] = Mathf.FloorToInt(((2f/3f) * amount * 2f) - cost[ItemType.Gold] * 2f);

        //create cost visualization
        foreach (KeyValuePair<ItemType, int> costElement in cost)
        {
            if (costElement.Value > 0)
            {
                TMP_Text costText = Instantiate(costElementPrefab, costGrid); //safe no injection needed
                Image costIcon = costText.GetComponentInChildren<Image>();

                ItemInfo costInfo = ItemsData.GetItemInfo(costElement.Key);

                //costText.text = costInfo.DisplayName;
                costText.text = costElement.Value.ToString() + "x";
                costIcon.sprite = costInfo.DisplaySprite;
            }
        }


        //update text and checkbox
        string text = "check this box to buy.";

        if (!CheckIfCanBuy(cost))
            text = "- too expensive -";

        costText.text = text;


    }

    private bool CheckIfCanBuy(Dictionary<ItemType, int> costsToOrder)
    {
        foreach (KeyValuePair<ItemType, int> i in costsToOrder)
        {
            if (!inventoryManager.PlayerHas(i.Key, i.Value))
                return SetCanBuy(false);
        }

        return SetCanBuy(true);
    }
    private bool SetCanBuy(bool canBuy)
    {
        (buyButton.targetGraphic as Image).sprite = canBuy ? checkmark : x;
        buyButton.interactable = canBuy;

        return canBuy;
    }

    public void Cancel()
    {
        OnClose?.Invoke();
        Hide();
    }

    public void Submit()
    {
        List<ItemAmountPair> itemAmountPairs = new List<ItemAmountPair>();

        foreach (KeyValuePair<ItemType, int> i in orderedElementsWithAmounts)
            itemAmountPairs.Add(new ItemAmountPair(i.Key, i.Value));

        int readableId = readableItemHandler.AddNewReadable(itemAmountPairs);
        progressionHandler.RegisterOrder(readableId, itemAmountPairs);
        inventoryManager.PlayerCollects(ItemType.NewOrder, readableId);

        foreach (var singlePrice in cost)
        {
            inventoryManager.PlayerTryPay(singlePrice.Key, singlePrice.Value);
        }

        OnClose?.Invoke();
        Hide();
    }

    private void UpdateTutorialDisplays()
    {
        leftclick.SetActive(progressionHandler.NeedsTutorialFor("NewOrderLeftClick"));
        rightclick.SetActive(progressionHandler.NeedsTutorialFor("NewOrderRightClick"));
    }
}
