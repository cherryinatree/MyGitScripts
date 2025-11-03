using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class HandMoney : CombatAction
{

    public UnityEvent onArrivedAtItem;
    private CustomerShopping customerShopping;
    public GameObject moneyPrefab;
    public GameObject cardPrefab;
    public Transform ArmR;
    private CheckOutCounter checkOut;

    private bool isPayingWithCard = false;
    private bool exactChange = false;
    private float paymentAmount = 0f;

    public Dictionary<Checkout.Outcome, float> myWeights = new Dictionary<Checkout.Outcome, float> {
    { Checkout.Outcome.NextTwenty, 0.7f },  // customers usually round to $20
    { Checkout.Outcome.NextTen,    0.2f },
    { Checkout.Outcome.NextFive,   0.05f },
    { Checkout.Outcome.NextHundred,0.04f }, // occasional big bill
    { Checkout.Outcome.NextDollar, 0.01f }  // rarely just the next $1
};


    private float elapsedTime = 0f;

    public void Start()
    {

        moneyPrefab.SetActive(false);
        cardPrefab.SetActive(false);
    }


    public override void OnEnterState()
    {
        base.OnEnterState();
        customerShopping = GetComponent<CustomerShopping>();
        checkOut = customerShopping.AssignedCheckOutLine.GetComponent<CheckOutCounter>();

        isPayingWithCard = UnityEngine.Random.value > 0.5f;
        exactChange = UnityEngine.Random.value > 0.5f;
        elapsedTime = 0f;
    }

    public override void PerformAction()
    {



        if (isPayingWithCard)
        {
            Debug.Log("Paying with Card...");
            Debug.Log("Card active: " + cardPrefab.activeSelf);
            if (!cardPrefab.activeSelf)
            {
                Debug.Log("Card payment done.");
                onArrivedAtItem.Invoke();
            }
        }
        else
        {
            Debug.Log("Paying with Cash...");
            Debug.Log("Card active: " + moneyPrefab.activeSelf);
            if (!moneyPrefab.activeSelf)
            {
                Debug.Log("Cash payment done.");
                onArrivedAtItem.Invoke();
            }
        }

        if (checkOut.allItemsScanned)
        {
            if (!cardPrefab.activeSelf && !moneyPrefab.activeSelf)
            {
                if (isPayingWithCard)
                {

                    cardPrefab.SetActive(true);
                    ArmR.Rotate(90, 0, 0);
                }
                else
                {
                    moneyPrefab.SetActive(true);
                    ArmR.Rotate(90, 0, 0);

                    if (exactChange)
                    {
                        paymentAmount = checkOut.GetTotalPrice();
                    }
                    else
                    {
                        var (autoAmount, autoBills, chosenOutcome) =
                         Checkout.Cash.GenerateTendered(checkOut.GetTotalPrice(), myWeights, singleDenominationWhenPossible: true);

                        // Now compute change for that generated tender:
                        // float autoChange = Checkout.Cash.ChangeDue(checkOut.GetTotalPrice(), autoAmount);
                        paymentAmount = Checkout.Cash.ChangeDue(checkOut.GetTotalPrice(), autoAmount);
                    }
                }
            }

            StartCoroutine(MoveObjectToPosition(0.5f));
        }

    }

    public void PaymentCash()
    {

        moneyPrefab.SetActive(false);
        checkOut.saleSystem.UpdateCashAndChange(paymentAmount);
    }

    public void PaymentCard()
    {

        cardPrefab.SetActive(false);
        checkOut.saleSystem.PayWithCard();
    }

    private IEnumerator MoveObjectToPosition(float duration)
    {
        //Vector3 startPosition = obj.transform.position;

        quaternion startRotation = ArmR.rotation;


        while (elapsedTime < duration)
        {

            //obj.transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //obj.transform.position = targetPosition;

    }

    private void OnDrawGizmosSelected()
    {
        if (ArmR != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(ArmR.position + ArmR.forward * 0.5f, 0.1f);
        }
    }
}
