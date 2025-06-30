using UnityEngine;
using UnityEngine.UI;

public class GraphInfoPanel : MonoBehaviour
{
    public Text currentBalanceText;
    public Text bankBalanceText;
    public Text maxBalanceText;

    public void UpdateInfo(int currentBalance, int bankBalance, int max)
    {
        currentBalanceText.text = $"Current Balance: ₹{currentBalance}";
        bankBalanceText.text = $"Bank Balance: ₹{bankBalance}";
        maxBalanceText.text = $"Max Balance: ₹{max}";
    }
}