using UnityEngine;

public class NewsManager : MonoBehaviour
{
    public UINewsTicker ticker;

    [Header("Virus Highlight Options")]
    public Color virusColor = Color.red;
    public bool boldVirus = true;          // 굵게
    public bool bracketVirus = true;       // [바이러스명]

    // Stage 1~5 고정 뉴스
    readonly string[] earlyNews =
{
    // Stage 1
    // 세계는 평온하고, 아직 아무도 이 병의 존재를 모른다
    "Global travel continues as usual as {VIRUS} remains undetected",

    // Stage 2
    // 특정 국가에서의 일상
    "{COUNTRY} reports record tourism numbers this season",

    // Stage 3
    // 미묘한 이상 신호
    "Health officials in {COUNTRY} note a rise in unexplained flu-like symptoms",

    // Stage 4
    // 대중의 관심 시작
    "Unusual illness reports in {COUNTRY} begin to draw public attention",

    // Stage 5
    // 사건화 직전
    "Experts warn a mysterious disease may be emerging in {COUNTRY}",

    // Stage 6
    "Doctors in {COUNTRY} observe a steady increase in seasonal flu cases",

    // Stage 7
    "Hospitals across {COUNTRY} report more patients with respiratory symptoms",

    // Stage 8
    "Health authorities begin monitoring unusual illness patterns in {COUNTRY}",

    // Stage 9
    "Isolated reports mention unexplained symptoms spreading in {COUNTRY}",

    // Stage 10
    "Medical experts closely observe emerging cases linked to {COUNTRY}",

    // Stage 11
    "Public concern grows as reports of an unidentified illness spread in {COUNTRY}",

    // Stage 12
    "Health authorities investigate clusters of unusual infections in {COUNTRY}",

    // Stage 13
    "Medical facilities in {COUNTRY} report increasing operational strain",

    // Stage 14
    "Experts debate whether recent illness cases in {COUNTRY} are connected",

    // Stage 15
    "Governments urge calm as investigations continue in {COUNTRY}",

    // Stage 16
    "Hospitals in {COUNTRY} report a sharp rise in patient admissions",

    // Stage 17
    "Authorities confirm the illness is spreading rapidly across {COUNTRY}",

    // Stage 18
    "International health agencies focus attention on developments in {COUNTRY}",

    // Stage 19
    "Travel advisories are issued following escalating cases in {COUNTRY}",

    // Stage 20
    "Experts warn the situation in {COUNTRY} may worsen without intervention",

    // Stage 21
    "Healthcare systems in {COUNTRY} face growing pressure as cases rise",

    // Stage 22
    "Emergency response measures are discussed as {COUNTRY} struggles to cope",

    // Stage 23
    "Infection rates linked to {VIRUS} continue to rise throughout {COUNTRY}",

    // Stage 24
    "Global concern intensifies as the outbreak in {COUNTRY} escalates",

    // Stage 25
    "Officials acknowledge the outbreak in {COUNTRY} is becoming difficult to control",

    // Stage 26
    "Widespread disruptions affect daily life across {COUNTRY}",

    // Stage 27
    "{COUNTRY} reports record levels of infection attributed to {VIRUS}",

    // Stage 28
    "Global health leaders call for urgent action as {VIRUS} spreads in {COUNTRY}",

    // Stage 29
    "The outbreak of {VIRUS} in {COUNTRY} reaches unprecedented scale",

    // Stage 30
    "Experts warn {COUNTRY} is entering a critical phase of the {VIRUS} crisis"
};


    string ColorToHex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }
    string FormatVirus(string virus)
    {
        // 혹시 모를 태그 깨짐 방지
        virus = virus.Replace("<", "").Replace(">", "");

        if (bracketVirus)
            virus = $"[{virus}]";

        if (boldVirus)
            virus = $"<b>{virus}</b>";

        string hex = ColorToHex(virusColor);
        virus = $"<color=#{hex}>{virus}</color>";

        return virus;
    }

    string ApplyContext(string msg, string virus, string country)
    {
        string formattedVirus = FormatVirus(virus);

        return msg
            .Replace("{VIRUS}", formattedVirus)
            .Replace("{COUNTRY}", country);
    }

    public void PlayNews(int stage, string virus, string country, int percent)
    {
        string msg;

        // 🔹 Stage 1~5 : 고정 스토리 뉴스
        if (stage <= 30)
        {
            msg = earlyNews[stage - 1];
        }
        // 🔹 Stage 6+ : 동적 뉴스
        else
        {
            if (percent < 50)
                msg = $"{virus} spreads across {country}";
            else if (percent < 80)
                msg = $"Health alert issued in {country}";
            else
                msg = $"{country} reaches critical infection levels";
        }

        msg = ApplyContext(msg, virus, country);
        ticker.SetText(msg);
    }
}
