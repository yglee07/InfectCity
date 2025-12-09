using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PieChart : MonoBehaviour
{
    public Image[] imagesPieChart;
    public TMP_Text centerText;      // 중앙 텍스트
    public float[] values;
    public float smoothSpeed = 6f; // Lerp 속도

    void LateUpdate()
    {
        float blue = NPCManager.Instance.Citizens.Count;
        float green = NPCManager.Instance.GreenZombies.Count;
        float purple = NPCManager.Instance.PurpleZombies.Count;

        values[0] = purple;
        values[1] = blue;
        values[2] = green;

        SetValues(values);
        UpdateCenterText(values);
    }
    //public void SetValues(float[] valuesToSet)
    //{
    //    float totalValues = 0;
    //    for (int i = 0; i < imagesPieChart.Length; i++)
    //    {
    //        totalValues += FindPercentage(valuesToSet, i);
    //        imagesPieChart[i].fillAmount = totalValues;
    //    }
    //}
    //private float FindPercentage(float[] valueToSet,int index)
    //{
    //    float totalAmount = 0;
    //    for (int i = 0; i < valueToSet.Length; i++)
    //    {
    //        totalAmount += valueToSet[i];
    //    }

    //    return valueToSet[index] / totalAmount;
    //}

    public void SetValues(float[] valuesToSet)
    {
        // 전체 합
        float totalAmount = valuesToSet[0] + valuesToSet[1] + valuesToSet[2];
        if (totalAmount <= 0) totalAmount = 1;

        float accumulated = 0f;

        for (int i = 0; i < imagesPieChart.Length; i++)
        {
            float percent = valuesToSet[i] / totalAmount;
            accumulated += percent;

            // ★ 부드럽게 채우기!
            imagesPieChart[i].fillAmount =
                Mathf.Lerp(imagesPieChart[i].fillAmount, accumulated, Time.deltaTime * smoothSpeed);
        }
    }
    void UpdateCenterText(float[] values)
    {
        float total = values[0] + values[1] + values[2];
        if (total <= 0)
        {
            centerText.text = "0%";
            return;
        }

        float greenRatio = values[2] / total;
        int pct = Mathf.RoundToInt(greenRatio * 100f);

        centerText.text = pct + "%";   // 중앙 퍼센트 출력
    }
}
